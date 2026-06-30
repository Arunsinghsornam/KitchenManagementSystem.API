using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Services;

public class RecipeService : IRecipeService
{
    private readonly AppDbContext _db;

    public RecipeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<MenuItem>> GetAllAsync(Guid outletId)
    {
        return await _db.MenuItems
            .Where(m => m.OutletId == outletId && m.Active)
            .Include(m => m.RecipeIngredients)
                .ThenInclude(ri => ri.RawMaterial)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<MenuItem> CreateAsync(Guid outletId, CreateRecipeDto dto)
    {
        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            OutletId = outletId,
            Name = dto.Name,
            Category = dto.Category,
            SellingPrice = dto.SellingPrice,
            Active = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.MenuItems.Add(menuItem);

        foreach (var ing in dto.Ingredients)
        {
            _db.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItem.Id,
                RawMaterialId = ing.RawMaterialId,
                Quantity = ing.Quantity
            });
        }

        await _db.SaveChangesAsync();
        return menuItem;
    }

    public async Task<bool> UpdateAsync(Guid id, CreateRecipeDto dto)
    {
        var menuItem = await _db.MenuItems.FindAsync(id);
        if (menuItem == null)
            return false;

        menuItem.Name = dto.Name;
        menuItem.Category = dto.Category;
        menuItem.SellingPrice = dto.SellingPrice;

        var oldIngredients = await _db.RecipeIngredients
            .Where(r => r.MenuItemId == id)
            .ToListAsync();

        _db.RecipeIngredients.RemoveRange(oldIngredients);

        foreach (var ing in dto.Ingredients)
        {
            _db.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                MenuItemId = id,
                RawMaterialId = ing.RawMaterialId,
                Quantity = ing.Quantity
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var menuItem = await _db.MenuItems.FirstOrDefaultAsync(x => x.Id == id);
        if (menuItem == null)
            return false;

        menuItem.Active = false;
        await _db.SaveChangesAsync();
        return true;
    }
}
