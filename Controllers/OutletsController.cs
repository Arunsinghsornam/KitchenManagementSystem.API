namespace KitchenManagementSystem.API.Controllers;

using global::KitchenManagementSystem.API.Data;
using global::KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("api/[controller]")]
public class OutletsController : ControllerBase
{
    private readonly AppDbContext _context;

    public OutletsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Outlet>>> GetOutlets()
    {
        return await _context.Outlets
            .OrderBy(o => o.Name)
            .ToListAsync();
    }
}
