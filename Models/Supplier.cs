namespace KitchenManagementSystem.API.Models;

public class Supplier
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Mobile { get; set; }
    public string? GstNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal Outstanding { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Outlet Outlet { get; set; } = null!;
}
