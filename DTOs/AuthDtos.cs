using System.ComponentModel.DataAnnotations;

namespace KitchenManagementSystem.API.DTOs;

// ── Auth ──────────────────────────────────────────────────────────────────────

public class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, MinLength(6)]
    public string Password { get; set; } = default!;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    /// <summary> UTC timestamp when the access token expires. </summary>
    public DateTime ExpiresAt { get; set; }

    // Convenience fields so the Angular app does not need to decode the JWT
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public string Role { get; set; } = default!;
    public Guid? OutletId { get; set; }
}

public class RefreshTokenRequestDto
{
    [Required] public string RefreshToken { get; set; } = default!;
}

public class ChangePasswordDto
{
    [Required] public string CurrentPassword { get; set; } = default!;
    [Required, MinLength(6)] public string NewPassword { get; set; } = default!;
}

public class ForgotPasswordDto
{
    [Required, EmailAddress] public string Email { get; set; } = default!;
}

// ── User Management ───────────────────────────────────────────────────────────

public class CreateUserDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, MinLength(6)]
    public string Password { get; set; } = default!;

    public string? FullName { get; set; }

    [Required]
    public string Role { get; set; } = "kitchen_staff";

    /// <summary> Required for all roles except super_admin. </summary>
    public Guid? OutletId { get; set; }
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public Guid? OutletId { get; set; }
    public bool? IsActive { get; set; }
}

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public string Role { get; set; } = default!;
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ── Standard API envelope (mirrors the response format from your brief) ───────

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
