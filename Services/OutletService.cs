using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Services;

public class OutletService : IOutletService
{
    private readonly AppDbContext _db;

    public OutletService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Outlet>> GetAllAsync(Guid? organizationId = null)
    {
        return await _db.Outlets
            .Where(o => organizationId == null || o.OrganizationId == organizationId)
            .OrderBy(o => o.Name)
            .ToListAsync();
    }

    public async Task<Outlet?> GetByIdAsync(Guid id)
    {
        return await _db.Outlets.FindAsync(id);
    }

    public async Task<Outlet> CreateAsync(Outlet outlet)
    {
        outlet.Id = Guid.NewGuid();
        outlet.CreatedAt = DateTimeOffset.UtcNow;

        _db.Outlets.Add(outlet);
        await _db.SaveChangesAsync();

        return outlet;
    }

    public async Task<bool> UpdateAsync(Guid id, Outlet updatedOutlet)
    {
        var outlet = await _db.Outlets.FindAsync(id);
        if (outlet == null)
            return false;

        outlet.Name = updatedOutlet.Name;
        outlet.Address = updatedOutlet.Address;
        outlet.Active = updatedOutlet.Active;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var outlet = await _db.Outlets.FindAsync(id);
        if (outlet == null)
            return false;

        _db.Outlets.Remove(outlet);
        await _db.SaveChangesAsync();
        return true;
    }
}
