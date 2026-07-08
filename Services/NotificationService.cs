using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenManagementSystem.API.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddNotificationAsync(
        Guid userId,
        Guid? organizationId,
        Guid? outletId,
        string message)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;

        string outletName = "Platform";
        if (outletId.HasValue)
        {
            var outlet = await _db.Outlets.FindAsync(outletId.Value);
            if (outlet != null)
            {
                outletName = outlet.Name;
            }
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId ?? user.OrganizationId,
            OutletId = outletId ?? user.OutletId,
            Message = message,
            UserFullName = user.FullName ?? user.Email,
            UserRole = user.Role,
            OutletName = outletName,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Notification>> GetRecentNotificationsAsync(
        string role,
        Guid? organizationId)
    {
        IQueryable<Notification> query = _db.Notifications;

        if (role.Equals("super_admin", StringComparison.OrdinalIgnoreCase) && organizationId.HasValue)
        {
            query = query.Where(n => n.OrganizationId == organizationId.Value);
        }
        else if (role.Equals("power_admin", StringComparison.OrdinalIgnoreCase))
        {
            // Power admin sees all
        }
        else
        {
            return Enumerable.Empty<Notification>();
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task MarkAllAsReadAsync(
        string role,
        Guid? organizationId)
    {
        IQueryable<Notification> query = _db.Notifications.Where(n => !n.IsRead);

        if (role.Equals("super_admin", StringComparison.OrdinalIgnoreCase) && organizationId.HasValue)
        {
            query = query.Where(n => n.OrganizationId == organizationId.Value);
        }
        else if (role.Equals("power_admin", StringComparison.OrdinalIgnoreCase))
        {
            // Power admin marks all unread as read
        }
        else
        {
            return;
        }

        var unread = await query.ToListAsync();
        foreach (var n in unread)
        {
            n.IsRead = true;
        }
        await _db.SaveChangesAsync();
    }
}
