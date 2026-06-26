using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace KitchenManagementSystem.API.Models;

public class RawMaterial
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal ReorderLevel { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal AverageCost { get; set; }

    public bool TrackExpiry { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // EF Core navigation property
    // Prevent Swagger/API validation from requiring Outlet in POST requests
    [ValidateNever]
    public Outlet Outlet { get; set; } = null!;

    [ValidateNever]
    public Category? Category { get; set; }
}