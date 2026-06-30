using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<RawMaterial>> GetAllRawMaterialsAsync(Guid outletId)
    {
        return await _db.RawMaterials
            .Where(m => m.OutletId == outletId)
            .Include(m => m.Category)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<RawMaterial?> GetRawMaterialByIdAsync(Guid id)
    {
        return await _db.RawMaterials.FindAsync(id);
    }

    public async Task<RawMaterial> CreateRawMaterialAsync(Guid outletId, RawMaterial material)
    {
        material.Id = Guid.NewGuid();
        material.OutletId = outletId;
        material.CreatedAt = DateTimeOffset.UtcNow;
        material.Outlet = null!;

        _db.RawMaterials.Add(material);
        await _db.SaveChangesAsync();

        return material;
    }

    public async Task<RawMaterial?> UpdateRawMaterialAsync(Guid id, RawMaterial updated)
    {
        var material = await _db.RawMaterials.FindAsync(id);
        if (material == null)
            return null;

        material.Code = updated.Code;
        material.Name = updated.Name;
        material.Unit = updated.Unit;
        material.ReorderLevel = updated.ReorderLevel;
        material.CategoryId = updated.CategoryId;

        // From RawMaterialsController properties update:
        material.CurrentStock = updated.CurrentStock;
        material.AverageCost = updated.AverageCost;
        material.TrackExpiry = updated.TrackExpiry;

        await _db.SaveChangesAsync();
        return material;
    }

    public async Task<bool> DeleteRawMaterialAsync(Guid id)
    {
        var material = await _db.RawMaterials.FindAsync(id);
        if (material == null)
            return false;

        _db.RawMaterials.Remove(material);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal?> AdjustStockAsync(Guid outletId, Guid id, decimal quantity, string? notes)
    {
        var material = await _db.RawMaterials.FindAsync(id);
        if (material == null)
            return null;

        if (material.CurrentStock + quantity < 0)
            throw new InvalidOperationException("Insufficient stock");

        material.CurrentStock += quantity;

        var ledger = new StockLedger
        {
            Id = Guid.NewGuid(),
            OutletId = outletId,
            RawMaterialId = id,
            TxnDate = DateTimeOffset.UtcNow,
            TxnType = "ADJUSTMENT",
            QuantityIn = quantity > 0 ? quantity : 0,
            QuantityOut = quantity < 0 ? Math.Abs(quantity) : 0,
            BalanceAfter = material.CurrentStock,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.StockLedger.Add(ledger);
        await _db.SaveChangesAsync();

        return material.CurrentStock;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync(Guid outletId)
    {
        return await _db.Categories
            .Where(c => c.OutletId == outletId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category> CreateCategoryAsync(Guid outletId, Category cat)
    {
        cat.Id = Guid.NewGuid();
        cat.OutletId = outletId;
        cat.CreatedAt = DateTimeOffset.UtcNow;

        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        return cat;
    }
}
