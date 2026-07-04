using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : BaseApiController
{
    private readonly ISupplierService _service;
    private readonly AppDbContext _db;

    public SuppliersController(ISupplierService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    // GET api/suppliers
    [HttpGet]
    [Authorize(Policy = "StoreOperations")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? outletId, [FromQuery] Guid? organizationId)
    {
        Guid? finalOutletId;
        Guid? orgId = IsPowerAdmin() ? (organizationId ?? null) : GetOrganizationId();

        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (outletId.HasValue)
                {
                    await ValidateOutletAccessAsync(outletId.Value, _db);
                }
                finalOutletId = outletId;
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

        var suppliers = await _service.GetAllAsync(orgId, finalOutletId);

        var projected = suppliers.Select(s => new
        {
            s.Id,
            s.OutletId,
            s.Name,
            s.ContactPerson,
            s.Mobile,
            s.GstNumber,
            s.Email,
            s.Address,
            s.Outstanding,
            Outlet = s.Outlet != null ? new { s.Outlet.Id, s.Outlet.Name } : null
        });

        return Ok(new
        {
            success = true,
            data = projected
        });
    }

    // POST api/suppliers
    [HttpPost]
    [Authorize(Policy = "StoreOperations")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
    {
        try
        {
            Guid defaultOutletId = Guid.Empty;
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (dto.OutletId == Guid.Empty)
                {
                    return BadRequest(new { success = false, message = "Please select an outlet." });
                }
                defaultOutletId = dto.OutletId;
                await ValidateOutletAccessAsync(defaultOutletId, _db);
            }
            else
            {
                defaultOutletId = GetOutletId();
            }

            var supplier = await _service.CreateAsync(defaultOutletId, dto);
            return Ok(new
            {
                success = true,
                data = supplier
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // PUT api/suppliers/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = "StoreOperations")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto dto)
    {
        Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();
        Guid? outletId = IsPowerAdmin() || IsSuperAdmin() ? null : GetOutletId();
        
        var supplier = await _service.UpdateAsync(orgId, outletId, id, dto);

        if (supplier == null)
            return NotFound(new { success = false, message = "Supplier not found or access denied" });

        return Ok(new
        {
            success = true,
            data = supplier
        });
    }

    // DELETE api/suppliers/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "StoreOperations")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();
        Guid? outletId = IsPowerAdmin() || IsSuperAdmin() ? null : GetOutletId();

        var deleted = await _service.DeleteAsync(orgId, outletId, id);

        if (!deleted)
            return NotFound(new { success = false, message = "Supplier not found or access denied" });

        return Ok(new
        {
            success = true,
            message = "Deleted successfully"
        });
    }
}