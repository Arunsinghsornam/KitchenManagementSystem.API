using KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdmin")]
public class OutletsController : BaseApiController
{
    private readonly IOutletService _service;

    public OutletsController(IOutletService service)
    {
        _service = service;
    }

    // GET: api/Outlets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Outlet>>> GetOutlets()
    {
        Guid? orgId = IsPowerAdmin() ? null : GetOrganizationId();
        var outlets = await _service.GetAllAsync(orgId);
        return Ok(outlets);
    }

    // GET: api/Outlets/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Outlet>> GetOutlet(Guid id)
    {
        var outlet = await _service.GetByIdAsync(id);

        if (outlet == null)
            return NotFound();

        if (!IsPowerAdmin() && outlet.OrganizationId != GetOrganizationId())
            return Forbid();

        return Ok(outlet);
    }

    // POST: api/Outlets
    [HttpPost]
    public async Task<ActionResult<Outlet>> CreateOutlet(Outlet outlet)
    {
        if (!IsPowerAdmin())
        {
            outlet.OrganizationId = GetOrganizationId();
        }
        var created = await _service.CreateAsync(outlet);
        return CreatedAtAction(nameof(GetOutlet), new { id = created.Id }, created);
    }

    // PUT: api/Outlets/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOutlet(Guid id, Outlet updatedOutlet)
    {
        if (id != updatedOutlet.Id)
            return BadRequest();

        var existing = await _service.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        if (!IsPowerAdmin() && existing.OrganizationId != GetOrganizationId())
            return Forbid();

        // Secure OrganizationId assignment
        updatedOutlet.OrganizationId = existing.OrganizationId;

        var updated = await _service.UpdateAsync(id, updatedOutlet);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // DELETE: api/Outlets/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOutlet(Guid id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        if (!IsPowerAdmin() && existing.OrganizationId != GetOrganizationId())
            return Forbid();

        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}