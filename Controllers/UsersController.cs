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

    public UsersController(IUserService users) => _users = users;

    // ── GET /api/users ────────────────────────────────────────────────────────
    /// <summary>
    /// SuperAdmin: returns all users across all outlets for their organization.
    /// PowerAdmin: returns all users across all organizations.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> GetAll()
    {
        Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();
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

        try
        {
            Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();
            var created = await _users.CreateAsync(dto, orgId);
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

        return Ok(ApiResponse<object>.Ok(null!, "Password reset successfully."));
    }
}
