using System.ComponentModel.DataAnnotations;
using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Guests;
using HotelPro.Domain.Entities.Rates;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Reservations;

public class Reservation : AuditableEntity
{
    public string ReservationNumber { get; set; } = string.Empty;
    public Guid HotelId { get; set; }

    public Guid PrimaryGuestId { get; set; }
    public Guest PrimaryGuest { get; set; } = null!;

    public Guid RatePlanId { get; set; }
    public RatePlan RatePlan { get; set; } = null!;

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int Nights { get; set; }
    public int AdultsCount { get; set; } = 1;
    public int ChildrenCount { get; set; }

    public BookingSource Source { get; set; } = BookingSource.DirectBooking;
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositRequired { get; set; }

    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ICollection<ReservationRoom> Rooms { get; set; } = new List<ReservationRoom>();
    public ICollection<ReservationGuest> Guests { get; set; } = new List<ReservationGuest>();
    public ICollection<ReservationStatusHistory> StatusHistory { get; set; } = new List<ReservationStatusHistory>();
}

