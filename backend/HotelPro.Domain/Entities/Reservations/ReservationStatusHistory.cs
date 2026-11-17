using HotelPro.Domain.Common;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Reservations;

public class ReservationStatusHistory : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public ReservationStatus FromStatus { get; set; }
    public ReservationStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public string? ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
}

