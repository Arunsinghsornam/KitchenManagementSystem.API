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

    public CategoriesController(ICategoryService service, AppDbContext db)
    {
        _service = service;
        _db = db;
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
            // For safety, load category and validate outlet access
            var category = await _db.Categories.FindAsync(id);
            if (category != null)
            {
                await ValidateOutletAccessAsync(category.OutletId, _db);
            }

            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound();

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