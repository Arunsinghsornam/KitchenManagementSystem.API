using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;


namespace KitchenManagementSystem.API.Services;

public class PLReportService : IPLReportService
{
    private readonly AppDbContext _db;

    public PLReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PLReport> GetReport(
    Guid outletId,
    DateTime from,
    DateTime to)
    {
        var sales = await _db.Sales
            .Where(s =>
                s.OutletId == outletId &&
                s.SaleDate >= DateOnly.FromDateTime(from) &&
                s.SaleDate <= DateOnly.FromDateTime(to))
            .ToListAsync();
        var purchases = await _db.Purchases
    .Where(p =>
        p.OutletId == outletId &&
        p.PurchaseDate >= DateOnly.FromDateTime(from) &&
        p.PurchaseDate <= DateOnly.FromDateTime(to))
    .ToListAsync();

        var totalPurchaseSpend =
            purchases.Sum(x => x.Total);

        var dailyPurchaseSpend =
            purchases
                .GroupBy(x => x.PurchaseDate)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Total));

        var totalRevenue = sales.Sum(x => x.Total);
        var totalDiscount = sales.Sum(x => x.Discount);
        var totalGrossRevenue = sales.Sum(x => x.Subtotal);

        var totalOrders = sales.Count;
        var saleIds = sales
    .Select(x => x.Id)
    .ToList();

        var saleItems = await _db.SalesItems
            .Where(x => saleIds.Contains(x.SaleId))
            .ToListAsync();
        var menuItemIds = saleItems
    .Select(x => x.MenuItemId)
    .Distinct()
    .ToList();

        var recipes = await _db.RecipeIngredients
            .Where(x => menuItemIds.Contains(x.MenuItemId))
            .ToListAsync();
        var rawMaterialIds = recipes
    .Select(x => x.RawMaterialId)
    .Distinct()
    .ToList();

        var rawMaterials = await _db.RawMaterials
            .Where(x => rawMaterialIds.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => x.AverageCost);

        Console.WriteLine($"Sales: {sales.Count}");
        Console.WriteLine($"SaleItems: {saleItems.Count}");
        Console.WriteLine($"Recipes: {recipes.Count}");
        Console.WriteLine($"RawMaterials: {rawMaterials.Count}");
        decimal totalCogs = 0;
        var dailyCogs = new Dictionary<DateOnly, decimal>();
        var monthlyCogs =
    new Dictionary<string, decimal>();
        var channelCogs =
    new Dictionary<string, decimal>();
        foreach (var saleItem in saleItems)
        {
            var sale = sales.First(x => x.Id == saleItem.SaleId);

            var recipeLines =
                recipes.Where(r =>
                    r.MenuItemId == saleItem.MenuItemId);

            foreach (var recipe in recipeLines)
            {
                if (rawMaterials.TryGetValue(
                    recipe.RawMaterialId,
                    out var averageCost))
                {
                    var itemCogs =
                        recipe.Quantity *
                        saleItem.Quantity *
                        averageCost;

                    totalCogs += itemCogs;

                    if (!dailyCogs.ContainsKey(sale.SaleDate))
                    {
                        dailyCogs[sale.SaleDate] = 0;
                    }

                    dailyCogs[sale.SaleDate] += itemCogs;
                    var monthKey =
    $"{sale.SaleDate.Year}-{sale.SaleDate.Month}";

                    if (!monthlyCogs.ContainsKey(monthKey))
                    {
                        monthlyCogs[monthKey] = 0;
                    }

                    monthlyCogs[monthKey] += itemCogs;
                    var channelKey =
    sale.Channel ?? "UNKNOWN";

                    if (!channelCogs.ContainsKey(channelKey))
                    {
                        channelCogs[channelKey] = 0;
                    }

                    channelCogs[channelKey] += itemCogs;
                }
            }
        }
        var grossProfit =
    totalRevenue - totalCogs;
        var marginPct =
    totalRevenue == 0
        ? 0
        : (grossProfit / totalRevenue) * 100;
        Console.WriteLine("===== DAILY COGS =====");

        foreach (var kv in dailyCogs)
        {
            Console.WriteLine($"{kv.Key} -> {kv.Value}");
        }
        var daily = sales
     .GroupBy(x => x.SaleDate)
     .Select(g =>
     {
         var revenue = g.Sum(x => x.Total);

         var cogs = dailyCogs.ContainsKey(g.Key)
             ? dailyCogs[g.Key]
             : 0;
         Console.WriteLine("===== DAILY COGS =====");

         foreach (var kv in dailyCogs)
         {
             Console.WriteLine($"{kv.Key} -> {kv.Value}");
         }
         var profit = revenue - cogs;

         var margin =
             revenue == 0
                 ? 0
                 : (profit / revenue) * 100;

         return new DailyPLRow
         {
             Date = g.Key.ToString(),
             DayName = g.Key.DayOfWeek.ToString(),

             Orders = g.Count(),

             GrossRevenue = g.Sum(x => x.Subtotal),

             Discount = g.Sum(x => x.Discount),

             NetRevenue = revenue,

             Cogs = cogs,

             GrossProfit = profit,

             GrossMarginPct = margin,

             PurchaseSpend =
        dailyPurchaseSpend.ContainsKey(g.Key)
            ? dailyPurchaseSpend[g.Key]
            : 0
         
     };
     })
     .OrderBy(x => x.Date)
     .ToList();


        var monthly = sales
    .GroupBy(x => new
    {
        x.SaleDate.Year,
        x.SaleDate.Month
    })
    .Select(g =>
    {
        var revenue = g.Sum(x => x.Total);

        var monthKey =
            $"{g.Key.Year}-{g.Key.Month}";

        var cogs =
            monthlyCogs.ContainsKey(monthKey)
                ? monthlyCogs[monthKey]
                : 0;

        var profit = revenue - cogs;

        var margin =
            revenue == 0
                ? 0
                : (profit / revenue) * 100;

        return new MonthlyPLRow
        {
            Year = g.Key.Year,
            Month = g.Key.Month,

            MonthName = new DateTime(
                g.Key.Year,
                g.Key.Month,
                1)
                .ToString("MMMM"),

            Orders = g.Count(),

            GrossRevenue = g.Sum(x => x.Subtotal),

            Discount = g.Sum(x => x.Discount),

            NetRevenue = revenue,

            Cogs = cogs,

            GrossProfit = profit,

            GrossMarginPct = margin
        };
    })
    .OrderBy(x => x.Year)
    .ThenBy(x => x.Month)
    .ToList();
        var channels = sales
 .GroupBy(x => x.Channel)
 .Select(g =>
 {
     var revenue =
         g.Sum(x => x.Total);

     var channelKey =
         g.Key ?? "UNKNOWN";

     var cogs =
         channelCogs.ContainsKey(channelKey)
             ? channelCogs[channelKey]
             : 0;

     var profit =
         revenue - cogs;

     var margin =
         revenue == 0
             ? 0
             : (profit / revenue) * 100;

     return new ChannelPLRow
     {
         Channel = channelKey,

         Orders = g.Count(),

         GrossRevenue =
             g.Sum(x => x.Subtotal),

         Discount =
             g.Sum(x => x.Discount),

         NetRevenue = revenue,

         Cogs = cogs,

         GrossProfit = profit,

         GrossMarginPct = margin,

         RevenueSharePct =
             totalRevenue == 0
                 ? 0
                 : (revenue / totalRevenue) * 100
     };
 })
 .OrderByDescending(x => x.NetRevenue)
 .ToList();
        Console.WriteLine($"Daily Rows = {daily.Count}");
        Console.WriteLine($"Monthly Rows = {monthly.Count}");
        Console.WriteLine($"Channel Rows = {channels.Count}");
        Console.WriteLine($"OutletId = {outletId}");
        Console.WriteLine($"From = {from}");
        Console.WriteLine($"To = {to}");
        Console.WriteLine($"Sales Count = {sales.Count}");
        return new PLReport
        {
            Daily = daily,
            Monthly = monthly,
            Channels = channels,

            TotalRevenue = totalRevenue,
            TotalDiscount = totalDiscount,
            TotalGrossRevenue = totalGrossRevenue,
            TotalCogs = totalCogs,
            TotalGrossProfit = grossProfit,
            AvgMarginPct = marginPct,
            TotalOrders = totalOrders,

            TotalPurchaseSpend = totalPurchaseSpend
        };
    }
}