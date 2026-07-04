using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Data;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController : BaseApiController
{
    private readonly IPurchaseService _service;
    private readonly AppDbContext _db;

    public PurchasesController(IPurchaseService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    // GET api/purchases
    [HttpGet]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? outletId)
    {
        Guid? finalOutletId;
        Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();

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

        var purchases = await _service.GetAllAsync(orgId, finalOutletId);

        // Project properties to match original output shape
        var projected = purchases.Select(p => new
        {
            p.Id,
            p.OutletId,
            p.InvoiceNumber,
            p.PurchaseDate,
            p.Subtotal,
            p.GstAmount,
            p.Total,
            SupplierName = p.Supplier?.Name
        });

        return Ok(projected);
    }

    // POST api/purchases
    [HttpPost]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseDto dto)
    {
        try
        {
            Guid outletId;
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (dto.OutletId == null || dto.OutletId == Guid.Empty)
                {
                    return BadRequest("Please select an outlet.");
                }
                outletId = dto.OutletId.Value;
                await ValidateOutletAccessAsync(outletId, _db);
            }
            else
            {
                outletId = GetOutletId();
            }

            var purchase = await _service.CreateAsync(outletId, dto);

            return Ok(new
            {
                purchase.Id,
                message = "Purchase recorded successfully. Stock and average cost updated."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}