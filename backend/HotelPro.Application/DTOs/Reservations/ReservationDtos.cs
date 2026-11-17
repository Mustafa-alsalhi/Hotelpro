using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Reservations;

public record ReservationRoomDto(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    Guid RoomTypeId,
    string RoomTypeName,
    decimal AgreedNightlyRate,
    int Nights,
    decimal TotalRoomAmount,
    DateOnly CheckInDate,
    DateOnly CheckOutDate
);

public record ReservationDto(
    Guid Id,
    string ReservationNumber,
    Guid PrimaryGuestId,
    string PrimaryGuestName,
    string PrimaryGuestPhone,
    Guid RatePlanId,
    string RatePlanName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Nights,
    int AdultsCount,
    int ChildrenCount,
    BookingSource Source,
    ReservationStatus Status,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ServiceChargeAmount,
    decimal TotalAmount,
    decimal DepositRequired,
    string? Notes,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ReservationRoomDto> Rooms
);

public record CreateReservationRequest(
    Guid PrimaryGuestId,
    Guid RatePlanId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int AdultsCount,
    int ChildrenCount,
    BookingSource Source,
    IReadOnlyList<Guid> RoomIds,
    decimal DiscountAmount,
    decimal DepositRequired,
    string? Notes,
    bool ConfirmImmediately = false
);

public record UpdateReservationStayRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int AdultsCount,
    int ChildrenCount,
    decimal DiscountAmount,
    string? Notes
);

public record ChangeReservationRoomRequest(
    Guid CurrentRoomId,
    Guid NewRoomId,
    string? Reason
);

public record CancelReservationRequest(string Reason);

