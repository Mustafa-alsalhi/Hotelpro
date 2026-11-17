using System.Data;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Rates;
using HotelPro.Application.DTOs.Reservations;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Entities.Reservations;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class ReservationService : IReservationService
{
    private readonly HotelProDbContext _db;
    private readonly IRateManagementService _rateService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    private static readonly ReservationStatus[] ActiveBlockingStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Confirmed,
        ReservationStatus.CheckedIn
    ];

    public ReservationService(
        HotelProDbContext db,
        IRateManagementService rateService,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _db = db;
        _rateService = rateService;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<ReservationDto>> GetReservationsAsync(
        PaginationParams pagination,
        ReservationStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default)
    {
        var query = _db.Reservations
            .Include(r => r.PrimaryGuest)
            .Include(r => r.RatePlan)
            .Include(r => r.Rooms).ThenInclude(rr => rr.Room)
            .Include(r => r.Rooms).ThenInclude(rr => rr.RoomType)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(r =>
                r.ReservationNumber.ToLower().Contains(term) ||
                r.PrimaryGuest.FullName.ToLower().Contains(term) ||
                r.PrimaryGuest.Phone.Contains(term));
        }

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (fromDate.HasValue) query = query.Where(r => r.CheckOutDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(r => r.CheckInDate <= toDate.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ReservationDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<ReservationDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var res = await _db.Reservations
            .Include(r => r.PrimaryGuest)
            .Include(r => r.RatePlan)
            .Include(r => r.Rooms).ThenInclude(rr => rr.Room)
            .Include(r => r.Rooms).ThenInclude(rr => rr.RoomType)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Reservation not found.");

        return Map(res);
    }

    public async Task<IReadOnlyList<RoomDto>> GetAvailableRoomsAsync(
        DateOnly checkIn,
        DateOnly checkOut,
        Guid? roomTypeId,
        CancellationToken ct = default)
    {
        if (checkOut <= checkIn)
            throw new BusinessRuleException("Check-out date must be after check-in date.");

        var busyRoomIds = await _db.ReservationRooms
            .Where(rr => ActiveBlockingStatuses.Contains(rr.Reservation.Status)
                         && rr.CheckInDate < checkOut
                         && rr.CheckOutDate > checkIn)
            .Select(rr => rr.RoomId)
            .Distinct()
            .ToListAsync(ct);

        var query = _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
            .AsNoTracking()
            .Where(r => !busyRoomIds.Contains(r.Id)
                        && r.Status != RoomStatus.OutOfService
                        && r.Status != RoomStatus.Maintenance);

        if (roomTypeId.HasValue)
            query = query.Where(r => r.RoomTypeId == roomTypeId.Value);

        var rooms = await query.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToListAsync(ct);
        return rooms.Select(r => new RoomDto(
            r.Id, r.RoomTypeId, r.RoomType.Name, r.RoomNumber, r.Floor, r.MaxCapacity,
            r.BedCount, r.BasePriceOverride ?? r.RoomType.BasePrice, r.BasePriceOverride,
            r.Status, r.Description, r.Notes, [], [])).ToList();
    }

    public async Task<BookingCalendarDto> GetCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
            throw new BusinessRuleException("End date must be on or after start date.");

        var rooms = await _db.Rooms
            .Include(r => r.RoomType)
            .AsNoTracking()
            .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .ToListAsync(ct);

        var blocks = await _db.ReservationRooms
            .Include(rr => rr.Reservation).ThenInclude(r => r.PrimaryGuest)
            .AsNoTracking()
            .Where(rr => rr.Reservation.Status != ReservationStatus.Cancelled
                         && rr.Reservation.Status != ReservationStatus.NoShow
                         && rr.CheckInDate <= to
                         && rr.CheckOutDate >= from)
            .ToListAsync(ct);

        var rows = rooms.Select(room => new CalendarRoomRowDto(
            room.Id,
            room.RoomNumber,
            room.Floor,
            room.RoomType.Name,
            room.Status,
            blocks.Where(b => b.RoomId == room.Id)
                .Select(b => new CalendarBookingBlockDto(
                    b.ReservationId,
                    b.Reservation.ReservationNumber,
                    b.Reservation.PrimaryGuest.FullName,
                    b.Reservation.Status,
                    b.CheckInDate,
                    b.CheckOutDate))
                .ToList()
        )).ToList();

        return new BookingCalendarDto(from, to, rows);
    }

    public async Task<ReservationDto> CreateAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        if (request.CheckOutDate <= request.CheckInDate)
            throw new BusinessRuleException("Check-out date must be after check-in date.");

        var hotelId = await _db.Hotels.Select(h => h.Id).FirstOrDefaultAsync(ct);
        if (hotelId == Guid.Empty) throw new BusinessRuleException("Hotel not initialized.");

        if (!await _db.Guests.AnyAsync(g => g.Id == request.PrimaryGuestId, ct))
            throw new NotFoundException("Primary guest not found.");

        if (!await _db.RatePlans.AnyAsync(rp => rp.Id == request.RatePlanId && rp.IsActive, ct))
            throw new NotFoundException("Active rate plan not found.");

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var roomIds = request.RoomIds.Distinct().ToList();
        var rooms = await _db.Rooms.Include(r => r.RoomType).Where(r => roomIds.Contains(r.Id)).ToListAsync(ct);
        if (rooms.Count != roomIds.Count)
            throw new NotFoundException("One or more selected rooms do not exist.");

        foreach (var room in rooms)
        {
            if (room.Status is RoomStatus.OutOfService or RoomStatus.Maintenance)
                throw new BusinessRuleException($"Room {room.RoomNumber} is currently {room.Status} and cannot be booked.");
        }

        // Strict overlap check inside Serializable transaction
        var conflictingRooms = await _db.ReservationRooms
            .Include(rr => rr.Room)
            .Where(rr => roomIds.Contains(rr.RoomId)
                         && ActiveBlockingStatuses.Contains(rr.Reservation.Status)
                         && rr.CheckInDate < request.CheckOutDate
                         && rr.CheckOutDate > request.CheckInDate)
            .Select(rr => rr.Room.RoomNumber)
            .Distinct()
            .ToListAsync(ct);

        if (conflictingRooms.Count > 0)
            throw new ConflictException($"Room(s) {string.Join(", ", conflictingRooms)} are already booked for the selected dates.");

        var nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
        var initialStatus = request.ConfirmImmediately ? ReservationStatus.Confirmed : ReservationStatus.Pending;

        var reservation = new Reservation
        {
            ReservationNumber = await GenerateReservationNumberAsync(ct),
            HotelId = hotelId,
            PrimaryGuestId = request.PrimaryGuestId,
            RatePlanId = request.RatePlanId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Nights = nights,
            AdultsCount = request.AdultsCount,
            ChildrenCount = request.ChildrenCount,
            Source = request.Source,
            Status = initialStatus,
            DiscountAmount = request.DiscountAmount,
            DepositRequired = request.DepositRequired,
            Notes = request.Notes?.Trim(),
            CreatedBy = _currentUser.Email
        };

        decimal subTotal = 0m;
        decimal taxTotal = 0m;
        decimal serviceTotal = 0m;

        foreach (var room in rooms)
        {
            var quote = await _rateService.CalculateQuoteAsync(new RateQuoteRequest(
                room.RoomTypeId, request.RatePlanId, request.CheckInDate, request.CheckOutDate, 1, 0), ct);

            // Lock agreed nightly rate at booking time
            var agreedRate = quote.AverageNightlyRate;
            var roomTotal = quote.SubTotal - quote.LongStayDiscount;

            reservation.Rooms.Add(new ReservationRoom
            {
                ReservationId = reservation.Id,
                RoomId = room.Id,
                RoomTypeId = room.RoomTypeId,
                AgreedNightlyRate = agreedRate,
                Nights = nights,
                TotalRoomAmount = roomTotal,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate
            });

            subTotal += roomTotal;
            if (room.Status == RoomStatus.Available && initialStatus == ReservationStatus.Confirmed && request.CheckInDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            {
                room.Status = RoomStatus.Reserved;
            }
        }

        var settings = await _db.HotelSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var taxable = Math.Max(0m, subTotal - request.DiscountAmount);
        serviceTotal = Math.Round(taxable * ((settings?.ServiceChargePercent ?? 0m) / 100m), 2, MidpointRounding.AwayFromZero);
        taxTotal = Math.Round((taxable + serviceTotal) * ((settings?.TaxRatePercent ?? 0m) / 100m), 2, MidpointRounding.AwayFromZero);

        reservation.SubTotal = subTotal;
        reservation.ServiceChargeAmount = serviceTotal;
        reservation.TaxAmount = taxTotal;
        reservation.TotalAmount = taxable + serviceTotal + taxTotal;

        reservation.Guests.Add(new ReservationGuest
        {
            ReservationId = reservation.Id,
            GuestId = request.PrimaryGuestId,
            IsPrimary = true
        });

        reservation.StatusHistory.Add(new ReservationStatusHistory
        {
            ReservationId = reservation.Id,
            FromStatus = ReservationStatus.Pending,
            ToStatus = initialStatus,
            Reason = "Reservation created",
            ChangedBy = _currentUser.Email
        });

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        await _audit.LogAsync("CreateReservation", "Reservation", reservation.Id.ToString(),
            newValues: new { reservation.ReservationNumber, reservation.TotalAmount, Status = initialStatus.ToString() }, ct: ct);

        return await GetByIdAsync(reservation.Id, ct);
    }

    public async Task<ReservationDto> ConfirmAsync(Guid id, CancellationToken ct = default)
    {
        var res = await _db.Reservations.Include(r => r.Rooms).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Reservation not found.");

        if (res.Status != ReservationStatus.Pending)
            throw new BusinessRuleException("Only Pending reservations can be confirmed.");

        res.Status = ReservationStatus.Confirmed;
        res.UpdatedAt = DateTimeOffset.UtcNow;
        res.UpdatedBy = _currentUser.Email;

        _db.ReservationStatusHistory.Add(new ReservationStatusHistory
        {
            ReservationId = res.Id,
            FromStatus = ReservationStatus.Pending,
            ToStatus = ReservationStatus.Confirmed,
            Reason = "Confirmed by staff",
            ChangedBy = _currentUser.Email
        });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ConfirmReservation", "Reservation", res.Id.ToString(), ct: ct);
        return await GetByIdAsync(res.Id, ct);
    }

    public async Task<ReservationDto> UpdateStayAsync(Guid id, UpdateReservationStayRequest request, CancellationToken ct = default)
    {
        if (request.CheckOutDate <= request.CheckInDate)
            throw new BusinessRuleException("Check-out date must be after check-in date.");

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var res = await _db.Reservations.Include(r => r.Rooms).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Reservation not found.");

        if (res.Status is ReservationStatus.Cancelled or ReservationStatus.CheckedOut or ReservationStatus.NoShow)
            throw new BusinessRuleException("Cannot modify a closed or cancelled reservation.");

        var roomIds = res.Rooms.Select(rr => rr.RoomId).ToList();
        var conflicts = await _db.ReservationRooms
            .Include(rr => rr.Room)
            .Where(rr => rr.ReservationId != id
                         && roomIds.Contains(rr.RoomId)
                         && ActiveBlockingStatuses.Contains(rr.Reservation.Status)
                         && rr.CheckInDate < request.CheckOutDate
                         && rr.CheckOutDate > request.CheckInDate)
            .Select(rr => rr.Room.RoomNumber)
            .Distinct()
            .ToListAsync(ct);

        if (conflicts.Count > 0)
            throw new ConflictException($"Room(s) {string.Join(", ", conflicts)} are not available for the new dates.");

        var nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
        res.CheckInDate = request.CheckInDate;
        res.CheckOutDate = request.CheckOutDate;
        res.Nights = nights;
        res.AdultsCount = request.AdultsCount;
        res.ChildrenCount = request.ChildrenCount;
        res.DiscountAmount = request.DiscountAmount;
        res.Notes = request.Notes?.Trim();
        res.UpdatedAt = DateTimeOffset.UtcNow;
        res.UpdatedBy = _currentUser.Email;

        decimal subTotal = 0m;
        foreach (var rr in res.Rooms)
        {
            rr.CheckInDate = request.CheckInDate;
            rr.CheckOutDate = request.CheckOutDate;
            rr.Nights = nights;
            rr.TotalRoomAmount = Math.Round(rr.AgreedNightlyRate * nights, 2, MidpointRounding.AwayFromZero);
            subTotal += rr.TotalRoomAmount;
        }

        var settings = await _db.HotelSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var taxable = Math.Max(0m, subTotal - res.DiscountAmount);
        res.SubTotal = subTotal;
        res.ServiceChargeAmount = Math.Round(taxable * ((settings?.ServiceChargePercent ?? 0m) / 100m), 2, MidpointRounding.AwayFromZero);
        res.TaxAmount = Math.Round((taxable + res.ServiceChargeAmount) * ((settings?.TaxRatePercent ?? 0m) / 100m), 2, MidpointRounding.AwayFromZero);
        res.TotalAmount = taxable + res.ServiceChargeAmount + res.TaxAmount;

        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        await _audit.LogAsync("UpdateReservationStay", "Reservation", res.Id.ToString(), newValues: request, ct: ct);
        return await GetByIdAsync(res.Id, ct);
    }

    public async Task<ReservationDto> ChangeRoomAsync(Guid id, ChangeReservationRoomRequest request, CancellationToken ct = default)
    {
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var res = await _db.Reservations.Include(r => r.Rooms).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Reservation not found.");

        if (res.Status is ReservationStatus.Cancelled or ReservationStatus.CheckedOut or ReservationStatus.NoShow)
            throw new BusinessRuleException("Cannot change room on a closed or cancelled reservation.");

        var currentRr = res.Rooms.FirstOrDefault(rr => rr.RoomId == request.CurrentRoomId)
            ?? throw new NotFoundException("Current room is not part of this reservation.");

        var newRoom = await _db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == request.NewRoomId, ct)
            ?? throw new NotFoundException("Target room not found.");

        if (newRoom.Status is RoomStatus.OutOfService or RoomStatus.Maintenance)
            throw new BusinessRuleException($"Room {newRoom.RoomNumber} is unavailable ({newRoom.Status}).");

        var hasConflict = await _db.ReservationRooms.AnyAsync(rr =>
            rr.ReservationId != id
            && rr.RoomId == request.NewRoomId
            && ActiveBlockingStatuses.Contains(rr.Reservation.Status)
            && rr.CheckInDate < res.CheckOutDate
            && rr.CheckOutDate > res.CheckInDate, ct);

        if (hasConflict)
            throw new ConflictException($"Room {newRoom.RoomNumber} is already booked during these dates.");

        currentRr.RoomId = newRoom.Id;
        currentRr.RoomTypeId = newRoom.RoomTypeId;

        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        await _audit.LogAsync("ChangeReservationRoom", "Reservation", res.Id.ToString(), newValues: request, ct: ct);
        return await GetByIdAsync(res.Id, ct);
    }

    public async Task<ReservationDto> CancelAsync(Guid id, CancelReservationRequest request, CancellationToken ct = default)
    {
        var res = await _db.Reservations.Include(r => r.Rooms).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Reservation not found.");

        if (res.Status is ReservationStatus.CheckedIn or ReservationStatus.CheckedOut or ReservationStatus.Cancelled)
            throw new BusinessRuleException($"Cannot cancel a reservation in '{res.Status}' status.");

        var oldStatus = res.Status;
        res.Status = ReservationStatus.Cancelled;
        res.CancellationReason = request.Reason.Trim();
        res.UpdatedAt = DateTimeOffset.UtcNow;
        res.UpdatedBy = _currentUser.Email;

        _db.ReservationStatusHistory.Add(new ReservationStatusHistory
        {
            ReservationId = res.Id,
            FromStatus = oldStatus,
            ToStatus = ReservationStatus.Cancelled,
            Reason = res.CancellationReason,
            ChangedBy = _currentUser.Email
        });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CancelReservation", "Reservation", res.Id.ToString(), newValues: new { request.Reason }, ct: ct);
        return await GetByIdAsync(res.Id, ct);
    }

    private async Task<string> GenerateReservationNumberAsync(CancellationToken ct)
    {
        var prefix = $"RES-{DateTime.UtcNow:yyyyMMdd}";
        var count = await _db.Reservations.CountAsync(r => r.ReservationNumber.StartsWith(prefix), ct);
        return $"{prefix}-{(count + 1):D4}";
    }

    private static ReservationDto Map(Reservation r) => new(
        r.Id, r.ReservationNumber, r.PrimaryGuestId, r.PrimaryGuest.FullName, r.PrimaryGuest.Phone,
        r.RatePlanId, r.RatePlan.Name, r.CheckInDate, r.CheckOutDate, r.Nights,
        r.AdultsCount, r.ChildrenCount, r.Source, r.Status,
        r.SubTotal, r.DiscountAmount, r.TaxAmount, r.ServiceChargeAmount, r.TotalAmount,
        r.DepositRequired, r.Notes, r.CancellationReason, r.CreatedAt,
        r.Rooms.Select(rr => new ReservationRoomDto(
            rr.Id, rr.RoomId, rr.Room.RoomNumber, rr.RoomTypeId, rr.RoomType.Name,
            rr.AgreedNightlyRate, rr.Nights, rr.TotalRoomAmount, rr.CheckInDate, rr.CheckOutDate
        )).ToList()
    );
}

