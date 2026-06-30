using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KitchenManagementSystem.API.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;

    public SupplierService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync(Guid? organizationId, Guid? outletId)
    {
        return await _db.Suppliers
            .AsNoTracking()
            .Include(s => s.Outlet)
            .Where(s => (organizationId == null || s.Outlet.OrganizationId == organizationId)
                     && (outletId == null || s.OutletId == outletId))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Supplier> CreateAsync(Guid defaultOutletId, CreateSupplierDto dto)
    {
        var outletId = dto.OutletId != Guid.Empty ? dto.OutletId : defaultOutletId;

        var outletExists = await _db.Outlets.AnyAsync(o => o.Id == outletId);
        if (!outletExists)
        {
            throw new KeyNotFoundException("Invalid OutletId.");
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            OutletId = outletId,
            Name = dto.Name,
            ContactPerson = dto.ContactPerson,
            Mobile = dto.Mobile,
            GstNumber = dto.GstNumber,
            Email = dto.Email,
            Address = dto.Address,
            Outstanding = dto.Outstanding,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return supplier;
    }

    public async Task<Supplier?> UpdateAsync(Guid? organizationId, Guid? outletId, Guid id, UpdateSupplierDto dto)
    {
        var supplier = await _db.Suppliers
            .Include(s => s.Outlet)
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                (organizationId == null || s.Outlet.OrganizationId == organizationId) &&
                (outletId == null || s.OutletId == outletId));

        if (supplier == null)
            return null;

        supplier.Name = dto.Name ?? supplier.Name;
        supplier.ContactPerson = dto.ContactPerson ?? supplier.ContactPerson;
        supplier.Mobile = dto.Mobile ?? supplier.Mobile;
        supplier.GstNumber = dto.GstNumber ?? supplier.GstNumber;
        supplier.Email = dto.Email ?? supplier.Email;
        supplier.Address = dto.Address ?? supplier.Address;
        supplier.Outstanding = dto.Outstanding;

        await _db.SaveChangesAsync();

        return supplier;
    }

    public async Task<bool> DeleteAsync(Guid? organizationId, Guid? outletId, Guid id)
    {
        var supplier = await _db.Suppliers
            .Include(s => s.Outlet)
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                (organizationId == null || s.Outlet.OrganizationId == organizationId) &&
                (outletId == null || s.OutletId == outletId));

        if (supplier == null)
            return false;

        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();

        return true;
    }
}
