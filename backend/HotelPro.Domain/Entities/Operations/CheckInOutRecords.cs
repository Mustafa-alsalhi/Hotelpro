using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Reservations;
using HotelPro.Domain.Entities.Rooms;

namespace HotelPro.Domain.Entities.Operations;

public class CheckInRecord : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTimeOffset ActualCheckInTime { get; set; } = DateTimeOffset.UtcNow;
    public int ActualAdults { get; set; }
    public int ActualChildren { get; set; }
    public bool DocumentsVerified { get; set; }
    public string? Notes { get; set; }
    public Guid PerformedByUserId { get; set; }
}

public class CheckOutRecord : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTimeOffset ActualCheckOutTime { get; set; } = DateTimeOffset.UtcNow;
    public decimal FinalBalanceSettled { get; set; }
    public Guid? ExceptionalBalanceApprovedBy { get; set; }
    public string? Notes { get; set; }
    public Guid PerformedByUserId { get; set; }
}

