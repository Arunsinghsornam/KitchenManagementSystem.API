using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface ISupplierService
{
    Task<IEnumerable<Supplier>> GetAllAsync(Guid? organizationId, Guid? outletId);
    Task<Supplier> CreateAsync(Guid defaultOutletId, CreateSupplierDto dto);
    Task<Supplier?> UpdateAsync(Guid? organizationId, Guid? outletId, Guid id, UpdateSupplierDto dto);
    Task<bool> DeleteAsync(Guid? organizationId, Guid? outletId, Guid id);
}
