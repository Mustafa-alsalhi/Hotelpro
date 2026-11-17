using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Guests;
using HotelPro.Domain.Entities.Reservations;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Finance;

public class Invoice : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public decimal AccommodationTotal { get; set; }
    public decimal ServicesTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ServiceChargeTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    public string? CancellationReason { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public InvoiceItemType ItemType { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

