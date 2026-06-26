using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace KitchenManagementSystem.API.Models;

public class StockLedger
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }

    public Guid RawMaterialId { get; set; }

    public DateTimeOffset TxnDate { get; set; }

    public string TxnType { get; set; } = string.Empty;

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public decimal QuantityIn { get; set; }

    public decimal QuantityOut { get; set; }

    public decimal BalanceAfter { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [ValidateNever]
    public Outlet Outlet { get; set; } = null!;

    [ValidateNever]
    public RawMaterial RawMaterial { get; set; } = null!;
}