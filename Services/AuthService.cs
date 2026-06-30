using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KitchenManagementSystem.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // --------------------------------------------------
    // LOGIN
    // --------------------------------------------------

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Outlet)
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        return await IssueTokensAsync(user);
    }

    // --------------------------------------------------
    // REFRESH TOKEN
    // --------------------------------------------------

    public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        var user = await _db.Users
            .Include(u => u.Outlet)
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken &&
                u.RefreshTokenExpiry > DateTimeOffset.UtcNow &&
                u.IsActive);

        if (user == null)
            return null;

        return await IssueTokensAsync(user);
    }

    // --------------------------------------------------
    // CHANGE PASSWORD
    // --------------------------------------------------

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;

        await _db.SaveChangesAsync();

        return true;
    }

    // --------------------------------------------------
    // LOGOUT
    // --------------------------------------------------

    public async Task<bool> RevokeTokenAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return false;

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;

        await _db.SaveChangesAsync();

        return true;
    }

    // --------------------------------------------------
    // ISSUE TOKENS
    // --------------------------------------------------

    private async Task<LoginResponseDto> IssueTokensAsync(User user)
    {
        var (accessToken, expiresAt) = GenerateAccessToken(user);

        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime);

        await _db.SaveChangesAsync();

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            OutletId = user.OutletId,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.Name
        };
    }

    // --------------------------------------------------
    // ACCESS TOKEN
    // --------------------------------------------------

    private (string token, DateTime expiresAt) GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.Add(AccessTokenLifetime);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("outletId", user.OutletId?.ToString() ?? ""),
            new Claim("organizationId", user.OrganizationId?.ToString() ?? ""),
            new Claim("fullName", user.FullName ?? ""),
            new Claim("organizationName", user.Organization?.Name ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds);

        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt
        );
    }

    // --------------------------------------------------
    // REFRESH TOKEN
    // --------------------------------------------------

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));
    }
}