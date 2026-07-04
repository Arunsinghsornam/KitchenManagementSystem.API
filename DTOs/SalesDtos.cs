using System;
using System.Collections.Generic;

namespace KitchenManagementSystem.API.DTOs;

public class CreateSaleDto
{
    public Guid? OutletId { get; set; }
    public DateOnly SaleDate { get; set; }
    public string Channel { get; set; } = "OUTLET";
    public decimal Discount { get; set; }
    public List<SaleLineDto> Items { get; set; } = [];
}

public class SaleLineDto
{
    public Guid MenuItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
