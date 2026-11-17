using HotelPro.Domain.Common;

namespace HotelPro.Domain.Entities.Notifications;

public class Notification : BaseEntity
{
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

