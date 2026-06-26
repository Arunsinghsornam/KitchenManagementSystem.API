using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;


private static readonly Guid DefaultOutletId =
    Guid.Parse("00000000-0000-0000-0000-000000000001");

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/categories
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories
            .Where(x => x.OutletId == DefaultOutletId)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(categories);
    }

    // POST api/categories
    [HttpPost]
    public async Task<IActionResult> Create(Category category)
    {
        category.Name = category.Name.Trim();

        bool exists = await _db.Categories.AnyAsync(x =>
            x.OutletId == DefaultOutletId &&
            x.Name.Trim().ToLower() == category.Name.ToLower());

        if (exists)
        {
            return BadRequest(new
            {
                message = "Category already exists"
            });
        }

        category.Id = Guid.NewGuid();
        category.OutletId = DefaultOutletId;
        category.CreatedAt = DateTimeOffset.UtcNow;

        category.Outlet = null!;

        _db.Categories.Add(category);

        await _db.SaveChangesAsync();

        return Ok(category);
    }

    // PUT api/categories/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Category updated)
    {
        var category = await _db.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        updated.Name = updated.Name.Trim();

        bool exists = await _db.Categories.AnyAsync(x =>
            x.Id != id &&
            x.OutletId == DefaultOutletId &&
            x.Name.Trim().ToLower() == updated.Name.ToLower());

        if (exists)
        {
            return BadRequest(new
            {
                message = "Category already exists"
            });
        }

        category.Name = updated.Name;

        await _db.SaveChangesAsync();

        return Ok(category);
    }

    // DELETE api/categories/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        bool isUsed = await _db.RawMaterials
            .AnyAsync(r => r.CategoryId == id);

        if (isUsed)
        {
            return BadRequest(new
            {
                message = "Cannot delete category because it is used by raw materials."
            });
        }

        _db.Categories.Remove(category);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Deleted successfully"
        });
    }


}
