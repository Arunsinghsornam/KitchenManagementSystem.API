using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface IRecipeService
{
    Task<IEnumerable<MenuItem>> GetAllAsync(Guid outletId);
    Task<MenuItem> CreateAsync(Guid outletId, CreateRecipeDto dto);
    Task<bool> UpdateAsync(Guid id, CreateRecipeDto dto);
    Task<bool> DeleteAsync(Guid id);
}
