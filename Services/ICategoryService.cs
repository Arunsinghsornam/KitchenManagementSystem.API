using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync(Guid? organizationId);
    Task<Category> CreateAsync(CreateCategoryDto dto);
    Task<Category?> UpdateAsync(Guid id, UpdateCategoryDto dto);
    Task<bool> DeleteAsync(Guid id);
}
