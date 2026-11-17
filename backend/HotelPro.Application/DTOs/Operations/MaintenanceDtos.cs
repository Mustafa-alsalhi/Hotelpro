using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Operations;

public record MaintenanceRequestDto(
    Guid Id,
    string RequestNumber,
    Guid? RoomId,
    string? RoomNumber,
    string IssueType,
    string Description,
    TaskPriority Priority,
    MaintenanceStatus Status,
    Guid? AssignedTechnicianUserId,
    bool TakeRoomOutOfService,
    string? SparePartsUsed,
    decimal Cost,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);

public record CreateMaintenanceRequest(
    Guid? RoomId,
    string IssueType,
    string Description,
    TaskPriority Priority,
    bool TakeRoomOutOfService
);

public record UpdateMaintenanceRequest(
    MaintenanceStatus Status,
    Guid? AssignedTechnicianUserId,
    string? SparePartsUsed,
    decimal Cost,
    bool RestoreRoomToAvailable
);

