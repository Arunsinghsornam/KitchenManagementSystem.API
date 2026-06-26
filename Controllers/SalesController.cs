using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _db;
    private static readonly Guid DefaultOutletId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public SalesController(AppDbContext db) => _db = db;

    // GET api/sales — list recent sales
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sales = await _db.Sales
            .Where(s => s.OutletId == DefaultOutletId)
            .OrderByDescending(s => s.SaleDate)
            .Take(100)
            .Select(s => new
            {
                s.Id,
                s.SaleDate,
                s.Channel,
                s.Subtotal,
                s.Discount,
                s.Total
            })
            .ToListAsync();
        return Ok(sales);
    }

    // GET api/sales/stock-check — check if stock is enough before saving
    [HttpGet("stock-check")]
    public async Task<IActionResult> StockCheck([FromQuery] Guid menuItemId,
                                                [FromQuery] decimal quantity)
    {
        var ingredients = await _db.RecipeIngredients
            .Where(r => r.MenuItemId == menuItemId)
            .Include(r => r.RawMaterial)
            .ToListAsync();

        var shortages = ingredients
            .Where(r => r.RawMaterial.CurrentStock < r.Quantity * quantity)
            .Select(r => new
            {
                Item = r.RawMaterial.Name,
                Have = r.RawMaterial.CurrentStock,
                Need = r.Quantity * quantity,
                Unit = r.RawMaterial.Unit
            })
            .ToList();

        return Ok(new { hasShortage = shortages.Any(), shortages });
    }

    // POST api/sales — record a new sale
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }


        try
        {
            if (dto.Items == null || !dto.Items.Any())
            {
                return BadRequest(new
                {
                    message = "Please add at least one sale item."
                });
            }

            var duplicateItems = dto.Items
                .GroupBy(x => x.MenuItemId)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateItems.Any())
            {
                return BadRequest(new
                {
                    message = "Duplicate menu items are not allowed."
                });
            }

            foreach (var line in dto.Items)
            {
                var ingredients = await _db.RecipeIngredients
                    .Where(r => r.MenuItemId == line.MenuItemId)
                    .Include(r => r.RawMaterial)
                    .ToListAsync();

                foreach (var ing in ingredients)
                {
                    var needed = ing.Quantity * line.Quantity;

                    if (ing.RawMaterial.CurrentStock < needed)
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Insufficient stock: {ing.RawMaterial.Name}. Need {needed} {ing.RawMaterial.Unit}, Available {ing.RawMaterial.CurrentStock}."
                        });
                    }
                }
            }

            var subtotal = dto.Items.Sum(i => i.Quantity * i.UnitPrice);
            var total = Math.Max(0, subtotal - dto.Discount);

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                OutletId = DefaultOutletId,
                SaleDate = dto.SaleDate,
                Channel = dto.Channel,
                Subtotal = subtotal,
                Discount = dto.Discount,
                Total = total,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Sales.Add(sale);

            await _db.SaveChangesAsync();

            foreach (var line in dto.Items)
            {
                _db.SalesItems.Add(new SaleItem
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    MenuItemId = line.MenuItemId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    LineTotal = line.Quantity * line.UnitPrice
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                sale.Id,
                message = "Sale recorded successfully."
            });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new
            {
                message = "Database error while saving sale."
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                message = "Unexpected error occurred while saving sale."
            });
        }


    }


    public class CreateSaleDto
    {
        public DateOnly SaleDate { get; set; }
        public string Channel { get; set; } = "OUTLET";
        public decimal Discount { get; set; }
        public List<SaleLineDto> Items { get; set; } = [];
    }

    public class SaleLineDto
    {
        public Guid MenuItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
