using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface IInventoryService
{
    Task<IEnumerable<RawMaterial>> GetAllRawMaterialsAsync(Guid outletId);
    Task<RawMaterial?> GetRawMaterialByIdAsync(Guid id);
    Task<RawMaterial> CreateRawMaterialAsync(Guid outletId, RawMaterial material);
    Task<RawMaterial?> UpdateRawMaterialAsync(Guid id, RawMaterial updated);
    Task<bool> DeleteRawMaterialAsync(Guid id);
    Task<decimal?> AdjustStockAsync(Guid outletId, Guid id, decimal quantity, string? notes);
    Task<IEnumerable<Category>> GetCategoriesAsync(Guid outletId);
    Task<Category> CreateCategoryAsync(Guid outletId, Category cat);
}
