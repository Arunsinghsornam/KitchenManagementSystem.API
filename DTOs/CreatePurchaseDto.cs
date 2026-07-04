using System;
using System.Collections.Generic;

namespace KitchenManagementSystem.API.DTOs;

public class CreatePurchaseDto
{
    public Guid? OutletId { get; set; }
    public Guid SupplierId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public List<PurchaseLineDto> Items { get; set; } = [];
}

public class PurchaseLineDto
{
    public Guid RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal GstPercent { get; set; }
}
