using HotelPro.Domain.Entities.Guests;

namespace HotelPro.Domain.Entities.Reservations;

public class ReservationGuest
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public bool IsPrimary { get; set; }
}

