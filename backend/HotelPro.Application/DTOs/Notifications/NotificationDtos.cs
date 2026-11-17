namespace HotelPro.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    string? RelatedEntityType,
    string? RelatedEntityId,
    bool IsRead,
    DateTimeOffset CreatedAt
);

