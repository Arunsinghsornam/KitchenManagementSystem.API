using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _db;
    private static readonly Guid DefaultOutletId =
    Guid.Parse("00000000-0000-0000-0000-000000000001");
    public InventoryController(AppDbContext db) => _db = db;

    // GET api/inventory — list all raw materials
    [HttpGet]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> GetAll()
    {
        var materials = await _db.RawMaterials
            .Where(m => m.OutletId == DefaultOutletId)
            .Include(m => m.Category)
            .OrderBy(m => m.Name)
            .Select(m => new {
                m.Id,
                m.Code,
                m.Name,
                m.Unit,
                m.ReorderLevel,
                m.CurrentStock,
                m.AverageCost,
                m.CategoryId,
                CategoryName = m.Category != null ? m.Category.Name : null
            })
            .ToListAsync();
        return Ok(materials);
    }

    // POST api/inventory — add new raw material
    [HttpPost]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> Create([FromBody] RawMaterial material)
    {
        material.Id = Guid.NewGuid();
        material.OutletId = DefaultOutletId;
        material.CreatedAt = DateTimeOffset.UtcNow;
        _db.RawMaterials.Add(material);
        await _db.SaveChangesAsync();
        return Ok(material);
    }

    // PUT api/inventory/{id} — update raw material
    [HttpPut("{id}")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RawMaterial updated)
    {
        var material = await _db.RawMaterials.FindAsync(id);
        if (material == null) return NotFound();

        material.Code = updated.Code;
        material.Name = updated.Name;
        material.Unit = updated.Unit;
        material.ReorderLevel = updated.ReorderLevel;
        material.CategoryId = updated.CategoryId;

        await _db.SaveChangesAsync();
        return Ok(material);
    }

    // DELETE api/inventory/{id} — delete raw material
    [HttpDelete("{id}")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var material = await _db.RawMaterials.FindAsync(id);
        if (material == null) return NotFound();
        _db.RawMaterials.Remove(material);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }

    // POST api/inventory/{id}/adjust — manually adjust stock
    [HttpPost("{id}/adjust")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> Adjust(Guid id, [FromBody] AdjustStockDto dto)
    {
        var material = await _db.RawMaterials.FindAsync(id);


if (material == null)
            return NotFound();

        // Prevent negative stock
        if (material.CurrentStock + dto.Quantity < 0)
        {
            return BadRequest(new
            {
                message = "Insufficient stock"
            });
        }

        material.CurrentStock += dto.Quantity;

        var ledger = new StockLedger
        {
            Id = Guid.NewGuid(),
            OutletId = DefaultOutletId,
            RawMaterialId = id,

            TxnDate = DateTimeOffset.UtcNow,
            TxnType = "ADJUSTMENT",

            QuantityIn = dto.Quantity > 0 ? dto.Quantity : 0,
            QuantityOut = dto.Quantity < 0 ? Math.Abs(dto.Quantity) : 0,

            BalanceAfter = material.CurrentStock,

            Notes = dto.Notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.StockLedger.Add(ledger);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            newStock = material.CurrentStock
        });


}


    // GET api/inventory/categories — list all categories
    [HttpGet("categories")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _db.Categories
            .Where(c => c.OutletId == DefaultOutletId)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(cats);
    }

    // POST api/inventory/categories — add category
    [HttpPost("categories")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> CreateCategory([FromBody] Category cat)
    {
        cat.Id = Guid.NewGuid();
        cat.OutletId = DefaultOutletId;
        cat.CreatedAt = DateTimeOffset.UtcNow;
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return Ok(cat);
    }
}

// DTO = Data Transfer Object — a simple class to receive request data
public class AdjustStockDto
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}
