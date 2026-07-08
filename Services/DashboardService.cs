using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetSummaryAsync(Guid? organizationId, Guid? outletId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var sevenDaysAgo = today.AddDays(-6);

        // Today's sales
        var todaySales = await _db.Sales
            .Where(s => (organizationId == null || s.Outlet.OrganizationId == organizationId) 
                     && (outletId == null || s.OutletId == outletId) 
                     && s.SaleDate == today)
            .Select(s => (decimal?)s.Total)
            .SumAsync() ?? 0;

        // Today's purchases
        var todayPurchases = await _db.Purchases
            .Where(p => (organizationId == null || p.Outlet.OrganizationId == organizationId) 
                     && (outletId == null || p.OutletId == outletId) 
                     && p.PurchaseDate == today)
            .Select(p => (decimal?)p.Total)
            .SumAsync() ?? 0;

        // Inventory value
        var stockValue = await _db.RawMaterials
            .Where(m => (organizationId == null || m.Outlet.OrganizationId == organizationId) 
                     && (outletId == null || m.OutletId == outletId))
            .Select(m => (decimal?)(m.CurrentStock * m.AverageCost))
            .SumAsync() ?? 0;

        // Total suppliers
        var totalSuppliers = await _db.Suppliers
            .CountAsync(s => (organizationId == null || s.Outlet.OrganizationId == organizationId) 
                          && (outletId == null || s.OutletId == outletId));

        // Total raw materials
        var totalRawMaterials = await _db.RawMaterials
            .CountAsync(r => (organizationId == null || r.Outlet.OrganizationId == organizationId) 
                          && (outletId == null || r.OutletId == outletId));

        // Low stock items
        var lowStock = await _db.RawMaterials
            .Where(m => (organizationId == null || m.Outlet.OrganizationId == organizationId) 
                     && (outletId == null || m.OutletId == outletId) 
                     && m.CurrentStock <= m.ReorderLevel)
            .Select(m => new
            {
                m.Id,
                m.OutletId,
                m.Name,
                m.CurrentStock,
                m.ReorderLevel,
                m.Unit
            })
            .ToListAsync();

        // Weekly sales chart
        var weekSales = await _db.Sales
            .Where(s => (organizationId == null || s.Outlet.OrganizationId == organizationId) 
                     && (outletId == null || s.OutletId == outletId) 
                     && s.SaleDate >= sevenDaysAgo)
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
            .Where(si => (organizationId == null || si.Sale.Outlet.OrganizationId == organizationId) 
                      && (outletId == null || si.Sale.OutletId == outletId))
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

        // Sales per outlet (for Super Admin / Power Admin)
        var outletSales = new List<object>();
        var lowSalesAlerts = new List<object>();

        if (organizationId.HasValue)
        {
            var outlets = await _db.Outlets
                .Where(o => o.OrganizationId == organizationId.Value && o.Active)
                .ToListAsync();

            var outletIds = outlets.Select(o => o.Id).ToList();

            var salesByOutlet = await _db.Sales
                .Where(s => outletIds.Contains(s.OutletId))
                .GroupBy(s => s.OutletId)
                .Select(g => new
                {
                    OutletId = g.Key,
                    TotalSales = g.Sum(s => s.Total)
                })
                .ToListAsync();

            var salesMap = salesByOutlet.ToDictionary(x => x.OutletId, x => x.TotalSales);

            var outletSalesList = outlets.Select(o => new
            {
                OutletId = o.Id,
                OutletName = o.Name,
                TotalSales = salesMap.ContainsKey(o.Id) ? salesMap[o.Id] : 0
            })
            .OrderBy(x => x.TotalSales) // Ascending order!
            .ToList();

            outletSales = outletSalesList.Cast<object>().ToList();

            // Low sales alert: threshold is 10,000
            lowSalesAlerts = outletSalesList
                .Where(x => x.TotalSales < 10000)
                .Select(x => new
                {
                    x.OutletName,
                    x.TotalSales,
                    Message = x.TotalSales == 0 ? "No sales recorded yet!" : $"Low sales performance (Under ₹10,000)"
                })
                .Cast<object>()
                .ToList();
        }
        // Net Revenue calculation (Sales - Purchases - Expenses)
        var totalSales = await _db.Sales
            .Where(s => (organizationId == null || s.Outlet.OrganizationId == organizationId) 
                     && (outletId == null || s.OutletId == outletId))
            .Select(s => (decimal?)s.Total)
            .SumAsync() ?? 0;

        var totalPurchases = await _db.Purchases
            .Where(p => (organizationId == null || p.Outlet.OrganizationId == organizationId) 
                     && (outletId == null || p.OutletId == outletId))
            .Select(p => (decimal?)p.Total)
            .SumAsync() ?? 0;

        var baseExpenses = await _db.Expenses
            .Where(e => (organizationId == null || e.Outlet.OrganizationId == organizationId) 
                     && (outletId == null || e.OutletId == outletId))
            .Select(e => new {
                TotalBase = e.StaffSalary + e.ShopRent + e.EbBill + e.GasBill + e.MiscExpense,
                CustomTotal = e.OtherExpenses.Sum(o => (decimal?)o.Amount) ?? 0
            })
            .ToListAsync();

        var totalExpenses = baseExpenses.Sum(e => e.TotalBase + e.CustomTotal);
        var netRevenue = totalSales - totalPurchases - totalExpenses;

        return new
        {
            todaySales,
            todayPurchases,

            stockValue,

            totalSuppliers,
            totalRawMaterials,

            lowStockCount = lowStock.Count,
            lowStock,

            weeklyChart = chart,

            topItems,

            outletSales,
            lowSalesAlerts,
            netRevenue
        };
    }
}
