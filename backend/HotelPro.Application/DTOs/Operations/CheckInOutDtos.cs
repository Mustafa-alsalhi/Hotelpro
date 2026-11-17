namespace HotelPro.Application.DTOs.Operations;

public record CheckInRequest(
    Guid ReservationId,
    int ActualAdults,
    int ActualChildren,
    bool DocumentsVerified,
    string? Notes
);

public record CheckInRecordDto(
    Guid Id,
    Guid ReservationId,
    string ReservationNumber,
    Guid RoomId,
    string RoomNumber,
    DateTimeOffset ActualCheckInTime,
    int ActualAdults,
    int ActualChildren,
    bool DocumentsVerified,
    string? Notes
);

public record CheckOutSummaryDto(
    Guid ReservationId,
    string ReservationNumber,
    string GuestName,
    decimal AccommodationTotal,
    decimal ServicesTotal,
    decimal DiscountAmount,
    decimal TaxAndServiceTotal,
    decimal DepositAmount,
    decimal GrandTotal,
    decimal BalanceDue
);

public record CheckOutRequest(
    Guid ReservationId,
    decimal FinalPaymentSettled,
    bool ExceptionalBalanceApproved,
    string? Notes
);

public record CheckOutRecordDto(
    Guid Id,
    Guid ReservationId,
    string ReservationNumber,
    Guid RoomId,
    string RoomNumber,
    DateTimeOffset ActualCheckOutTime,
    decimal FinalBalanceSettled,
    string? Notes
);

