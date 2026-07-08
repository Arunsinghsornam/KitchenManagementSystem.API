using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> Get()
    {
        var role = GetRole();
        if (!role.Equals("super_admin", System.StringComparison.OrdinalIgnoreCase) && 
            !role.Equals("power_admin", System.StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        System.Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();
        var notifications = await _service.GetRecentNotificationsAsync(role, orgId);
        return Ok(notifications);
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkRead()
    {
        var role = GetRole();
        if (!role.Equals("super_admin", System.StringComparison.OrdinalIgnoreCase) && 
            !role.Equals("power_admin", System.StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        System.Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();
        await _service.MarkAllAsReadAsync(role, orgId);
        return NoContent();
    }
}
