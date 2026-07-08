using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Services;

public interface INotificationService
{
    Task AddNotificationAsync(
        Guid userId,
        Guid? organizationId,
        Guid? outletId,
        string message);

    Task<IEnumerable<Notification>> GetRecentNotificationsAsync(
        string role,
        Guid? organizationId);

    Task MarkAllAsReadAsync(
        string role,
        Guid? organizationId);
}
