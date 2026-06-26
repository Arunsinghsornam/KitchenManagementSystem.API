using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitchenManagementSystem.API.Data;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;


private static readonly Guid DefaultOutletId =
    Guid.Parse("00000000-0000-0000-0000-000000000001");

    public DashboardController(AppDbContext db) => _db = db;

    // GET api/dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var sevenDaysAgo = today.AddDays(-6);

        // Today's sales
        var todaySales = await _db.Sales
            .Where(s => s.OutletId == DefaultOutletId &&
                        s.SaleDate == today)
            .Select(s => (decimal?)s.Total)
            .SumAsync() ?? 0;

        // Today's purchases
        var todayPurchases = await _db.Purchases
            .Where(p => p.OutletId == DefaultOutletId &&
                        p.PurchaseDate == today)
            .Select(p => (decimal?)p.Total)
            .SumAsync() ?? 0;

        // Inventory value
        var stockValue = await _db.RawMaterials
            .Where(m => m.OutletId == DefaultOutletId)
            .Select(m => (decimal?)(m.CurrentStock * m.AverageCost))
            .SumAsync() ?? 0;

        // Total suppliers
        var totalSuppliers = await _db.Suppliers
            .CountAsync(s => s.OutletId == DefaultOutletId);

        // Total raw materials
        var totalRawMaterials = await _db.RawMaterials
            .CountAsync(r => r.OutletId == DefaultOutletId);

        // Low stock items
        var lowStock = await _db.RawMaterials
            .Where(m => m.OutletId == DefaultOutletId &&
                        m.CurrentStock <= m.ReorderLevel)
            .Select(m => new
            {
                m.Name,
                m.CurrentStock,
                m.ReorderLevel,
                m.Unit
            })
            .ToListAsync();

        // Weekly sales chart
        var weekSales = await _db.Sales
            .Where(s => s.OutletId == DefaultOutletId &&
                        s.SaleDate >= sevenDaysAgo)
            .GroupBy(s => s.SaleDate)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Sum(s => s.Total)
            })
            .ToListAsync();

        var chart = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = today.AddDays(-6 + i);

                var found = weekSales
                    .FirstOrDefault(w => w.Date == date);

                return new
                {
                    Label = date.ToString("ddd"),
                    Date = date.ToString("yyyy-MM-dd"),
                    Total = found?.Total ?? 0
                };
            })
            .ToList();

        // Top selling items
        var topItems = await _db.SalesItems
            .Where(si => si.Sale.OutletId == DefaultOutletId)
            .GroupBy(si => si.MenuItemId)
            .Select(g => new
            {
                MenuItemId = g.Key,
                TotalQty = g.Sum(si => si.Quantity),
                TotalRevenue = g.Sum(si => si.LineTotal)
            })
            .OrderByDescending(x => x.TotalQty)
            .Take(5)
            .Join(
                _db.MenuItems,
                top => top.MenuItemId,
                menu => menu.Id,
                (top, menu) => new
                {
                    menu.Name,
                    Qty = top.TotalQty,
                    Revenue = top.TotalRevenue
                })
            .ToListAsync();

        return Ok(new
        {
            todaySales,
            todayPurchases,

            stockValue,

            totalSuppliers,
            totalRawMaterials,

            lowStockCount = lowStock.Count,
            lowStock,

            weeklyChart = chart,

            topItems
        });
    }


}
