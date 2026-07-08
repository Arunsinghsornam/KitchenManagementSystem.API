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
[Authorize(Policy = "StoreOperations")]
public class SalesController : BaseApiController
{
    private readonly ISalesService _service;
    private readonly AppDbContext _db;

    public SalesController(ISalesService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    // GET api/sales — list recent sales
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? outletId, 
        [FromQuery] Guid? organizationId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
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

        var sales = await _service.GetAllAsync(orgId, finalOutletId, fromDate, toDate);

        var projected = sales.Select(s => new
        {
            s.Id,
            s.OutletId,
            s.SaleDate,
            s.Channel,
            s.Subtotal,
            s.Discount,
            s.Total,
            SaleItems = s.Items.Select(si => new
            {
                si.MenuItemId,
                si.Quantity,
                si.UnitPrice
            })
        });

        return Ok(projected);
    }

    // GET api/sales/stock-check
    [HttpGet("stock-check")]
    public async Task<IActionResult> StockCheck(
        [FromQuery] Guid menuItemId,
        [FromQuery] decimal quantity)
    {
        var (hasShortage, shortages) = await _service.StockCheckAsync(menuItemId, quantity);

        return Ok(new
        {
            hasShortage,
            shortages
        });
    }

    // POST api/sales
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            Guid outletId;
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (dto.OutletId == null || dto.OutletId == Guid.Empty)
                {
                    return BadRequest(new { message = "Please select an outlet." });
                }
                outletId = dto.OutletId.Value;
                await ValidateOutletAccessAsync(outletId, _db);
            }
            else
            {
                outletId = GetOutletId();
            }

            var sale = await _service.CreateAsync(outletId, dto);

            return Ok(new
            {
                sale.Id,
                message = "Sale recorded successfully."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                message = "Unexpected error occurred while saving sale."
            });
        }
    }
}
