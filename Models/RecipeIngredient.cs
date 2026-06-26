namespace KitchenManagementSystem.API.Models;

public class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid RawMaterialId { get; set; }
    public decimal Quantity { get; set; }

    public MenuItem MenuItem { get; set; } = null!;
    public RawMaterial RawMaterial { get; set; } = null!;
}
