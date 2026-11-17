using System.Data;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Finance;
using HotelPro.Domain.Entities.Finance;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public BillingService(HotelProDbContext db, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(PaginationParams pagination, InvoiceStatus? status, CancellationToken ct = default)
    {
        var query = _db.Invoices
            .Include(i => i.Reservation)
            .Include(i => i.Guest)
            .Include(i => i.Items)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(term) ||
                i.Reservation.ReservationNumber.ToLower().Contains(term) ||
                i.Guest.FullName.ToLower().Contains(term));
        }
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<InvoiceDto>
        {
            Items = items.Select(MapInvoice).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
    {
        var inv = await _db.Invoices
            .Include(i => i.Reservation)
            .Include(i => i.Guest)
            .Include(i => i.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException("Invoice not found.");

        return MapInvoice(inv);
    }

    public async Task<InvoiceDto> IssueInvoiceForReservationAsync(IssueInvoiceRequest request, CancellationToken ct = default)
    {
        var res = await _db.Reservations
            .Include(r => r.Rooms).ThenInclude(rr => rr.Room)
            .Include(r => r.PrimaryGuest)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");

        if (res.Status == ReservationStatus.Cancelled)
            throw new BusinessRuleException("Cannot issue an invoice for a cancelled reservation.");

        var existing = await _db.Invoices.FirstOrDefaultAsync(i => i.ReservationId == res.Id && i.Status != InvoiceStatus.Cancelled, ct);
        if (existing is not null)
            return await GetInvoiceByIdAsync(existing.Id, ct);

        var orders = await _db.ServiceOrders
            .Include(o => o.Items).ThenInclude(oi => oi.GuestService)
            .Where(o => o.ReservationId == res.Id && o.Status != ServiceOrderStatus.Cancelled)
            .ToListAsync(ct);

        var servicesSubTotal = orders.Sum(o => o.SubTotal);
        var servicesTax = orders.Sum(o => o.TaxAmount);
        var grandTotal = res.TotalAmount + servicesSubTotal + servicesTax;

        var invCount = await _db.Invoices.CountAsync(ct);
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{(invCount + 1):D4}",
            ReservationId = res.Id,
            GuestId = res.PrimaryGuestId,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = request.DueDate ?? res.CheckOutDate,
            AccommodationTotal = res.SubTotal,
            ServicesTotal = servicesSubTotal,
            DiscountTotal = res.DiscountAmount,
            TaxTotal = res.TaxAmount + servicesTax,
            ServiceChargeTotal = res.ServiceChargeAmount,
            GrandTotal = grandTotal,
            PaidAmount = 0m,
            BalanceDue = grandTotal,
            Status = InvoiceStatus.Issued,
            CreatedBy = _currentUser.Email
        };

        foreach (var rr in res.Rooms)
        {
            invoice.Items.Add(new InvoiceItem
            {
                InvoiceId = invoice.Id,
                ItemType = InvoiceItemType.RoomCharge,
                Description = $"Room #{rr.Room.RoomNumber} ({rr.Nights} nights)",
                Quantity = rr.Nights,
                UnitPrice = rr.AgreedNightlyRate,
                Total = rr.TotalRoomAmount
            });
        }

        foreach (var ord in orders)
        {
            foreach (var oi in ord.Items)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    InvoiceId = invoice.Id,
                    ItemType = InvoiceItemType.ServiceCharge,
                    Description = $"{oi.GuestService.Name} ({ord.OrderNumber})",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Total = oi.LineTotal
                });
            }
        }

        if (res.DiscountAmount > 0)
        {
            invoice.Items.Add(new InvoiceItem
            {
                InvoiceId = invoice.Id,
                ItemType = InvoiceItemType.Discount,
                Description = "Reservation Discount",
                Quantity = 1,
                UnitPrice = -res.DiscountAmount,
                Total = -res.DiscountAmount
            });
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("IssueInvoice", "Invoice", invoice.Id.ToString(), newValues: new { invoice.InvoiceNumber, invoice.GrandTotal }, ct: ct);

        return await GetInvoiceByIdAsync(invoice.Id, ct);
    }

    public async Task<InvoiceDto> CancelInvoiceAsync(Guid id, CancelInvoiceRequest request, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException("Invoice not found.");

        if (invoice.PaidAmount > 0)
            throw new BusinessRuleException("Cannot cancel an invoice with active payments. Reverse payments first.");

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.CancellationReason = request.Reason.Trim();
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        invoice.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CancelInvoice", "Invoice", invoice.Id.ToString(), newValues: new { request.Reason }, ct: ct);
        return await GetInvoiceByIdAsync(invoice.Id, ct);
    }

    public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(PaginationParams pagination, Guid? invoiceId, CancellationToken ct = default)
    {
        var query = _db.Payments.Include(p => p.Invoice).AsNoTracking().AsQueryable();
        if (invoiceId.HasValue) query = query.Where(p => p.InvoiceId == invoiceId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.PaymentDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<PaymentDto>
        {
            Items = items.Select(MapPayment).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        // Idempotency check
        var existing = await _db.Payments.Include(p => p.Invoice).AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null)
            return MapPayment(existing);

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new NotFoundException("Invoice not found.");

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new BusinessRuleException("Cannot record payment for a cancelled invoice.");

        if (request.Amount > invoice.BalanceDue)
            throw new BusinessRuleException($"Payment amount ({request.Amount:F2}) exceeds balance due ({invoice.BalanceDue:F2}).");

        var payCount = await _db.Payments.CountAsync(ct);
        var payment = new Payment
        {
            ReceiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{(payCount + 1):D4}",
            InvoiceId = invoice.Id,
            ReservationId = invoice.ReservationId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber?.Trim(),
            PaymentDate = DateTimeOffset.UtcNow,
            IsReversal = false,
            IdempotencyKey = request.IdempotencyKey.Trim(),
            RecordedByUserId = _currentUser.UserId ?? Guid.Empty,
            Notes = request.Notes?.Trim()
        };

        invoice.PaidAmount += request.Amount;
        invoice.BalanceDue = Math.Max(0m, invoice.GrandTotal - invoice.PaidAmount);
        invoice.Status = invoice.BalanceDue <= 0m ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        invoice.UpdatedBy = _currentUser.Email;

        _db.Payments.Add(payment);

        // Recognize collected revenue once on actual payment
        _db.RevenueRecords.Add(new RevenueRecord
        {
            Source = RevenueSource.Rooms,
            ReferenceId = payment.Id,
            Description = $"Payment {payment.ReceiptNumber} for {invoice.InvoiceNumber}",
            Amount = payment.Amount,
            RecognizedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RecordedByUserId = _currentUser.UserId ?? Guid.Empty
        });

        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        await _audit.LogAsync("RecordPayment", "Payment", payment.Id.ToString(),
            newValues: new { payment.ReceiptNumber, payment.Amount, invoice.InvoiceNumber }, ct: ct);

        payment.Invoice = invoice;
        return MapPayment(payment);
    }

    public async Task<PaymentDto> ReversePaymentAsync(Guid paymentId, ReversePaymentRequest request, CancellationToken ct = default)
    {
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var original = await _db.Payments.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new NotFoundException("Payment not found.");

        if (original.IsReversal)
            throw new BusinessRuleException("Cannot reverse a reversal entry.");

        if (await _db.Payments.AnyAsync(p => p.ReversedPaymentId == original.Id, ct))
            throw new ConflictException("This payment has already been reversed.");

        var payCount = await _db.Payments.CountAsync(ct);
        var reversal = new Payment
        {
            ReceiptNumber = $"REV-{DateTime.UtcNow:yyyyMMdd}-{(payCount + 1):D4}",
            InvoiceId = original.InvoiceId,
            ReservationId = original.ReservationId,
            Amount = -original.Amount,
            PaymentMethod = original.PaymentMethod,
            ReferenceNumber = original.ReceiptNumber,
            PaymentDate = DateTimeOffset.UtcNow,
            IsReversal = true,
            ReversedPaymentId = original.Id,
            IdempotencyKey = $"REV-{original.Id}",
            RecordedByUserId = _currentUser.UserId ?? Guid.Empty,
            Notes = $"Reversal: {request.Reason}"
        };

        original.Invoice.PaidAmount = Math.Max(0m, original.Invoice.PaidAmount - original.Amount);
        original.Invoice.BalanceDue = original.Invoice.GrandTotal - original.Invoice.PaidAmount;
        original.Invoice.Status = original.Invoice.PaidAmount <= 0m ? InvoiceStatus.Issued : InvoiceStatus.PartiallyPaid;

        _db.Payments.Add(reversal);

        // Offset revenue recognition
        _db.RevenueRecords.Add(new RevenueRecord
        {
            Source = RevenueSource.Rooms,
            ReferenceId = reversal.Id,
            Description = $"Reversal {reversal.ReceiptNumber} of {original.ReceiptNumber}",
            Amount = -original.Amount,
            RecognizedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RecordedByUserId = _currentUser.UserId ?? Guid.Empty
        });

        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        await _audit.LogAsync("ReversePayment", "Payment", reversal.Id.ToString(),
            newValues: new { Original = original.ReceiptNumber, request.Reason }, ct: ct);

        reversal.Invoice = original.Invoice;
        return MapPayment(reversal);
    }

    private static InvoiceDto MapInvoice(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.ReservationId, i.Reservation.ReservationNumber,
        i.GuestId, i.Guest.FullName, i.IssueDate, i.DueDate,
        i.AccommodationTotal, i.ServicesTotal, i.DiscountTotal, i.TaxTotal, i.ServiceChargeTotal,
        i.GrandTotal, i.PaidAmount, i.BalanceDue, i.Status, i.CancellationReason,
        i.Items.Select(it => new InvoiceItemDto(it.Id, it.ItemType, it.Description, it.Quantity, it.UnitPrice, it.Total)).ToList()
    );

    private static PaymentDto MapPayment(Payment p) => new(
        p.Id, p.ReceiptNumber, p.InvoiceId, p.Invoice.InvoiceNumber, p.ReservationId,
        p.Amount, p.PaymentMethod, p.ReferenceNumber, p.PaymentDate,
        p.IsReversal, p.ReversedPaymentId, p.IdempotencyKey, p.Notes
    );
}

