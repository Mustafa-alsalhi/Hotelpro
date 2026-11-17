using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Operations;

public class HousekeepingTask : AuditableEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public HousekeepingTaskType TaskType { get; set; } = HousekeepingTaskType.DailyCleaning;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public HousekeepingStatus Status { get; set; } = HousekeepingStatus.Pending;

    public Guid? AssignedToUserId { get; set; }
    public Guid? InspectedByUserId { get; set; }
    public DateOnly ScheduledFor { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? InspectedAt { get; set; }
    public string? Notes { get; set; }
    public bool DamageReported { get; set; }
}

