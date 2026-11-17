using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Domain.Entities.Operations;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MaintenanceService(HotelProDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<MaintenanceRequestDto>> GetRequestsAsync(
        PaginationParams pagination,
        MaintenanceStatus? status,
        TaskPriority? priority,
        CancellationToken ct = default)
    {
        var query = _db.MaintenanceRequests.Include(m => m.Room).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(m =>
                m.RequestNumber.ToLower().Contains(term) ||
                m.IssueType.ToLower().Contains(term) ||
                (m.Room != null && m.Room.RoomNumber.ToLower().Contains(term)));
        }
        if (status.HasValue) query = query.Where(m => m.Status == status.Value);
        if (priority.HasValue) query = query.Where(m => m.Priority == priority.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(m => m.Priority).ThenByDescending(m => m.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<MaintenanceRequestDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<MaintenanceRequestDto> CreateRequestAsync(CreateMaintenanceRequest request, CancellationToken ct = default)
    {
        var count = await _db.MaintenanceRequests.CountAsync(ct);
        var entity = new MaintenanceRequest
        {
            RequestNumber = $"MNT-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}",
            RoomId = request.RoomId,
            IssueType = request.IssueType.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority,
            Status = MaintenanceStatus.Open,
            TakeRoomOutOfService = request.TakeRoomOutOfService,
            CreatedBy = _currentUser.Email
        };

        if (request.RoomId.HasValue && request.TakeRoomOutOfService)
        {
            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId.Value, ct)
                ?? throw new NotFoundException("Room not found.");
            if (room.Status == RoomStatus.Occupied)
                throw new BusinessRuleException("Cannot take an Occupied room out of service before Check-Out.");
            room.Status = RoomStatus.Maintenance;
        }

        _db.MaintenanceRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.MaintenanceRequests.Include(m => m.Room).AsNoTracking().FirstAsync(m => m.Id == entity.Id, ct);
        return Map(saved);
    }

    public async Task<MaintenanceRequestDto> UpdateRequestAsync(Guid id, UpdateMaintenanceRequest request, CancellationToken ct = default)
    {
        var entity = await _db.MaintenanceRequests.Include(m => m.Room).FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Maintenance request not found.");

        entity.Status = request.Status;
        entity.AssignedTechnicianUserId = request.AssignedTechnicianUserId ?? entity.AssignedTechnicianUserId;
        entity.SparePartsUsed = request.SparePartsUsed?.Trim() ?? entity.SparePartsUsed;
        entity.Cost = request.Cost;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = _currentUser.Email;

        if (request.Status is MaintenanceStatus.Completed or MaintenanceStatus.Closed)
        {
            entity.CompletedAt ??= DateTimeOffset.UtcNow;
            if (request.RestoreRoomToAvailable && entity.Room is not null && entity.Room.Status is RoomStatus.Maintenance or RoomStatus.OutOfService)
            {
                entity.Room.Status = RoomStatus.Available;
            }
        }

        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    private static MaintenanceRequestDto Map(MaintenanceRequest m) => new(
        m.Id, m.RequestNumber, m.RoomId, m.Room?.RoomNumber, m.IssueType, m.Description,
        m.Priority, m.Status, m.AssignedTechnicianUserId, m.TakeRoomOutOfService,
        m.SparePartsUsed, m.Cost, m.CreatedAt, m.CompletedAt
    );
}

