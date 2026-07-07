using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Services;
using KitchenManagementSystem.API.Data;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecipesController : BaseApiController
{
    private readonly IRecipeService _service;
    private readonly AppDbContext _db;

    public RecipesController(IRecipeService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    // GET api/recipes
    [HttpGet]
    [Authorize(Policy = "RecipeAccess")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? outletId)
    {
        Guid finalOutletId;
        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (outletId == null || outletId == Guid.Empty)
                {
                    return BadRequest(new { message = "Please select an outlet." });
                }
                await ValidateOutletAccessAsync(outletId.Value, _db);
                finalOutletId = outletId.Value;
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

        var menuItems = await _service.GetAllAsync(finalOutletId);

        // Project MenuItems to match expected frontend DTO shape
        var projected = menuItems.Select(m => new
        {
            m.Id,
            m.Name,
            m.Category,
            m.SellingPrice,
            m.Active,
            m.ImageUrl,

            RecipeCost = m.RecipeIngredients.Sum(r =>
                r.Quantity * (r.RawMaterial != null ? r.RawMaterial.AverageCost : 0)),

            Ingredients = m.RecipeIngredients.Select(r => new
            {
                r.Id,
                r.RawMaterialId,
                r.Quantity,
                RawMaterialName = r.RawMaterial != null ? r.RawMaterial.Name : null,
                Unit = r.RawMaterial != null ? r.RawMaterial.Unit : null,
                AverageCost = r.RawMaterial != null ? r.RawMaterial.AverageCost : 0,
                Cost = r.Quantity * (r.RawMaterial != null ? r.RawMaterial.AverageCost : 0)
            })
        });

        return Ok(projected);
    }

    // POST api/recipes
    [HttpPost]
    [Authorize(Policy = "RecipeAccess")]
    public async Task<IActionResult> Create([FromBody] CreateRecipeDto dto, [FromQuery] Guid? outletId)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            Guid finalOutletId;
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (outletId == null || outletId == Guid.Empty)
                    return BadRequest(new { message = "Please select an outlet." });
                finalOutletId = outletId.Value;
                await ValidateOutletAccessAsync(finalOutletId, _db);
            }
            else
            {
                finalOutletId = GetOutletId();
            }

            var menuItem = await _service.CreateAsync(finalOutletId, dto);

            return Ok(new
            {
                menuItem.Id,
                menuItem.Name,
                menuItem.Category,
                menuItem.SellingPrice,
                menuItem.ImageUrl
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Duplicate ingredients are not allowed." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT api/recipes/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = "RecipeAccess")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRecipeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var menuItem = await _db.MenuItems.FindAsync(id);
            if (menuItem == null)
                return NotFound();

            await ValidateOutletAccessAsync(menuItem.OutletId, _db);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (!updated)
                return NotFound();

            return Ok();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Duplicate ingredients are not allowed." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE api/recipes/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "RecipeAccess")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var menuItem = await _db.MenuItems.FindAsync(id);
            if (menuItem == null)
                return NotFound();

            await ValidateOutletAccessAsync(menuItem.OutletId, _db);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return Ok(new { message = "Recipe archived successfully" });
    }

    // POST api/recipes/upload
    [HttpPost("upload")]
    [Authorize(Policy = "RecipeAccess")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "recipes");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var relativeUrl = $"/uploads/recipes/{uniqueFileName}";
        return Ok(new { imageUrl = $"http://localhost:5253{relativeUrl}" });
    }
}