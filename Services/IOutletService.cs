using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface IOutletService
{
    Task<IEnumerable<Outlet>> GetAllAsync(Guid? organizationId = null);
    Task<Outlet?> GetByIdAsync(Guid id);
    Task<Outlet> CreateAsync(Outlet outlet);
    Task<bool> UpdateAsync(Guid id, Outlet updatedOutlet);
    Task<bool> DeleteAsync(Guid id);
}
