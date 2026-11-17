using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Operations;

public record GuestServiceDto(
    Guid Id,
    string Name,
    string? Description,
    ServiceCategory Category,
    decimal Price,
    bool Taxable,
    bool IsActive
);

public record SaveGuestServiceRequest(
    string Name,
    string? Description,
    ServiceCategory Category,
    decimal Price,
    bool Taxable,
    bool IsActive
);

public record ServiceOrderItemDto(
    Guid Id,
    Guid GuestServiceId,
    string ServiceName,
    int Quantity,
    decimal UnitPrice,
    decimal TaxAmount,
    decimal LineTotal
);

public record ServiceOrderDto(
    Guid Id,
    string OrderNumber,
    Guid ReservationId,
    string ReservationNumber,
    Guid RoomId,
    string RoomNumber,
    Guid GuestId,
    string GuestName,
    ServiceOrderStatus Status,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    Guid? AssignedUserId,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    string? Notes,
    IReadOnlyList<ServiceOrderItemDto> Items
);

public record CreateServiceOrderItemRequest(Guid GuestServiceId, int Quantity);

public record CreateServiceOrderRequest(
    Guid ReservationId,
    Guid RoomId,
    string? Notes,
    IReadOnlyList<CreateServiceOrderItemRequest> Items
);

public record UpdateServiceOrderStatusRequest(ServiceOrderStatus Status, Guid? AssignedUserId);

