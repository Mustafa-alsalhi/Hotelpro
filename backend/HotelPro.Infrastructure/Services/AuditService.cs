using System.Text.Json;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Domain.Entities.Identity;
using HotelPro.Infrastructure.Persistence;

namespace HotelPro.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditService(HotelProDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.Email,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
            IpAddress = _currentUser.IpAddress,
            Timestamp = DateTimeOffset.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}

