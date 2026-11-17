using System.Data;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Entities.Operations;
using HotelPro.Domain.Entities.Reservations;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class FrontDeskService : IFrontDeskService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public FrontDeskService(HotelProDbContext db, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CheckInRecordDto>> CheckInAsync(CheckInRequest request, CancellationToken ct = default)
    {
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var res = await _db.Reservations
            .Include(r => r.Rooms).ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");

        if (res.Status == ReservationStatus.CheckedIn)
            throw new BusinessRuleException("Reservation is already checked in.");

        if (res.Status != ReservationStatus.Confirmed)
            throw new BusinessRuleException($"Only Confirmed reservations can be checked in (current status: {res.Status}).");

        foreach (var rr in res.Rooms)
        {
            if (rr.Room.Status is RoomStatus.Maintenance or RoomStatus.OutOfService or RoomStatus.Cleaning or RoomStatus.Occupied)
                throw new BusinessRuleException($"Room {rr.Room.RoomNumber} is not ready for check-in (status: {rr.Room.Status}).");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var records = new List<CheckInRecord>();

        foreach (var rr in res.Rooms)
        {
            rr.Room.Status = RoomStatus.Occupied;
            rr.Room.UpdatedAt = now;
            rr.Room.UpdatedBy = _currentUser.Email;

            var rec = new CheckInRecord
            {
                ReservationId = res.Id,
                RoomId = rr.RoomId,
                ActualCheckInTime = now,
                ActualAdults = request.ActualAdults,
                ActualChildren = request.ActualChildren,
                DocumentsVerified = request.DocumentsVerified,
                Notes = request.Notes?.Trim(),
                PerformedByUserId = userId
            };
            _db.CheckInRecords.Add(rec);
            records.Add(rec);
        }

        res.Status = ReservationStatus.CheckedIn;
        res.UpdatedAt = now;
        res.UpdatedBy = _currentUser.Email;

        _db.ReservationStatusHistory.Add(new ReservationStatusHistory
        {
            ReservationId = res.Id,
            FromStatus = ReservationStatus.Confirmed,
            ToStatus = ReservationStatus.CheckedIn,
            Reason = "Guest checked in",
            ChangedBy = _currentUser.Email
        });

        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        await _audit.LogAsync("CheckIn", "Reservation", res.Id.ToString(), newValues: new { res.ReservationNumber }, ct: ct);

        return records.Select(rec => new CheckInRecordDto(
            rec.Id, res.Id, res.ReservationNumber, rec.RoomId,
            res.Rooms.First(x => x.RoomId == rec.RoomId).Room.RoomNumber,
            rec.ActualCheckInTime, rec.ActualAdults, rec.ActualChildren, rec.DocumentsVerified, rec.Notes
        )).ToList();
    }

    public async Task<CheckOutSummaryDto> GetCheckOutSummaryAsync(Guid reservationId, CancellationToken ct = default)
    {
        var res = await _db.Reservations
            .Include(r => r.PrimaryGuest)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");

        var servicesTotal = await _db.ServiceOrders
            .Where(o => o.ReservationId == reservationId && o.Status != ServiceOrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, ct);

        var grandTotal = res.TotalAmount + servicesTotal;
        var balanceDue = Math.Max(0m, grandTotal - res.DepositRequired);

        return new CheckOutSummaryDto(
            res.Id,
            res.ReservationNumber,
            res.PrimaryGuest.FullName,
            res.SubTotal,
            servicesTotal,
            res.DiscountAmount,
            res.TaxAmount + res.ServiceChargeAmount,
            res.DepositRequired,
            grandTotal,
            balanceDue
        );
    }

    public async Task<IReadOnlyList<CheckOutRecordDto>> CheckOutAsync(CheckOutRequest request, CancellationToken ct = default)
    {
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var res = await _db.Reservations
            .Include(r => r.Rooms).ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");

        if (res.Status != ReservationStatus.CheckedIn)
            throw new BusinessRuleException($"Only CheckedIn reservations can be checked out (current status: {res.Status}).");

        var summary = await GetCheckOutSummaryAsync(res.Id, ct);
        var remainingAfterPayment = summary.BalanceDue - request.FinalPaymentSettled;

        if (remainingAfterPayment > 0.01m)
        {
            var settings = await _db.HotelSettings.AsNoTracking().FirstOrDefaultAsync(ct);
            var canApprove = request.ExceptionalBalanceApproved
                             && (settings?.AllowCheckoutWithBalancePermission ?? false)
                             && _currentUser.HasPermission(PermissionsCatalog.Billing.Approve);

            if (!canApprove)
                throw new BusinessRuleException($"Cannot check out with unpaid balance of {remainingAfterPayment:F2}. Settle the balance first.");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var records = new List<CheckOutRecord>();

        foreach (var rr in res.Rooms)
        {
            rr.Room.Status = RoomStatus.Cleaning;
            rr.Room.UpdatedAt = now;
            rr.Room.UpdatedBy = _currentUser.Email;

            var rec = new CheckOutRecord
            {
                ReservationId = res.Id,
                RoomId = rr.RoomId,
                ActualCheckOutTime = now,
                FinalBalanceSettled = request.FinalPaymentSettled,
                ExceptionalBalanceApprovedBy = remainingAfterPayment > 0.01m ? userId : null,
                Notes = request.Notes?.Trim(),
                PerformedByUserId = userId
            };
            _db.CheckOutRecords.Add(rec);
            records.Add(rec);

            // Auto-create high-priority checkout cleaning task
            _db.HousekeepingTasks.Add(new HousekeepingTask
            {
                RoomId = rr.RoomId,
                TaskType = HousekeepingTaskType.CheckoutCleaning,
                Priority = TaskPriority.High,
                Status = HousekeepingStatus.Pending,
                ScheduledFor = DateOnly.FromDateTime(DateTime.UtcNow),
                Notes = $"Auto-generated from Check-out ({res.ReservationNumber})",
                CreatedBy = _currentUser.Email
            });
        }

        res.Status = ReservationStatus.CheckedOut;
        res.UpdatedAt = now;
        res.UpdatedBy = _currentUser.Email;

        _db.ReservationStatusHistory.Add(new ReservationStatusHistory
        {
            ReservationId = res.Id,
            FromStatus = ReservationStatus.CheckedIn,
            ToStatus = ReservationStatus.CheckedOut,
            Reason = "Guest checked out",
            ChangedBy = _currentUser.Email
        });

        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        await _audit.LogAsync("CheckOut", "Reservation", res.Id.ToString(),
            newValues: new { res.ReservationNumber, request.FinalPaymentSettled }, ct: ct);

        return records.Select(rec => new CheckOutRecordDto(
            rec.Id, res.Id, res.ReservationNumber, rec.RoomId,
            res.Rooms.First(x => x.RoomId == rec.RoomId).Room.RoomNumber,
            rec.ActualCheckOutTime, rec.FinalBalanceSettled, rec.Notes
        )).ToList();
    }
}

