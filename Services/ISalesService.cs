using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface ISalesService
{
    Task<IEnumerable<Sale>> GetAllAsync(Guid? organizationId, Guid? outletId, DateOnly? fromDate = null, DateOnly? toDate = null);
    Task<(bool HasShortage, IEnumerable<object> Shortages)> StockCheckAsync(Guid menuItemId, decimal quantity);
    Task<Sale> CreateAsync(Guid outletId, CreateSaleDto dto);
}
