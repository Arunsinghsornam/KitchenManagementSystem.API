namespace KitchenManagementSystem.API.Models;

public class Purchase
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid SupplierId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal GstAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public Outlet Outlet { get; set; } = null!;
    public ICollection<PurchaseItem> Items { get; set; } = [];
}