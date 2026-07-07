using System;
using System.Collections.Generic;

namespace KitchenManagementSystem.API.Models;

public class Expense
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Outlet Outlet { get; set; } = null!;
    
    public DateOnly ExpenseDate { get; set; }
    
    public decimal StaffSalary { get; set; }
    public decimal ShopRent { get; set; }
    public decimal EbBill { get; set; }
    public decimal GasBill { get; set; }
    public decimal MiscExpense { get; set; }
    
    public ICollection<OtherExpenseItem> OtherExpenses { get; set; } = new List<OtherExpenseItem>();
    
    public DateTimeOffset CreatedAt { get; set; }
}

public class OtherExpenseItem
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Expense Expense { get; set; } = null!;
    
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
