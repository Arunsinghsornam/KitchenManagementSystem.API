using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KitchenManagementSystem.API.DTOs;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface IPurchaseService
{
    Task<IEnumerable<Purchase>> GetAllAsync(Guid? organizationId, Guid? outletId, DateOnly? fromDate = null, DateOnly? toDate = null);
    Task<Purchase> CreateAsync(Guid outletId, CreatePurchaseDto dto);
}
