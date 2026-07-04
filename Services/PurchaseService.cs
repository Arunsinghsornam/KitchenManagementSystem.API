using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Services;

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _db;

    public PurchaseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Purchase>> GetAllAsync(Guid? organizationId, Guid? outletId)
    {
        return await _db.Purchases
            .Where(p => (organizationId == null || p.Outlet.OrganizationId == organizationId)
                     && (outletId == null || p.OutletId == outletId))
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(100)
            .ToListAsync();
    }

    public async Task<Purchase> CreateAsync(Guid outletId, CreatePurchaseDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
        {
            throw new ArgumentException("Purchase must contain at least one item.");
        }

        var subtotal = dto.Items.Sum(i => i.Quantity * i.UnitCost);
        var gst = dto.Items.Sum(i => i.Quantity * i.UnitCost * (i.GstPercent / 100));
        var total = subtotal + gst;

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            OutletId = outletId,
            SupplierId = dto.SupplierId,
            InvoiceNumber = dto.InvoiceNumber,
            PurchaseDate = dto.PurchaseDate,
            Subtotal = subtotal,
            GstAmount = gst,
            Total = total,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync();

        foreach (var item in dto.Items)
        {
            var lineTotal = item.Quantity * item.UnitCost * (1 + item.GstPercent / 100);

            _db.PurchaseItems.Add(new PurchaseItem
            {
                Id = Guid.NewGuid(),
                PurchaseId = purchase.Id,
                RawMaterialId = item.RawMaterialId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                GstPercent = item.GstPercent,
                LineTotal = lineTotal
            });

            var rawMaterial = await _db.RawMaterials
                .FirstOrDefaultAsync(r => r.Id == item.RawMaterialId);

            if (rawMaterial == null)
            {
                Console.WriteLine($"RawMaterial not found: {item.RawMaterialId}");
                continue;
            }

            Console.WriteLine($"Updating cost for {rawMaterial.Name} -> {item.UnitCost}");

            var oldStock = rawMaterial.CurrentStock;
            var oldCost = rawMaterial.AverageCost;
            var newStock = oldStock + item.Quantity;

            rawMaterial.AverageCost = oldStock <= 0
                ? item.UnitCost
                : ((oldStock * oldCost) + (item.Quantity * item.UnitCost)) / newStock;

            rawMaterial.CurrentStock = newStock;

            _db.StockLedger.Add(new StockLedger
            {
                Id = Guid.NewGuid(),
                OutletId = outletId,
                RawMaterialId = item.RawMaterialId,
                TxnDate = new DateTimeOffset(dto.PurchaseDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                TxnType = "PURCHASE",
                ReferenceType = "Purchase",
                ReferenceId = purchase.Id,
                QuantityIn = item.Quantity,
                QuantityOut = 0,
                BalanceAfter = newStock,
                UnitCost = item.UnitCost,
                Notes = $"Invoice: {dto.InvoiceNumber}",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return purchase;
    }
}
