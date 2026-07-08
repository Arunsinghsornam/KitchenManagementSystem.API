using System;
using System.Linq;
using System.Threading.Tasks;
using KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _service;
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public InventoryController(IInventoryService service, AppDbContext db, INotificationService notificationService)
    {
        _service = service;
        _db = db;
        _notificationService = notificationService;
    }

    // GET api/inventory — list all raw materials
    [HttpGet]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? outletId)
    {
        Guid finalOutletId;
        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (outletId == null || outletId == Guid.Empty)
                {
                    return BadRequest(new { message = "Please select an outlet." });
                }
                await ValidateOutletAccessAsync(outletId.Value, _db);
                finalOutletId = outletId.Value;
            }
            else
            {
                finalOutletId = GetOutletId();
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var materials = await _service.GetAllRawMaterialsAsync(finalOutletId);

        // Project to match expected client DTO shape
        var projected = materials.Select(m => new
        {
            m.Id,
            m.Code,
            m.Name,
            m.Unit,
            m.ReorderLevel,
            m.CurrentStock,
            m.AverageCost,
            m.CategoryId,
            CategoryName = m.Category != null ? m.Category.Name : null
        });

        return Ok(projected);
    }

    // POST api/inventory — add new raw material
    [HttpPost]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> Create([FromBody] RawMaterial material)
    {
        Guid outletId;
        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (material.OutletId == Guid.Empty)
                    return BadRequest(new { message = "Please select an outlet." });
                outletId = material.OutletId;
                await ValidateOutletAccessAsync(outletId, _db);
            }
            else
            {
                outletId = GetOutletId();
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var created = await _service.CreateRawMaterialAsync(outletId, material);

        await _notificationService.AddNotificationAsync(
            GetUserId(),
            IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
            outletId,
            $"Added new raw material '{created.Name}' ({created.Code})");

        return Ok(created);
    }

    // PUT api/inventory/{id} — update raw material
    [HttpPut("{id}")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RawMaterial updated)
    {
        try
        {
            var existing = await _service.GetRawMaterialByIdAsync(id);
            if (existing == null)
                return NotFound();

            await ValidateOutletAccessAsync(existing.OutletId, _db);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var result = await _service.UpdateRawMaterialAsync(id, updated);

        if (result == null)
            return NotFound();

        await _notificationService.AddNotificationAsync(
            GetUserId(),
            IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
            result.OutletId,
            $"Updated raw material '{result.Name}' ({result.Code})");

        return Ok(result);
    }

    // DELETE api/inventory/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _service.GetRawMaterialByIdAsync(id);
        if (existing == null)
            return NotFound();

        var matName = existing.Name;
        var matCode = existing.Code;
        var matOutletId = existing.OutletId;

        try
        {
            await ValidateOutletAccessAsync(existing.OutletId, _db);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var deleted = await _service.DeleteRawMaterialAsync(id);

        if (!deleted)
            return NotFound();

        await _notificationService.AddNotificationAsync(
            GetUserId(),
            IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
            matOutletId,
            $"Deleted raw material '{matName}' ({matCode})");

        return Ok(new { message = "Deleted" });
    }

    // POST api/inventory/{id}/adjust
    [HttpPost("{id}/adjust")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> Adjust(Guid id, [FromBody] AdjustStockDto dto)
    {
        try
        {
            Guid outletId;
            var mat = await _service.GetRawMaterialByIdAsync(id);
            if (mat == null)
                return NotFound();

            try
            {
                if (IsPowerAdmin() || IsSuperAdmin())
                {
                    outletId = mat.OutletId;
                    await ValidateOutletAccessAsync(outletId, _db);
                }
                else
                {
                    outletId = GetOutletId();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }

            var newStock = await _service.AdjustStockAsync(outletId, id, dto.Quantity, dto.Notes);

            if (newStock == null)
                return NotFound();

            await _notificationService.AddNotificationAsync(
                GetUserId(),
                IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
                outletId,
                $"Adjusted stock for '{mat.Name}' by {dto.Quantity:N2} {mat.Unit} (New stock: {newStock:N2} {mat.Unit}). Notes: {dto.Notes}");

            return Ok(new { newStock });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET api/inventory/{id}/ledger
    [HttpGet("{id}/ledger")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> GetLedger(Guid id)
    {
        var mat = await _service.GetRawMaterialByIdAsync(id);
        if (mat == null)
            return NotFound();

        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                await ValidateOutletAccessAsync(mat.OutletId, _db);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var entries = await _db.StockLedger
            .Where(l => l.RawMaterialId == id)
            .OrderByDescending(l => l.TxnDate)
            .Select(l => new
            {
                l.Id,
                l.TxnDate,
                l.TxnType,
                l.QuantityIn,
                l.QuantityOut,
                l.BalanceAfter,
                l.UnitCost,
                l.Notes
            })
            .ToListAsync();

        return Ok(entries);
    }

    // GET api/inventory/categories
    [HttpGet("categories")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> GetCategories([FromQuery] Guid? outletId)
    {
        Guid finalOutletId;
        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (outletId == null || outletId == Guid.Empty)
                    return BadRequest(new { message = "Please select an outlet." });
                await ValidateOutletAccessAsync(outletId.Value, _db);
                finalOutletId = outletId.Value;
            }
            else
            {
                finalOutletId = GetOutletId();
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var categories = await _service.GetCategoriesAsync(finalOutletId);
        return Ok(categories);
    }

    // POST api/inventory/categories
    [HttpPost("categories")]
    [Authorize(Policy = "InventoryAccess")]
    public async Task<IActionResult> CreateCategory([FromBody] Category cat)
    {
        Guid outletId;
        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (cat.OutletId == Guid.Empty)
                    return BadRequest(new { message = "Please select an outlet." });
                outletId = cat.OutletId;
                await ValidateOutletAccessAsync(outletId, _db);
            }
            else
            {
                outletId = GetOutletId();
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var created = await _service.CreateCategoryAsync(outletId, cat);
        return Ok(created);
    }
}

// DTO
public class AdjustStockDto
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}