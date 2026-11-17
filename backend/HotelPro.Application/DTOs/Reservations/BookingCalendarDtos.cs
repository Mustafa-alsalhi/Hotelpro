using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Reservations;

public record CalendarBookingBlockDto(
    Guid ReservationId,
    string ReservationNumber,
    string GuestName,
    ReservationStatus Status,
    DateOnly CheckInDate,
    DateOnly CheckOutDate
);

public record CalendarRoomRowDto(
    Guid RoomId,
    string RoomNumber,
    int Floor,
    string RoomTypeName,
    RoomStatus CurrentRoomStatus,
    IReadOnlyList<CalendarBookingBlockDto> Bookings
);

public record BookingCalendarDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<CalendarRoomRowDto> Rooms
);

