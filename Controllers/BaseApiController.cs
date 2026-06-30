using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using KitchenManagementSystem.API.Data;
using System.Threading.Tasks;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid GetOutletId()
    {
        // power_admin has no outletId — callers should use IsPowerAdmin() check before this
        if (IsPowerAdmin()) return Guid.Empty;

        var outletId = User.FindFirstValue("outletId");

        if (string.IsNullOrWhiteSpace(outletId))
            throw new UnauthorizedAccessException("OutletId missing in token");

        return Guid.Parse(outletId);
    }

    protected Guid? GetOutletIdOrNull()
    {
        var outletId = User.FindFirstValue("outletId");

        if (string.IsNullOrWhiteSpace(outletId))
            return null;

        return Guid.Parse(outletId);
    }

    protected string GetRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    protected bool IsSuperAdmin()
    {
        return GetRole().Equals("super_admin", StringComparison.OrdinalIgnoreCase);
    }

    protected bool IsPowerAdmin()
    {
        return GetRole().Equals("power_admin", StringComparison.OrdinalIgnoreCase);
    }

    protected Guid GetOrganizationId()
    {
        // power_admin has no organizationId — they span all orgs
        if (IsPowerAdmin()) return Guid.Empty;

        var orgId = User.FindFirstValue("organizationId");

        if (string.IsNullOrWhiteSpace(orgId))
            throw new UnauthorizedAccessException("OrganizationId missing in token");

        return Guid.Parse(orgId);
    }

    protected Guid? GetOrganizationIdOrNull()
    {
        var orgId = User.FindFirstValue("organizationId");

        if (string.IsNullOrWhiteSpace(orgId))
            return null;

        return Guid.Parse(orgId);
    }

    protected async Task ValidateOutletAccessAsync(Guid outletId, AppDbContext db)
    {
        if (IsPowerAdmin()) return; // Power admin has access to all outlets

        var userOrgId = GetOrganizationId();
        var outlet = await db.Outlets.FindAsync(outletId);
        if (outlet == null || outlet.OrganizationId != userOrgId)
        {
            throw new UnauthorizedAccessException("You do not have access to this outlet.");
        }
    }

    protected Guid GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("UserId missing in token");

        return Guid.Parse(userId);
    }
}