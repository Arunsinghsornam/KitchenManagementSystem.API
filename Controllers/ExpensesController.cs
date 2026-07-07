using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
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

    public ExpensesController(AppDbContext db)
    {
        _db = db;
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

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
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
