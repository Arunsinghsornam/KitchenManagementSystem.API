using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(Guid? organizationId)
    {
        var query = _db.Categories.Include(c => c.Outlet).AsQueryable();

        if (organizationId.HasValue)
        {
            query = query.Where(c => c.Outlet != null && c.Outlet.OrganizationId == organizationId.Value);
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category> CreateAsync(CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Category name is required.");

        var outletExists = await _db.Outlets.AnyAsync(o => o.Id == dto.OutletId);
        if (!outletExists)
            throw new KeyNotFoundException("Invalid OutletId.");

        var name = dto.Name.Trim();
        bool exists = await _db.Categories.AnyAsync(c =>
            c.OutletId == dto.OutletId &&
            c.Name.ToLower() == name.ToLower());

        if (exists)
            throw new InvalidOperationException("Category already exists.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            OutletId = dto.OutletId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        // Load the outlet reference to match previous controller return behavior
        await _db.Entry(category).Reference(c => c.Outlet).LoadAsync();

        return category;
    }

    public async Task<Category?> UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Category name is required.");

        var outletExists = await _db.Outlets.AnyAsync(o => o.Id == dto.OutletId);
        if (!outletExists)
            throw new KeyNotFoundException("Selected outlet does not exist.");

        var name = dto.Name.Trim();
        bool exists = await _db.Categories.AnyAsync(c =>
            c.Id != id &&
            c.OutletId == dto.OutletId &&
            c.Name.ToLower() == name.ToLower());

        if (exists)
            throw new InvalidOperationException("Category already exists for this outlet.");

        category.Name = name;
        category.OutletId = dto.OutletId;
        category.Outlet = null;

        await _db.SaveChangesAsync();

        // Reload the outlet reference
        await _db.Entry(category).Reference(c => c.Outlet).LoadAsync();

        return category;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null)
            return false;

        bool isUsed = await _db.RawMaterials.AnyAsync(r => r.CategoryId == id);
        if (isUsed)
            throw new InvalidOperationException("Cannot delete category because it is used by raw materials.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return true;
    }
}
