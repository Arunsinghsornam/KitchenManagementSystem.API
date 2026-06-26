namespace KitchenManagementSystem.API.Models;

public class PurchaseItem
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public Guid RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal GstPercent { get; set; }
    public decimal LineTotal { get; set; }

    public Purchase Purchase { get; set; } = null!;
    public RawMaterial RawMaterial { get; set; } = null!;
}
