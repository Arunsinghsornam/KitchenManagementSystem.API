using System.Security.Claims;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public AuthController(IAuthService auth, AppDbContext db, IEmailService emailService)
    {
        _auth = auth;
        _db = db;
        _emailService = emailService;
    }

 

    // ── POST /api/auth/login ──────────────────────────────────────────────────
    /// <summary>
    /// Authenticate with email + password.
    /// Returns a short-lived JWT access token and a long-lived refresh token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user != null && user.OrganizationId.HasValue && user.Organization != null)
        {
            if (user.Organization.Status == "Pending")
            {
                return StatusCode(403, ApiResponse<object>.Fail("Your organization onboarding request is pending approval by the platform administrator."));
            }
            if (user.Organization.Status == "Rejected")
            {
                return StatusCode(403, ApiResponse<object>.Fail("Your organization onboarding request was rejected."));
            }
        }

        var result = await _auth.LoginAsync(dto);

        if (result is null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid email or password."));

        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful."));
    }

    // ── POST /api/auth/refresh-token ─────────────────────────────────────────
    /// <summary>
    /// Exchange a valid refresh token for a new access + refresh token pair.
    /// Call this when the Angular app receives a 401 with an expired access token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Refresh token is required."));

        var result = await _auth.RefreshTokenAsync(dto.RefreshToken);

        if (result is null)
            return Unauthorized(ApiResponse<object>.Fail(
                "Refresh token is invalid or expired. Please log in again."));

        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Token refreshed."));
    }

    // ── POST /api/auth/logout ─────────────────────────────────────────────────
    /// <summary>
    /// Revoke the current user's refresh token. The access token stays valid
    /// until its 15-minute expiry — the Angular app should also clear localStorage.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        await _auth.RevokeTokenAsync(userId.Value);
        return Ok(ApiResponse<object>.Ok(null!, "Logged out successfully."));
    }

    // ── POST /api/auth/change-password ────────────────────────────────────────
    /// <summary>
    /// Change the currently-authenticated user's own password.
    /// Revokes all refresh tokens on success (forces re-login on other devices).
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var ok = await _auth.ChangePasswordAsync(userId.Value, dto);

        return ok
            ? Ok(ApiResponse<object>.Ok(null!, "Password changed. Please log in again."))
            : BadRequest(ApiResponse<object>.Fail("Current password is incorrect."));
    }

    // ── GET /api/auth/me ──────────────────────────────────────────────────────
    /// <summary>
    /// Returns the current user's claims decoded from the JWT.
    /// Useful for the Angular app to populate the sidebar/header without an
    /// extra DB round-trip.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var profile = new
        {
            UserId   = GetCurrentUserId(),
            Email    = User.FindFirstValue(ClaimTypes.Email),
            FullName = User.FindFirstValue("fullName"),
            Role     = User.FindFirstValue(ClaimTypes.Role),
            OutletId = User.FindFirstValue("outletId")
        };

        return Ok(ApiResponse<object>.Ok(profile));
    }

    // ── POST /api/auth/register ──────────────────────────────────────────────
    /// <summary>
    /// Public registration: creates a new Organization, a first Outlet,
    /// and a super_admin user in a single transaction.
    /// Returns a JWT so the user is immediately logged in.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromForm] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

        // Check for duplicate email
        var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (emailExists)
            return Conflict(ApiResponse<object>.Fail("An account with this email already exists."));

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // 1. Create Organization
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = dto.OrganizationName,
                Status = "Pending", // Requires approval by power_admin
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Save custom logo file if uploaded
            if (dto.Logo != null && dto.Logo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "logos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(dto.Logo.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Logo.CopyToAsync(fileStream);
                }
                org.LogoUrl = $"/uploads/logos/{uniqueFileName}";
            }

            _db.Organizations.Add(org);

            // 2. Create initial Outlet
            var outlet = new Outlet
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Name = dto.OutletName,
                Address = dto.OutletAddress,
                Active = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Outlets.Add(outlet);

            // 3. Create inactive super_admin user (active = false until organization is approved)
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                OutletId = null,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                Role = "super_admin",
                IsActive = false, // Becomes active when approved
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Users.Add(user);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // 4. Send onboarding notification email to power admin
            try
            {
                var powerAdminEmail = "arunsinghsornam@gmail.com";
                var onboardingSubject = $"New Onboarding Request: {org.Name}";
                var onboardingBody = $@"
                    <h3>New Organization Onboarding Request</h3>
                    <p><strong>Organization Name:</strong> {org.Name}</p>
                    <p><strong>Owner Name:</strong> {user.FullName}</p>
                    <p><strong>Owner Email:</strong> {user.Email}</p>
                    <p><strong>Initial Outlet Name:</strong> {outlet.Name}</p>
                    <p>Please log in to the Platform Administration portal to approve or reject this request.</p>";

                await _emailService.SendEmailAsync(powerAdminEmail, onboardingSubject, onboardingBody);
            }
            catch (Exception ex)
            {
                // Log and continue, registration shouldn't fail if notification email fails
                Console.WriteLine($"[Warning] Failed to send onboarding email notification: {ex.Message}");
            }

            return Ok(ApiResponse<object>.Ok(
                new { orgId = org.Id, status = org.Status },
                "Registration onboarding request submitted successfully. It is pending approval by the platform administrator."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<object>.Fail(
                $"An unexpected error occurred during registration: {ex.Message}"));
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
