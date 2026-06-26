using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface IPLReportService
{
    Task<PLReport> GetReport(
    Guid outletId,
    DateTime from,
    DateTime to);
}
