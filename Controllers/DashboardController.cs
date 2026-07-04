using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Data;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AnyStaff")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _service;
    private readonly AppDbContext _db;

    public DashboardController(IDashboardService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    // GET api/dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid? outletId)
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

        var summary = await _service.GetSummaryAsync(orgId, finalOutletId);
        return Ok(summary);
    }
}
