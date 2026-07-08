using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "PLAccess")]
public class PLReportController : BaseApiController
{
    private readonly IPLReportService _service;
    private readonly AppDbContext _db;

    public PLReportController(
        IPLReportService service,
        AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? outletId,
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo)
    {
        Guid? finalOutletId = outletId;
        Guid? orgId = IsPowerAdmin() ? (organizationId ?? null) : GetOrganizationId();

        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (outletId.HasValue)
                {
                    await ValidateOutletAccessAsync(outletId.Value, _db);
                }
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

        var result = await _service.GetReport(
            orgId,
            finalOutletId,
            dateFrom,
            dateTo);

        return Ok(result);
    }
}
