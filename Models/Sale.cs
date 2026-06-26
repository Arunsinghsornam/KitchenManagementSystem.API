using System.Net.ServerSentEvents;

namespace KitchenManagementSystem.API.Models;

public class Sale
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public DateOnly SaleDate { get; set; }
    public string Channel { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Outlet Outlet { get; set; } = null!;
    public ICollection<SaleItem> Items { get; set; } = [];
}
