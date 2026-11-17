using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Notifications;
using HotelPro.Domain.Entities.Notifications;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(HotelProDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(int take = 25, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var userRoles = userId.HasValue
            ? await (from ur in _db.UserRoles
                     join r in _db.Roles on ur.RoleId equals r.Id
                     where ur.UserId == userId.Value
                     select r.Name!).ToListAsync(ct)
            : [];

        return await _db.Notifications.AsNoTracking()
            .Where(n => (n.TargetUserId == null && n.TargetRole == null)
                        || n.TargetUserId == userId
                        || (n.TargetRole != null && userRoles.Contains(n.TargetRole)))
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type, n.RelatedEntityType, n.RelatedEntityId, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var list = await GetMyNotificationsAsync(100, ct);
        return list.Count(n => !n.IsRead);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is not null)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllAsReadAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var unread = await _db.Notifications
            .Where(n => !n.IsRead && (n.TargetUserId == null || n.TargetUserId == userId))
            .ToListAsync(ct);
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CreateAsync(
        string title,
        string message,
        string type = "Info",
        Guid? targetUserId = null,
        string? targetRole = null,
        CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            TargetUserId = targetUserId,
            TargetRole = targetRole
        });
        await _db.SaveChangesAsync(ct);
    }
}

