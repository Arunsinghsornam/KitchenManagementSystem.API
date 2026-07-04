using System.ComponentModel.DataAnnotations;

namespace KitchenManagementSystem.API.Models;

public class PLReport
{
    public List<DailyPLRow> Daily { get; set; } = new();


public List<MonthlyPLRow> Monthly { get; set; } = new();

    public List<ChannelPLRow> Channels { get; set; } = new();
    public decimal TotalPurchaseSpend { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalGrossRevenue { get; set; }

    public decimal TotalCogs { get; set; }

    public decimal TotalGrossProfit { get; set; }

    public decimal AvgMarginPct { get; set; }

    public int TotalOrders { get; set; }


}

public class DailyPLRow
{
    public string Date { get; set; } = "";


public string DayName { get; set; } = "";

    public int Orders { get; set; }

    public decimal GrossRevenue { get; set; }

    public decimal Discount { get; set; }

    public decimal NetRevenue { get; set; }

    public decimal Cogs { get; set; }

    public decimal GrossProfit { get; set; }

    public decimal GrossMarginPct { get; set; }

    public decimal PurchaseSpend { get; set; }


}

public class MonthlyPLRow
{
    public int Year { get; set; }


public int Month { get; set; }

    public string MonthName { get; set; } = "";

    public int Orders { get; set; }

    public decimal GrossRevenue { get; set; }

    public decimal Discount { get; set; }

    public decimal NetRevenue { get; set; }

    public decimal Cogs { get; set; }

    public decimal GrossProfit { get; set; }

    public decimal GrossMarginPct { get; set; }


}

public class ChannelPLRow
{
    public string Channel { get; set; } = "";
    public decimal Cogs { get; set; }

    public decimal GrossProfit { get; set; }

    public decimal GrossMarginPct { get; set; }

    public int Orders { get; set; }

    public decimal GrossRevenue { get; set; }

    public decimal Discount { get; set; }

    public decimal NetRevenue { get; set; }

    public decimal RevenueSharePct { get; set; }


}
