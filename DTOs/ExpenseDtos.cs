using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KitchenManagementSystem.API.DTOs;

public class CreateExpenseDto
{
    [Required]
    public Guid OutletId { get; set; }

    [Required]
    public DateOnly ExpenseDate { get; set; }

    public decimal StaffSalary { get; set; }
    public decimal ShopRent { get; set; }
    public decimal EbBill { get; set; }
    public decimal GasBill { get; set; }
    public decimal MiscExpense { get; set; }

    public List<CreateOtherExpenseItemDto> OtherExpenses { get; set; } = new();
}

public class CreateOtherExpenseItemDto
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public decimal Amount { get; set; }
}

public class UpdateExpenseDto
{
    [Required]
    public DateOnly ExpenseDate { get; set; }

    public decimal StaffSalary { get; set; }
    public decimal ShopRent { get; set; }
    public decimal EbBill { get; set; }
    public decimal GasBill { get; set; }
    public decimal MiscExpense { get; set; }

    public List<CreateOtherExpenseItemDto> OtherExpenses { get; set; } = new();
}

public class ExpenseResponseDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public DateOnly ExpenseDate { get; set; }
    
    public decimal StaffSalary { get; set; }
    public decimal ShopRent { get; set; }
    public decimal EbBill { get; set; }
    public decimal GasBill { get; set; }
    public decimal MiscExpense { get; set; }
    
    public List<OtherExpenseItemResponseDto> OtherExpenses { get; set; } = new();
    
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class OtherExpenseItemResponseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
