using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Finance;

public record InvoiceItemDto(
    Guid Id,
    InvoiceItemType ItemType,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Total
);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid ReservationId,
    string ReservationNumber,
    Guid GuestId,
    string GuestName,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal AccommodationTotal,
    decimal ServicesTotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal ServiceChargeTotal,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    InvoiceStatus Status,
    string? CancellationReason,
    IReadOnlyList<InvoiceItemDto> Items
);

public record IssueInvoiceRequest(Guid ReservationId, DateOnly? DueDate);

public record CancelInvoiceRequest(string Reason);

