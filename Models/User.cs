namespace KitchenManagementSystem.API.Models;

/// <summary>
/// Maps to dbo.Users — schema already exists in PavRepublicDB as confirmed
/// by the SSMS screenshot (Id, OutletId, Email, PasswordHash, FullName,
/// Role, IsActive, RefreshToken, RefreshTokenExpiry, CreatedAt).
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary> Nullable FK to dbo.Outlets — null means the user is a SuperAdmin
    /// not scoped to any single outlet. </summary>
    public Guid? OutletId { get; set; }
    public Outlet? Outlet { get; set; }

    public string Email { get; set; } = default!;

    /// <summary> BCrypt hash — NEVER store plain-text here. </summary>
    public string PasswordHash { get; set; } = default!;

    public string? FullName { get; set; }

    /// <summary>
    /// Must be one of the four role strings the existing Program.cs policies
    /// expect: "super_admin" | "store_manager" | "kitchen_staff" | "accountant"
    /// </summary>
    public string Role { get; set; } = "kitchen_staff";

    public bool IsActive { get; set; } = true;

    /// <summary> Opaque random string stored after a successful login. </summary>
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
