using System;

namespace KitchenManagementSystem.API.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OutletId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string OutletName { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
