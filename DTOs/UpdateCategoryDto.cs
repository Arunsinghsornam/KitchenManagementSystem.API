namespace KitchenManagementSystem.API.DTOs;

public class UpdateCategoryDto
{
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
}