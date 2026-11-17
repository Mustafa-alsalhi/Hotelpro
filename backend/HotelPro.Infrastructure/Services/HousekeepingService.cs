using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Entities.Operations;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class HousekeepingService : IHousekeepingService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public HousekeepingService(HotelProDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<HousekeepingTaskDto>> GetTasksAsync(
        PaginationParams pagination,
        HousekeepingStatus? status,
        TaskPriority? priority,
        CancellationToken ct = default)
    {
        var query = _db.HousekeepingTasks.Include(t => t.Room).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(t => t.Room.RoomNumber.ToLower().Contains(term));
        }
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<HousekeepingTaskDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<HousekeepingTaskDto> CreateTaskAsync(CreateHousekeepingTaskRequest request, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct)
            ?? throw new NotFoundException("Room not found.");

        var task = new HousekeepingTask
        {
            RoomId = room.Id,
            TaskType = request.TaskType,
            Priority = request.Priority,
            Status = request.AssignedToUserId.HasValue ? HousekeepingStatus.Assigned : HousekeepingStatus.Pending,
            AssignedToUserId = request.AssignedToUserId,
            ScheduledFor = request.ScheduledFor,
            Notes = request.Notes?.Trim(),
            CreatedBy = _currentUser.Email
        };

        _db.HousekeepingTasks.Add(task);
        await _db.SaveChangesAsync(ct);

        task.Room = room;
        return Map(task);
    }

    public async Task<HousekeepingTaskDto> UpdateTaskAsync(Guid id, UpdateHousekeepingTaskRequest request, CancellationToken ct = default)
    {
        var task = await _db.HousekeepingTasks.Include(t => t.Room).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("Housekeeping task not found.");

        if (request.Status == HousekeepingStatus.Inspected && !_currentUser.HasPermission(PermissionsCatalog.Housekeeping.Approve))
            throw new ForbiddenException("Only supervisors with Housekeeping.Approve permission can inspect rooms.");

        var now = DateTimeOffset.UtcNow;
        task.Status = request.Status;
        task.AssignedToUserId = request.AssignedToUserId ?? task.AssignedToUserId;
        task.DamageReported = request.DamageReported;
        task.Notes = request.Notes?.Trim() ?? task.Notes;
        task.UpdatedAt = now;
        task.UpdatedBy = _currentUser.Email;

        if (request.Status == HousekeepingStatus.InProgress && !task.StartedAt.HasValue)
            task.StartedAt = now;

        if (request.Status == HousekeepingStatus.Completed)
        {
            task.CompletedAt = now;
            var settings = await _db.HotelSettings.AsNoTracking().FirstOrDefaultAsync(ct);
            if (!(settings?.RequireInspectionAfterCleaning ?? true) && task.Room.Status == RoomStatus.Cleaning)
                task.Room.Status = RoomStatus.Available;
        }

        if (request.Status == HousekeepingStatus.Inspected)
        {
            task.InspectedAt = now;
            task.InspectedByUserId = _currentUser.UserId;
            if (task.Room.Status == RoomStatus.Cleaning)
                task.Room.Status = RoomStatus.Available;
        }

        await _db.SaveChangesAsync(ct);
        return Map(task);
    }

    private static HousekeepingTaskDto Map(HousekeepingTask t) => new(
        t.Id, t.RoomId, t.Room.RoomNumber, t.Room.Floor, t.TaskType, t.Priority, t.Status,
        t.AssignedToUserId, t.ScheduledFor, t.StartedAt, t.CompletedAt, t.InspectedAt,
        t.Notes, t.DamageReported
    );
}

