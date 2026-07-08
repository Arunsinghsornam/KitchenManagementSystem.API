using Microsoft.AspNetCore.Mvc;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Data;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AnyStaff")]
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _service;
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public CategoriesController(ICategoryService service, AppDbContext db, INotificationService notificationService)
    {
        _service = service;
        _db = db;
        _notificationService = notificationService;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? organizationId)
    {
        var orgId = IsPowerAdmin() ? (organizationId ?? null) : GetOrganizationIdOrNull();
        var categories = await _service.GetAllAsync(orgId);
        return Ok(categories);
    }

    // POST: api/categories
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        try
        {
            await ValidateOutletAccessAsync(dto.OutletId, _db);
            var category = await _service.CreateAsync(dto);
            
            await _notificationService.AddNotificationAsync(
                GetUserId(),
                IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
                dto.OutletId,
                $"Added new category '{category.Name}'");

            return Ok(category);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/categories/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryDto dto)
    {
        try
        {
            await ValidateOutletAccessAsync(dto.OutletId, _db);
            var category = await _service.UpdateAsync(id, dto);
            if (category == null)
                return NotFound();

            await _notificationService.AddNotificationAsync(
                GetUserId(),
                IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
                dto.OutletId,
                $"Updated category details for '{category.Name}'");

            return Ok(category);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/categories/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            var catName = category.Name;
            var catOutletId = category.OutletId;

            await ValidateOutletAccessAsync(category.OutletId, _db);

            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound();

            await _notificationService.AddNotificationAsync(
                GetUserId(),
                IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
                catOutletId,
                $"Deleted category '{catName}'");

            return Ok(new { message = "Category deleted successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}