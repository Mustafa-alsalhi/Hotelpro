namespace HotelPro.Domain.Enums;

public enum BookingSource
{
    DirectBooking = 1,
    Phone = 2,
    Website = 3,
    TravelAgent = 4,
    WalkIn = 5,
    Other = 6
}

public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    CheckedIn = 3,
    CheckedOut = 4,
    Cancelled = 5,
    NoShow = 6
}

