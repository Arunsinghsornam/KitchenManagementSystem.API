namespace KitchenManagementSystem.API.Models;

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal SellingPrice { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public string? ImageUrl { get; set; }

    public Outlet Outlet { get; set; } = null!;
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
}
