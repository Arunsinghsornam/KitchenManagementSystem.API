namespace KitchenManagementSystem.API.Controllers;

using global::KitchenManagementSystem.API.Data;
using global::KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("api/[controller]")]
public class RawMaterialsController : ControllerBase
{
    private readonly AppDbContext _db;

    private static readonly Guid DefaultOutletId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public RawMaterialsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.RawMaterials
            .Where(x => x.OutletId == DefaultOutletId)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await _db.RawMaterials.FindAsync(id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RawMaterial material)
    {
        material.Id = Guid.NewGuid();
        material.OutletId = DefaultOutletId;
        material.CreatedAt = DateTimeOffset.UtcNow;

        material.Outlet = null!;

        _db.RawMaterials.Add(material);

        await _db.SaveChangesAsync();

        return Ok(material);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RawMaterial updated)
    {
        var material = await _db.RawMaterials.FindAsync(id);

        if (material == null)
            return NotFound();

        material.Code = updated.Code;
        material.Name = updated.Name;
        material.Unit = updated.Unit;
        material.ReorderLevel = updated.ReorderLevel;
        material.CurrentStock = updated.CurrentStock;
        material.AverageCost = updated.AverageCost;
        material.TrackExpiry = updated.TrackExpiry;
        material.CategoryId = updated.CategoryId;

        await _db.SaveChangesAsync();

        return Ok(material);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var material = await _db.RawMaterials.FindAsync(id);

        if (material == null)
            return NotFound();

        _db.RawMaterials.Remove(material);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Deleted successfully"
        });
    }
}
