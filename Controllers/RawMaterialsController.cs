using System;
using System.Threading.Tasks;
using KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RawMaterialsController : BaseApiController
{
    private readonly IInventoryService _service;

    public RawMaterialsController(IInventoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? outletId)
    {
        Guid finalOutletId;
        if (IsSuperAdmin())
        {
            if (outletId == null || outletId == Guid.Empty)
            {
                return BadRequest(new { message = "Please select an outlet." });
            }
            finalOutletId = outletId.Value;
        }
        else
        {
            finalOutletId = GetOutletId();
        }

        var items = await _service.GetAllRawMaterialsAsync(finalOutletId);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await _service.GetRawMaterialByIdAsync(id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RawMaterial material)
    {
        Guid outletId;
        if (IsSuperAdmin())
        {
            if (material.OutletId == Guid.Empty)
                return BadRequest(new { message = "Please select an outlet." });
            outletId = material.OutletId;
        }
        else
        {
            outletId = GetOutletId();
        }

        var created = await _service.CreateRawMaterialAsync(outletId, material);
        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RawMaterial updated)
    {
        var result = await _service.UpdateRawMaterialAsync(id, updated);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteRawMaterialAsync(id);

        if (!deleted)
            return NotFound();

        return Ok(new { message = "Deleted successfully" });
    }

    [HttpPost("{id}/adjust")]
    public async Task<IActionResult> Adjust(Guid id, [FromBody] KitchenManagementSystem.API.DTOs.StockAdjustmentDto dto)
    {
        Guid outletId;
        if (IsSuperAdmin())
        {
            var rawMaterial = await _service.GetRawMaterialByIdAsync(id);
            if (rawMaterial == null) return NotFound();
            outletId = rawMaterial.OutletId;
        }
        else
        {
            outletId = GetOutletId();
        }

        try
        {
            var newStock = await _service.AdjustStockAsync(outletId, id, dto.Quantity, dto.Notes);
            if (newStock == null) return NotFound();
            return Ok(new { currentStock = newStock });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
