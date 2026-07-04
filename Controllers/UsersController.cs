using System.Security.Claims;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenManagementSystem.API.Controllers;

/// <summary>
/// User management — only SuperAdmin / PowerAdmin can create / delete users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Every endpoint requires a valid JWT; individual actions tighten further
public class UsersController : BaseApiController
{
    private readonly IUserService _users;
    private readonly IEmailService _emailService;

    public UsersController(IUserService users, IEmailService emailService)
    {
        _users = users;
        _emailService = emailService;
    }

    // ── GET /api/users ────────────────────────────────────────────────────────
    /// <summary>
    /// SuperAdmin: returns all users across all outlets for their organization.
    /// PowerAdmin: returns all users across all organizations.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? organizationId)
    {
        Guid? orgId = IsPowerAdmin() ? organizationId : GetOrganizationId();
        var users = await _users.GetAllAsync(orgId, null);
        return Ok(ApiResponse<IEnumerable<UserResponseDto>>.Ok(users));
    }

    // ── GET /api/users/{id} ───────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail($"User {id} not found."));

        if (!IsPowerAdmin() && user.OrganizationId != GetOrganizationId())
            return Forbid();

        return Ok(ApiResponse<UserResponseDto>.Ok(user));
    }

    // ── POST /api/users ───────────────────────────────────────────────────────
    /// <summary>
    /// Create a new user assignment.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

        // Prevent email duplicates
        if (await _users.ExistsAsync(dto.Email))
            return Conflict(ApiResponse<object>.Fail(
                $"A user with email '{dto.Email}' already exists."));

        if (dto.Role == "super_admin" && !IsPowerAdmin())
            return BadRequest(ApiResponse<object>.Fail("You do not have permission to create a Super Admin user."));

        try
        {
            Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();
            var created = await _users.CreateAsync(dto, orgId);

            // Send onboarding credentials email to the newly created user
            try
            {
                string? replyTo = null;
                string? fromName = null;
                if (!IsPowerAdmin())
                {
                    replyTo = User.FindFirstValue(ClaimTypes.Email);
                    fromName = User.FindFirstValue("organizationName");
                }

                var subject = "Your Account Credentials - Kitchen Operations Portal";
                var body = $@"
                    <h3>Welcome to the Kitchen Operations Portal</h3>
                    <p>Hello {created.FullName},</p>
                    <p>Your account has been created by your administrator.</p>
                    <p><strong>Username/Email:</strong> {created.Email}</p>
                    <p><strong>Temporary Password:</strong> {dto.Password}</p>
                    <p><strong>Your Assigned Role:</strong> {created.Role}</p>
                    <p><a href='http://localhost:4200/login'>Click here to login</a></p>
                    <p>Please log in and update your password as soon as possible.</p>";

                await _emailService.SendEmailAsync(created.Email, subject, body, replyTo, fromName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to send credentials email to {created.Email}: {ex.Message}");
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                ApiResponse<UserResponseDto>.Ok(created, "User created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── PUT /api/users/{id} ───────────────────────────────────────────────────
    /// <summary>
    /// Update user.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail($"User {id} not found."));

        if (!IsPowerAdmin() && user.OrganizationId != GetOrganizationId())
            return Forbid();

        if (dto.Role == "super_admin" && !IsPowerAdmin())
            return BadRequest(ApiResponse<object>.Fail("You do not have permission to assign the Super Admin role."));

        if (!string.IsNullOrEmpty(dto.Email) && !IsPowerAdmin())
            return BadRequest(ApiResponse<object>.Fail("You do not have permission to change the username/email of users."));

        try
        {
            var updated = await _users.UpdateAsync(id, dto);
            return updated is null
                ? NotFound(ApiResponse<object>.Fail($"User {id} not found."))
                : Ok(ApiResponse<UserResponseDto>.Ok(updated, "User updated."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── DELETE /api/users/{id} ────────────────────────────────────────────────
    /// <summary>
    /// Soft-deletes the user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Guard: a user cannot delete themselves
        var currentUserId = GetUserId();
        if (currentUserId == id)
            return BadRequest(ApiResponse<object>.Fail("You cannot delete your own account."));

        var user = await _users.GetByIdAsync(id);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail($"User {id} not found."));

        if (!IsPowerAdmin() && user.OrganizationId != GetOrganizationId())
            return Forbid();

        // Guard: Super Admin cannot be deleted by non-Power Admins
        if (user.Role == "super_admin" && !IsPowerAdmin())
            return BadRequest(ApiResponse<object>.Fail("Super Admin accounts cannot be deleted."));

        var ok = await _users.DeleteAsync(id);
        return ok
            ? Ok(ApiResponse<object>.Ok(null!, "User deactivated successfully."))
            : NotFound(ApiResponse<object>.Fail($"User {id} not found."));
    }

    // ── POST /api/users/{id}/reset-password ───────────────────────────────────
    /// <summary>
    /// Admin reset password.
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed"));

        var user = await _users.GetByIdAsync(id);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail($"User {id} not found."));

        if (!IsPowerAdmin() && user.OrganizationId != GetOrganizationId())
            return Forbid();

        await _users.SetPasswordAsync(id, dto.NewPassword);

        // Send password reset email to the user
        try
        {
            string? replyTo = null;
            string? fromName = null;
            if (!IsPowerAdmin())
            {
                replyTo = User.FindFirstValue(ClaimTypes.Email);
                fromName = User.FindFirstValue("organizationName");
            }

            var subject = "Your Password Has Been Reset";
            var body = $@"
                <h3>Password Reset Notification</h3>
                <p>Hello {user.FullName},</p>
                <p>Your account password has been reset by the administrator.</p>
                <p><strong>Username/Email:</strong> {user.Email}</p>
                <p><strong>New Password:</strong> {dto.NewPassword}</p>
                <p><a href='http://localhost:4200/login'>Click here to login</a></p>
                <p>Please log in using your new password. Make sure to change it after logging in.</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body, replyTo, fromName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Failed to send password reset email to {user.Email}: {ex.Message}");
        }

        return Ok(ApiResponse<object>.Ok(null!, "Password reset successfully."));
    }
}
