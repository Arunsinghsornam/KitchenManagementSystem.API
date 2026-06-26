using System.Security.Claims;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

 

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

    // ── Helper ────────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
