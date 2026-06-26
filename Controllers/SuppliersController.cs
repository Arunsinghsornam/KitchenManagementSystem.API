using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;

    private static readonly Guid DefaultOutletId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public SuppliersController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/suppliers
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var suppliers = await _db.Suppliers
            .Where(s => s.OutletId == DefaultOutletId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(suppliers);
    }

    // POST api/suppliers
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Supplier supplier)
    {
        try
        {
            supplier.Id = Guid.NewGuid();
            supplier.OutletId = DefaultOutletId;
            supplier.CreatedAt = DateTimeOffset.UtcNow;

            // Prevent EF from trying to insert Outlet
            supplier.Outlet = null!;

            _db.Suppliers.Add(supplier);

            await _db.SaveChangesAsync();

            return Ok(supplier);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                InnerError = ex.InnerException?.Message
            });
        }
    }

    // PUT api/suppliers/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Supplier updated)
    {
        var supplier = await _db.Suppliers.FindAsync(id);

        if (supplier == null)
            return NotFound();

        supplier.Name = updated.Name;
        supplier.ContactPerson = updated.ContactPerson;
        supplier.Mobile = updated.Mobile;
        supplier.GstNumber = updated.GstNumber;
        supplier.Email = updated.Email;
        supplier.Address = updated.Address;
        supplier.Outstanding = updated.Outstanding;

        await _db.SaveChangesAsync();

        return Ok(supplier);
    }

    // DELETE api/suppliers/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);

        if (supplier == null)
            return NotFound();

        _db.Suppliers.Remove(supplier);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Deleted successfully"
        });
    }
}