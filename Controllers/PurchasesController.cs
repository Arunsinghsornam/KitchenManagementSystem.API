using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly AppDbContext _db;

    private static readonly Guid DefaultOutletId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public PurchasesController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/purchases
    [HttpGet]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> GetAll()
    {
        var purchases = await _db.Purchases
            .Where(p => p.OutletId == DefaultOutletId)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(100)
            .Select(p => new
            {
                p.Id,
                p.InvoiceNumber,
                p.PurchaseDate,
                p.Subtotal,
                p.GstAmount,
                p.Total,
                SupplierName = p.Supplier.Name
            })
            .ToListAsync();

        return Ok(purchases);
    }

    // POST api/purchases
    [HttpPost]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
        {
            return BadRequest("Purchase must contain at least one item.");
        }

        var subtotal = dto.Items.Sum(i => i.Quantity * i.UnitCost);
        var gst = dto.Items.Sum(i => i.Quantity * i.UnitCost * (i.GstPercent / 100));
        var total = subtotal + gst;

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            OutletId = DefaultOutletId,
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
            var lineTotal =
                item.Quantity *
                item.UnitCost *
                (1 + item.GstPercent / 100);

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

            rawMaterial.AverageCost =
                oldStock <= 0
                    ? item.UnitCost
                    : (
                        (oldStock * oldCost) +
                        (item.Quantity * item.UnitCost)
                      ) / newStock;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            purchase.Id,
            message = "Purchase recorded successfully. Stock and average cost updated."
        });
    }
}

public class CreatePurchaseDto
{
    public Guid SupplierId { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateOnly PurchaseDate { get; set; }

    public List<PurchaseLineDto> Items { get; set; } = [];
}

public class PurchaseLineDto
{
    public Guid RawMaterialId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal GstPercent { get; set; }
}