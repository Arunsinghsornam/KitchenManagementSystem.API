using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PLReportController : ControllerBase
{
    private readonly IPLReportService _service;

    public PLReportController(
        IPLReportService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        Guid outletId,
        DateTime dateFrom,
        DateTime dateTo)
    {
        var result =
            await _service.GetReport(
                outletId,
                dateFrom,
                dateTo);

        return Ok(result);
    }
}
