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

        var fromDateOnly = DateOnly.FromDateTime(from);
        var toDateOnly = DateOnly.FromDateTime(to);

        var startOfFromMonth = new DateOnly(fromDateOnly.Year, fromDateOnly.Month, 1);

        var expenses = await _db.Expenses
            .Include(e => e.OtherExpenses)
            .Where(e => e.OutletId == outletId && e.ExpenseDate >= startOfFromMonth && e.ExpenseDate <= toDateOnly)
            .ToListAsync();

        var totalPurchaseSpend =
            purchases.Sum(x => x.Total);

        var dailyPurchaseSpend =
            purchases
                .GroupBy(x => x.PurchaseDate)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Total));

        decimal totalExpenseAmount = 0;
        var dailyExpenses = new Dictionary<DateOnly, decimal>();

        for (var date = fromDateOnly; date <= toDateOnly; date = date.AddDays(1))
        {
            var exp = expenses.FirstOrDefault(e => e.ExpenseDate.Year == date.Year && e.ExpenseDate.Month == date.Month);
            decimal dailyExpense = 0;
            if (exp != null)
            {
                var monthlyTotal = exp.StaffSalary + exp.ShopRent + exp.EbBill + exp.GasBill + exp.MiscExpense + exp.OtherExpenses.Sum(o => o.Amount);
                var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
                dailyExpense = monthlyTotal / daysInMonth;
            }

            var dailyPurchase = dailyPurchaseSpend.ContainsKey(date) ? dailyPurchaseSpend[date] : 0;
            dailyExpense += dailyPurchase;

            dailyExpenses[date] = dailyExpense;
            totalExpenseAmount += dailyExpense;
        }

        var monthlyExpenses = dailyExpenses
            .GroupBy(kv => $"{kv.Key.Year}-{kv.Key.Month}")
            .ToDictionary(
                g => g.Key,
                g => g.Sum(kv => kv.Value)
            );

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
        var allDates = sales.Select(s => s.SaleDate)
            .Union(expenses.Select(e => e.ExpenseDate))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var daily = allDates
            .Select(date =>
            {
                var daySales = sales.Where(s => s.SaleDate == date).ToList();
                var revenue = daySales.Sum(s => s.Total);
                var subtotal = daySales.Sum(s => s.Subtotal);
                var discount = daySales.Sum(s => s.Discount);
                var orders = daySales.Count;

                var cogs = dailyCogs.ContainsKey(date) ? dailyCogs[date] : 0;
                var expense = dailyExpenses.ContainsKey(date) ? dailyExpenses[date] : 0;

                var grossProfit = revenue - cogs;
                var netProfit = revenue - cogs - expense;

                var margin = revenue == 0 ? 0 : (grossProfit / revenue) * 100;
                var spend = dailyPurchaseSpend.ContainsKey(date) ? dailyPurchaseSpend[date] : 0;

                return new DailyPLRow
                {
                    Date = date.ToString(),
                    DayName = date.DayOfWeek.ToString(),
                    Orders = orders,
                    GrossRevenue = subtotal,
                    Discount = discount,
                    NetRevenue = revenue,
                    Cogs = cogs,
                    Expenses = expense,
                    GrossProfit = grossProfit,
                    NetProfit = netProfit,
                    GrossMarginPct = margin,
                    PurchaseSpend = spend
                };
            })
            .OrderBy(x => x.Date)
            .ToList();

        var allMonths = sales.Select(s => new { Year = s.SaleDate.Year, Month = s.SaleDate.Month })
            .Union(expenses.Select(e => new { Year = e.ExpenseDate.Year, Month = e.ExpenseDate.Month }))
            .Distinct()
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        var monthly = allMonths
            .Select(g =>
            {
                var monthSales = sales.Where(s => s.SaleDate.Year == g.Year && s.SaleDate.Month == g.Month).ToList();
                var revenue = monthSales.Sum(s => s.Total);
                var subtotal = monthSales.Sum(s => s.Subtotal);
                var discount = monthSales.Sum(s => s.Discount);
                var orders = monthSales.Count;

                var monthKey = $"{g.Year}-{g.Month}";
                var cogs = monthlyCogs.ContainsKey(monthKey) ? monthlyCogs[monthKey] : 0;
                var expense = monthlyExpenses.ContainsKey(monthKey) ? monthlyExpenses[monthKey] : 0;

                var grossProfit = revenue - cogs;
                var netProfit = revenue - cogs - expense;

                var margin = revenue == 0 ? 0 : (grossProfit / revenue) * 100;

                return new MonthlyPLRow
                {
                    Year = g.Year,
                    Month = g.Month,
                    MonthName = new DateTime(g.Year, g.Month, 1).ToString("MMMM"),
                    Orders = orders,
                    GrossRevenue = subtotal,
                    Discount = discount,
                    NetRevenue = revenue,
                    Cogs = cogs,
                    Expenses = expense,
                    GrossProfit = grossProfit,
                    NetProfit = netProfit,
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
                var revenue = g.Sum(x => x.Total);
                var channelKey = g.Key ?? "UNKNOWN";
                var cogs = channelCogs.ContainsKey(channelKey) ? channelCogs[channelKey] : 0;
                var profit = revenue - cogs;
                var margin = revenue == 0 ? 0 : (profit / revenue) * 100;

                return new ChannelPLRow
                {
                    Channel = channelKey,
                    Orders = g.Count(),
                    GrossRevenue = g.Sum(x => x.Subtotal),
                    Discount = g.Sum(x => x.Discount),
                    NetRevenue = revenue,
                    Cogs = cogs,
                    GrossProfit = profit,
                    GrossMarginPct = margin,
                    RevenueSharePct = totalRevenue == 0 ? 0 : (revenue / totalRevenue) * 100
                };
            })
            .OrderByDescending(x => x.NetRevenue)
            .ToList();

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
            TotalExpenses = totalExpenseAmount,
            NetProfit = grossProfit - totalExpenseAmount,
            AvgMarginPct = marginPct,
            TotalOrders = totalOrders,
            TotalPurchaseSpend = totalPurchaseSpend
        };
    }
}