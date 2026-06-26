using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly AppDbContext _db;
    private static readonly Guid DefaultOutletId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public RecipesController(AppDbContext db) => _db = db;

    // GET api/recipes — all menu items with ingredients and cost
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.MenuItems
            .Where(m =>
    m.OutletId == DefaultOutletId &&
    m.Active)
            .OrderBy(m => m.Name)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Category,
                m.SellingPrice,
                m.Active,

                RecipeCost = m.RecipeIngredients.Sum(r =>
                    r.Quantity * r.RawMaterial.AverageCost),

                Ingredients = m.RecipeIngredients.Select(r => new
                {
                    r.Id,
                    r.RawMaterialId,
                    r.Quantity,
                    RawMaterialName = r.RawMaterial.Name,
                    Unit = r.RawMaterial.Unit,
                    AverageCost = r.RawMaterial.AverageCost,
                    Cost = r.Quantity * r.RawMaterial.AverageCost
                })
            })
            .ToListAsync();

        return Ok(items);
    }

    // POST api/recipes — create menu item + ingredients
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var menuItem = new MenuItem
            {
                Id = Guid.NewGuid(),
                OutletId = DefaultOutletId,
                Name = dto.Name,
                Category = dto.Category,
                SellingPrice = dto.SellingPrice,
                Active = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.MenuItems.Add(menuItem);

            foreach (var ing in dto.Ingredients)
            {
                _db.RecipeIngredients.Add(new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = menuItem.Id,
                    RawMaterialId = ing.RawMaterialId,
                    Quantity = ing.Quantity
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                menuItem.Id,
                menuItem.Name,
                menuItem.Category,
                menuItem.SellingPrice
            });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new
            {
                message = "Duplicate ingredients are not allowed."
            });
        }
        catch (Exception)
        {
            return BadRequest(new
            {
                message = "Failed to create recipe."
            });
        }
    }

    // PUT api/recipes/{id} — update menu item + replace ingredients
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRecipeDto dto)
    {
        // ADD THIS
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var menuItem = await _db.MenuItems.FindAsync(id);

            if (menuItem == null)
                return NotFound();

            menuItem.Name = dto.Name;
            menuItem.Category = dto.Category;
            menuItem.SellingPrice = dto.SellingPrice;

            var oldIngredients = await _db.RecipeIngredients
                .Where(r => r.MenuItemId == id)
                .ToListAsync();

            _db.RecipeIngredients.RemoveRange(oldIngredients);

            foreach (var ing in dto.Ingredients)
            {
                _db.RecipeIngredients.Add(new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = id,
                    RawMaterialId = ing.RawMaterialId,
                    Quantity = ing.Quantity
                });
            }

            await _db.SaveChangesAsync();

            return Ok();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new
            {
                message = "Duplicate ingredients are not allowed."
            });
        }
        catch (Exception)
        {
            return BadRequest(new
            {
                message = "Failed to update recipe."
            });
        }
    }
    // DELETE api/recipes/{id} — delete menu item
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var menuItem = await _db.MenuItems
            .FirstOrDefaultAsync(x => x.Id == id);

        if (menuItem == null)
            return NotFound();

        menuItem.Active = false;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Recipe archived successfully"
        });
    }

    public class CreateRecipeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal SellingPrice { get; set; }
        public List<IngredientDto> Ingredients { get; set; } = [];
    }

    public class IngredientDto
    {
        public Guid RawMaterialId { get; set; }
        public decimal Quantity { get; set; }
    }
}
