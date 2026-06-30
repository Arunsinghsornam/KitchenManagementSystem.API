using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Manager")]
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
        Guid outletId,
        DateTime dateFrom,
        DateTime dateTo)
    {
        try
        {
            await ValidateOutletAccessAsync(outletId, _db);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var result =
            await _service.GetReport(
                outletId,
                dateFrom,
                dateTo);

        return Ok(result);
    }
}
