using System.ComponentModel.DataAnnotations;

namespace KitchenManagementSystem.API.DTOs;

/// <summary>
/// Used by POST /api/users/{id}/reset-password (SuperAdmin only).
/// </summary>
public class ResetPasswordDto
{
    [Required, MinLength(6)]
    public string NewPassword { get; set; } = default!;
}
