using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Services;

public class SalesService : ISalesService
{
    private readonly AppDbContext _db;

    public SalesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Sale>> GetAllAsync(Guid? organizationId, Guid? outletId, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        return await _db.Sales
            .Include(s => s.Items)
            .Where(s => (organizationId == null || s.Outlet.OrganizationId == organizationId)
                     && (outletId == null || s.OutletId == outletId)
                     && (fromDate == null || s.SaleDate >= fromDate.Value)
                     && (toDate == null || s.SaleDate <= toDate.Value))
            .OrderByDescending(s => s.SaleDate)
            .Take(100)
            .ToListAsync();
    }

    public async Task<(bool HasShortage, IEnumerable<object> Shortages)> StockCheckAsync(Guid menuItemId, decimal quantity)
    {
        var ingredients = await _db.RecipeIngredients
            .Where(r => r.MenuItemId == menuItemId)
            .Include(r => r.RawMaterial)
            .ToListAsync();

        var shortages = ingredients
            .Where(r => r.RawMaterial.CurrentStock < r.Quantity * quantity)
            .Select(r => new
            {
                Item = r.RawMaterial.Name,
                Have = r.RawMaterial.CurrentStock,
                Need = r.Quantity * quantity,
                Unit = r.RawMaterial.Unit
            })
            .ToList();

        return (shortages.Any(), shortages);
    }

    public async Task<Sale> CreateAsync(Guid outletId, CreateSaleDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
        {
            throw new ArgumentException("Please add at least one sale item.");
        }

        var duplicateItems = dto.Items
            .GroupBy(x => x.MenuItemId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateItems.Any())
        {
            throw new ArgumentException("Duplicate menu items are not allowed.");
        }

        foreach (var line in dto.Items)
        {
            var ingredients = await _db.RecipeIngredients
                .Where(r => r.MenuItemId == line.MenuItemId)
                .Include(r => r.RawMaterial)
                .ToListAsync();

            foreach (var ing in ingredients)
            {
                var needed = ing.Quantity * line.Quantity;

                if (ing.RawMaterial.CurrentStock < needed)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock: {ing.RawMaterial.Name}. Need {needed} {ing.RawMaterial.Unit}, Available {ing.RawMaterial.CurrentStock}.");
                }
            }
        }

        var subtotal = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

        if (dto.Discount < 0)
        {
            throw new ArgumentException("Discount cannot be negative.");
        }

        if (dto.Discount > subtotal)
        {
            throw new ArgumentException("Discount cannot be greater than subtotal.");
        }

        var total = subtotal - dto.Discount;

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            OutletId = outletId,
            SaleDate = dto.SaleDate,
            Channel = dto.Channel,
            Subtotal = subtotal,
            Discount = dto.Discount,
            Total = total,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();

        foreach (var line in dto.Items)
        {
            _db.SalesItems.Add(new SaleItem
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                MenuItemId = line.MenuItemId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice
            });

            var ingredients = await _db.RecipeIngredients
                .Where(r => r.MenuItemId == line.MenuItemId)
                .Include(r => r.RawMaterial)
                .ToListAsync();

            foreach (var ing in ingredients)
            {
                var needed = ing.Quantity * line.Quantity;
                
                ing.RawMaterial.CurrentStock -= needed;

                _db.StockLedger.Add(new StockLedger
                {
                    Id = Guid.NewGuid(),
                    OutletId = outletId,
                    RawMaterialId = ing.RawMaterialId,
                    TxnDate = new DateTimeOffset(dto.SaleDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                    TxnType = "SALE",
                    ReferenceType = "Sale",
                    ReferenceId = sale.Id,
                    QuantityIn = 0,
                    QuantityOut = needed,
                    BalanceAfter = ing.RawMaterial.CurrentStock,
                    Notes = $"Sale of {line.Quantity} units",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        return sale;
    }
}
