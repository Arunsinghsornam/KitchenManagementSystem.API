using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public ExpensesController(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    // GET /api/expenses
    [HttpGet]
    [Authorize(Policy = "PLAccess")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? outletId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        Guid? finalOutletId = null;
        try
        {
            if (IsPowerAdmin() || IsSuperAdmin())
            {
                if (outletId.HasValue)
                {
                    if (!IsPowerAdmin())
                    {
                        await ValidateOutletAccessAsync(outletId.Value, _db);
                    }
                    finalOutletId = outletId.Value;
                }
            }
            else
            {
                finalOutletId = GetOutletId();
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var orgId = IsPowerAdmin() ? null : (Guid?)GetOrganizationId();

        var query = _db.Expenses
            .Include(e => e.Outlet)
            .Include(e => e.OtherExpenses)
            .AsQueryable();

        if (finalOutletId.HasValue)
        {
            query = query.Where(e => e.OutletId == finalOutletId.Value);
        }
        else if (orgId.HasValue)
        {
            query = query.Where(e => e.Outlet.OrganizationId == orgId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate <= toDate.Value);
        }

        var list = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        var response = list.Select(ToDto);
        return Ok(response);
    }

    // GET /api/expenses/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "PLAccess")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var expense = await _db.Expenses
            .Include(e => e.Outlet)
            .Include(e => e.OtherExpenses)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
            return NotFound();

        try
        {
            if (!IsPowerAdmin())
            {
                await ValidateOutletAccessAsync(expense.OutletId, _db);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        return Ok(ToDto(expense));
    }

    // POST /api/expenses
    [HttpPost]
    [Authorize(Policy = "PLAccess")]
    public async Task<IActionResult> Create([FromBody] CreateExpenseDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            if (!IsPowerAdmin())
            {
                await ValidateOutletAccessAsync(dto.OutletId, _db);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            OutletId = dto.OutletId,
            ExpenseDate = dto.ExpenseDate,
            StaffSalary = dto.StaffSalary,
            ShopRent = dto.ShopRent,
            EbBill = dto.EbBill,
            GasBill = dto.GasBill,
            MiscExpense = dto.MiscExpense,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var item in dto.OtherExpenses)
        {
            expense.OtherExpenses.Add(new OtherExpenseItem
            {
                Id = Guid.NewGuid(),
                ExpenseId = expense.Id,
                Description = item.Description.Trim(),
                Amount = item.Amount
            });
        }

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        // Reload details for response
        await _db.Entry(expense).Reference(e => e.Outlet).LoadAsync();

        var total = expense.StaffSalary + expense.ShopRent + expense.EbBill + expense.GasBill + expense.MiscExpense + expense.OtherExpenses.Sum(o => o.Amount);
        await _notificationService.AddNotificationAsync(
            GetUserId(),
            IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
            expense.OutletId,
            $"Added new expense of ₹{total:N2} for date {expense.ExpenseDate:yyyy-MM-dd}");

        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, ToDto(expense));
    }

    // PUT /api/expenses/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "PLAccess")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var expense = await _db.Expenses
            .Include(e => e.OtherExpenses)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
            return NotFound();

        try
        {
            if (!IsPowerAdmin())
            {
                await ValidateOutletAccessAsync(expense.OutletId, _db);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var oldDate = expense.ExpenseDate;
        var oldStaffSalary = expense.StaffSalary;
        var oldShopRent = expense.ShopRent;
        var oldEbBill = expense.EbBill;
        var oldGasBill = expense.GasBill;
        var oldMiscExpense = expense.MiscExpense;
        var oldOthersSum = expense.OtherExpenses.Sum(o => o.Amount);
        var oldTotal = oldStaffSalary + oldShopRent + oldEbBill + oldGasBill + oldMiscExpense + oldOthersSum;

        expense.ExpenseDate = dto.ExpenseDate;
        expense.StaffSalary = dto.StaffSalary;
        expense.ShopRent = dto.ShopRent;
        expense.EbBill = dto.EbBill;
        expense.GasBill = dto.GasBill;
        expense.MiscExpense = dto.MiscExpense;

        // Remove old child items using standard tracked deletion
        foreach (var oldItem in expense.OtherExpenses.ToList())
        {
            _db.OtherExpenseItems.Remove(oldItem);
        }
        expense.OtherExpenses.Clear();

        // Add new child items
        foreach (var item in dto.OtherExpenses)
        {
            var newItem = new OtherExpenseItem
            {
                Id = Guid.NewGuid(),
                ExpenseId = expense.Id,
                Description = item.Description.Trim(),
                Amount = item.Amount
            };
            expense.OtherExpenses.Add(newItem);
            _db.Entry(newItem).State = EntityState.Added;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var details = _db.ChangeTracker.Entries().Select(e => new {
                Entity = e.Entity.GetType().Name,
                State = e.State.ToString(),
                Keys = e.Metadata.FindPrimaryKey()?.Properties.ToDictionary(p => p.Name, p => e.Property(p.Name).CurrentValue)
            }).ToList();
            return BadRequest(new { 
                message = "Concurrency exception during save.", 
                details = details,
                innerException = ex.InnerException?.Message ?? ex.Message
            });
        }

        await _db.Entry(expense).Reference(e => e.Outlet).LoadAsync();

        var newOthersSum = dto.OtherExpenses.Sum(o => o.Amount);
        var newTotal = dto.StaffSalary + dto.ShopRent + dto.EbBill + dto.GasBill + dto.MiscExpense + newOthersSum;

        var changes = new List<string>();
        if (oldDate != dto.ExpenseDate) changes.Add($"Date: {oldDate:yyyy-MM-dd} → {dto.ExpenseDate:yyyy-MM-dd}");
        if (oldStaffSalary != dto.StaffSalary) changes.Add($"Salary: ₹{oldStaffSalary} → ₹{dto.StaffSalary}");
        if (oldShopRent != dto.ShopRent) changes.Add($"Rent: ₹{oldShopRent} → ₹{dto.ShopRent}");
        if (oldEbBill != dto.EbBill) changes.Add($"EB: ₹{oldEbBill} → ₹{dto.EbBill}");
        if (oldGasBill != dto.GasBill) changes.Add($"Gas: ₹{oldGasBill} → ₹{dto.GasBill}");
        if (oldMiscExpense != dto.MiscExpense) changes.Add($"Misc: ₹{oldMiscExpense} → ₹{dto.MiscExpense}");
        if (oldOthersSum != newOthersSum) changes.Add($"Others: ₹{oldOthersSum} → ₹{newOthersSum}");

        var changesString = changes.Any() ? " | Changes: " + string.Join(", ", changes) : "";

        await _notificationService.AddNotificationAsync(
            GetUserId(),
            IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
            expense.OutletId,
            $"Updated expense details for date {oldDate:yyyy-MM-dd} (Total: ₹{oldTotal:N2} → ₹{newTotal:N2}){changesString}");

        return Ok(ToDto(expense));
    }

    // DELETE /api/expenses/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PLAccess")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense == null)
            return NotFound();

        try
        {
            if (!IsPowerAdmin())
            {
                await ValidateOutletAccessAsync(expense.OutletId, _db);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        var expenseDate = expense.ExpenseDate;
        var expenseOutletId = expense.OutletId;

        await _db.Entry(expense).Collection(e => e.OtherExpenses).LoadAsync();
        var total = expense.StaffSalary + expense.ShopRent + expense.EbBill + expense.GasBill + expense.MiscExpense + expense.OtherExpenses.Sum(o => o.Amount);

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();

        await _notificationService.AddNotificationAsync(
            GetUserId(),
            IsPowerAdmin() ? null : GetOrganizationIdOrNull(),
            expenseOutletId,
            $"Deleted expense of ₹{total:N2} for date {expenseDate:yyyy-MM-dd}");

        return Ok(new { message = "Deleted" });
    }

    private static ExpenseResponseDto ToDto(Expense e)
    {
        var otherItems = e.OtherExpenses.Select(o => new OtherExpenseItemResponseDto
        {
            Id = o.Id,
            Description = o.Description,
            Amount = o.Amount
        }).ToList();

        var totalOthers = otherItems.Sum(o => o.Amount);
        var totalAmount = e.StaffSalary + e.ShopRent + e.EbBill + e.GasBill + e.MiscExpense + totalOthers;

        return new ExpenseResponseDto
        {
            Id = e.Id,
            OutletId = e.OutletId,
            OutletName = e.Outlet?.Name,
            ExpenseDate = e.ExpenseDate,
            StaffSalary = e.StaffSalary,
            ShopRent = e.ShopRent,
            EbBill = e.EbBill,
            GasBill = e.GasBill,
            MiscExpense = e.MiscExpense,
            OtherExpenses = otherItems,
            TotalAmount = totalAmount,
            CreatedAt = e.CreatedAt
        };
    }
}
