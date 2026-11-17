using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Operations;

public class MaintenanceRequest : AuditableEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }

    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    public Guid? AssignedTechnicianUserId { get; set; }
    public bool TakeRoomOutOfService { get; set; }
    public string? SparePartsUsed { get; set; }
    public decimal Cost { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

