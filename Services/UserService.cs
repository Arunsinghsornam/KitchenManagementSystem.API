using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    // Valid role strings — keep in sync with Program.cs policy definitions
    private static readonly HashSet<string> ValidRoles = new()
    {
        "power_admin", "super_admin", "store_manager", "kitchen_staff", "accountant"
    };

    public UserService(AppDbContext db) => _db = db;

    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync(Guid? organizationId, Guid? outletId = null)
    {
        var users = await _db.Users
            .Include(u => u.Outlet)
            .Where(u =>
                u.IsActive &&
                (organizationId == null || u.OrganizationId == organizationId) &&
                (!outletId.HasValue || u.OutletId == outletId))
            .OrderBy(u => u.FullName)
            .ToListAsync();

        Console.WriteLine("===== USERS RETURNED =====");
        foreach (var u in users)
        {
            Console.WriteLine($"{u.FullName} - Active: {u.IsActive}");
        }

        return users.Select(ToDto);
    }

    public async Task<UserResponseDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.Include(u => u.Outlet)
                                   .FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? null : ToDto(user);
    }

    public async Task<bool> ExistsAsync(string email) =>
        await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto, Guid? organizationId)
    {
        if (!ValidRoles.Contains(dto.Role))
            throw new ArgumentException($"Invalid role '{dto.Role}'. " +
                $"Valid roles: {string.Join(", ", ValidRoles)}");

        if (dto.OutletId.HasValue)
        {
            var outlet = await _db.Outlets.FindAsync(dto.OutletId.Value);
            if (outlet == null || (organizationId.HasValue && outlet.OrganizationId != organizationId.Value))
            {
                throw new ArgumentException("Selected outlet does not belong to your organization.");
            }
        }

        var user = new User
        {
            Email       = dto.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName    = dto.FullName?.Trim(),
            Role        = dto.Role,
            OutletId    = dto.OutletId,
            OrganizationId = organizationId,
            IsActive    = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Reload with Outlet navigation for the response DTO
        await _db.Entry(user).Reference(u => u.Outlet).LoadAsync();
        return ToDto(user);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<UserResponseDto?> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _db.Users.Include(u => u.Outlet)
                                   .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return null;

        if (dto.FullName  is not null) user.FullName  = dto.FullName.Trim();
        if (dto.IsActive  is not null) user.IsActive  = dto.IsActive.Value;
        
        if (dto.OutletId is not null)
        {
            if (dto.OutletId == Guid.Empty)
            {
                user.OutletId = null;
            }
            else
            {
                var outlet = await _db.Outlets.FindAsync(dto.OutletId.Value);
                if (outlet == null || (user.OrganizationId.HasValue && outlet.OrganizationId != user.OrganizationId.Value))
                {
                    throw new ArgumentException("Selected outlet does not belong to your organization.");
                }
                user.OutletId = dto.OutletId;
            }
        }

        if (dto.Role is not null)
        {
            if (!ValidRoles.Contains(dto.Role))
                throw new ArgumentException($"Invalid role '{dto.Role}'.");
            user.Role = dto.Role;
        }

        await _db.SaveChangesAsync();
        await _db.Entry(user).Reference(u => u.Outlet).LoadAsync();
        return ToDto(user);
    }

    // ── Delete (soft) ─────────────────────────────────────────────────────────

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return false;

        // Soft delete: mark inactive and revoke any live session
        user.IsActive = false;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Admin password reset ──────────────────────────────────────────────────

    public async Task SetPasswordAsync(Guid id, string newPassword)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync();
    }

    // ── Projection helper ─────────────────────────────────────────────────────

    private static UserResponseDto ToDto(User u) => new()
    {
        Id         = u.Id,
        Email      = u.Email,
        FullName   = u.FullName,
        Role       = u.Role,
        OutletId   = u.OutletId,
        OutletName = u.Outlet?.Name,
        OrganizationId = u.OrganizationId,
        IsActive   = u.IsActive,
        CreatedAt  = u.CreatedAt
    };
}
