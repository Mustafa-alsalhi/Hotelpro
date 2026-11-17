using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Finance;

public record PaymentDto(
    Guid Id,
    string ReceiptNumber,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid ReservationId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber,
    DateTimeOffset PaymentDate,
    bool IsReversal,
    Guid? ReversedPaymentId,
    string IdempotencyKey,
    string? Notes
);

public record RecordPaymentRequest(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber,
    string IdempotencyKey,
    string? Notes
);

public record ReversePaymentRequest(string Reason);

