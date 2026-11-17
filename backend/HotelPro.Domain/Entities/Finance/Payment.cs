using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Reservations;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Finance;

public class Payment : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.UtcNow;

    public bool IsReversal { get; set; }
    public Guid? ReversedPaymentId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid RecordedByUserId { get; set; }
    public string? Notes { get; set; }
}

