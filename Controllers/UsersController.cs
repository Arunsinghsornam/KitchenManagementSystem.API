using System.Security.Claims;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenManagementSystem.API.Controllers;

/// <summary>
/// User management — only SuperAdmin can create / delete users.
/// StoreManager can list users scoped to their own outlet.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Every endpoint requires a valid JWT; individual actions tighten further
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    // ── GET /api/users ────────────────────────────────────────────────────────
    /// <summary>
    /// SuperAdmin: returns all users across all outlets.
    /// StoreManager: returns only users belonging to their outlet.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Manager")] // super_admin + store_manager
    public async Task<IActionResult> GetAll()
    {
        Guid? filterOutlet = null;

        // Store managers see only their outlet's users
        if (!User.IsInRole("super_admin"))
        {
            var outletClaim = User.FindFirstValue("outletId");
            if (Guid.TryParse(outletClaim, out var outletId))
                filterOutlet = outletId;
        }

        var users = await _users.GetAllAsync(filterOutlet);
        return Ok(ApiResponse<IEnumerable<UserResponseDto>>.Ok(users));
    }

    // ── GET /api/users/{id} ───────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        return user is null
            ? NotFound(ApiResponse<object>.Fail($"User {id} not found."))
            : Ok(ApiResponse<UserResponseDto>.Ok(user));
    }

    // ── POST /api/users ───────────────────────────────────────────────────────
    /// <summary>
    /// Create a new user. Only SuperAdmin can create other admins.
    /// StoreManager can only create staff for their own outlet.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

        // Only super_admin can create another super_admin or store_manager
        if ((dto.Role == "super_admin" || dto.Role == "store_manager")
            && !User.IsInRole("super_admin"))
        {
            return Forbid();
        }

        // Prevent email duplicates
        if (await _users.ExistsAsync(dto.Email))
            return Conflict(ApiResponse<object>.Fail(
                $"A user with email '{dto.Email}' already exists."));

        try
        {
            var created = await _users.CreateAsync(dto);
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
    /// Update role, outlet assignment, full name, or active status.
    /// Does NOT change the password — use /api/auth/change-password for that.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        // A store_manager cannot promote anyone to super_admin
        if (dto.Role is "super_admin" && !User.IsInRole("super_admin"))
            return Forbid();

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
    /// Soft-deletes the user (IsActive = false) and revokes their refresh token.
    /// Only SuperAdmin can delete users.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Guard: a super_admin cannot delete themselves
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");
        if (Guid.TryParse(currentUserId, out var callerId) && callerId == id)
            return BadRequest(ApiResponse<object>.Fail("You cannot delete your own account."));

        var ok = await _users.DeleteAsync(id);
        return ok
            ? Ok(ApiResponse<object>.Ok(null!, "User deactivated successfully."))
            : NotFound(ApiResponse<object>.Fail($"User {id} not found."));
    }

    // ── POST /api/users/{id}/reset-password ───────────────────────────────────
    /// <summary>
    /// SuperAdmin only: set a new temporary password for any user.
    /// The user must change it on next login (you can add a ForcePasswordReset
    /// column later if needed).
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

        // Re-use the ChangePassword flow but bypass the current-password check
        await _users.SetPasswordAsync(id, dto.NewPassword);

        return Ok(ApiResponse<object>.Ok(null!, "Password reset successfully."));
    }
}
