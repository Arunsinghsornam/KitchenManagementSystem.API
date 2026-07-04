namespace KitchenManagementSystem.API.Models;

public class Category
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Outlet? Outlet { get; set; }
}
