using System;
using System.Threading.Tasks;

namespace KitchenManagementSystem.API.Services;

public interface IDashboardService
{
    Task<object> GetSummaryAsync(Guid? organizationId, Guid? outletId);
}
