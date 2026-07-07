namespace KitchenManagementSystem.API.DTOs;

public class CreateRecipeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal SellingPrice { get; set; }
    public List<IngredientDto> Ingredients { get; set; } = [];
    public string? ImageUrl { get; set; }
}

public class IngredientDto
{
    public Guid RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
}
