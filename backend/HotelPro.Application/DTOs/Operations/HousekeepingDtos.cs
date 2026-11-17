using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Operations;

public record HousekeepingTaskDto(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    int Floor,
    HousekeepingTaskType TaskType,
    TaskPriority Priority,
    HousekeepingStatus Status,
    Guid? AssignedToUserId,
    DateOnly ScheduledFor,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? InspectedAt,
    string? Notes,
    bool DamageReported
);

public record CreateHousekeepingTaskRequest(
    Guid RoomId,
    HousekeepingTaskType TaskType,
    TaskPriority Priority,
    Guid? AssignedToUserId,
    DateOnly ScheduledFor,
    string? Notes
);

public record UpdateHousekeepingTaskRequest(
    HousekeepingStatus Status,
    Guid? AssignedToUserId,
    bool DamageReported,
    string? Notes
);

