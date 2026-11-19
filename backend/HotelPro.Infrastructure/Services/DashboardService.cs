using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Dashboard;
using HotelPro.Domain.Enums;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly HotelProDbContext _db;

    public DashboardService(HotelProDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
        {
            (from, to) = (to, from);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rooms = await _db.Rooms.Include(r => r.RoomType).AsNoTracking().ToListAsync(ct);

        var totalRooms = rooms.Count;
        var available = rooms.Count(r => r.Status == RoomStatus.Available);
        var reserved = rooms.Count(r => r.Status == RoomStatus.Reserved);
        var occupied = rooms.Count(r => r.Status == RoomStatus.Occupied);
        var cleaning = rooms.Count(r => r.Status == RoomStatus.Cleaning);
        var maintenance = rooms.Count(r => r.Status == RoomStatus.Maintenance);
        var outOfService = rooms.Count(r => r.Status == RoomStatus.OutOfService);

        var serviceable = totalRooms - outOfService;
        var occupancyPercent = serviceable > 0
            ? Math.Round((occupied / (decimal)serviceable) * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var checkedInRes = await _db.Reservations.AsNoTracking()
            .Where(r => r.Status == ReservationStatus.CheckedIn)
            .ToListAsync(ct);
        var currentGuests = checkedInRes.Sum(r => r.AdultsCount + r.ChildrenCount);

        var todayBookings = await _db.Reservations.AsNoTracking()
            .CountAsync(r => r.CheckInDate == today || r.CheckOutDate == today, ct);

        var expectedArrivalsToday = await _db.Reservations.AsNoTracking()
            .CountAsync(r => r.Status == ReservationStatus.Confirmed && r.CheckInDate == today, ct);

        var expectedDeparturesToday = checkedInRes.Count(r => r.CheckOutDate == today);

        var revenues = await _db.RevenueRecords.AsNoTracking()
            .Where(r => r.RecognizedDate >= from && r.RecognizedDate <= to)
            .ToListAsync(ct);

        var expenses = await _db.Expenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync(ct);

        var totalRev = revenues.Sum(r => r.Amount);
        var totalExp = expenses.Sum(e => e.Amount);

        var pendingHk = await _db.HousekeepingTasks.CountAsync(t => t.Status != HousekeepingStatus.Inspected, ct);
        var openMaint = await _db.MaintenanceRequests.CountAsync(m => m.Status != MaintenanceStatus.Closed && m.Status != MaintenanceStatus.Completed, ct);
        var lowStock = await _db.InventoryItems.CountAsync(i => i.CurrentStock <= i.ReorderLevel, ct);

        var revByDate = revenues.GroupBy(r => r.RecognizedDate).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var expByDate = expenses.GroupBy(e => e.ExpenseDate).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var allDates = revByDate.Keys.Union(expByDate.Keys).OrderBy(d => d).ToList();
        var revVsExp = allDates
            .Select(d => new ChartSeriesPointDto(d.ToString("O"), revByDate.GetValueOrDefault(d), expByDate.GetValueOrDefault(d)))
            .ToList();

        // Monthly Revenue vs Expense series (at least 6 months ending at 'to', or wider if 'from' is earlier)
        var sixMonthsAgo = new DateOnly(to.Year, to.Month, 1).AddMonths(-5);
        var monthlyStart = from < sixMonthsAgo ? new DateOnly(from.Year, from.Month, 1) : sixMonthsAgo;
        var monthlyEnd = new DateOnly(to.Year, to.Month, DateTime.DaysInMonth(to.Year, to.Month));

        var monthlyRevs = await _db.RevenueRecords.AsNoTracking()
            .Where(r => r.RecognizedDate >= monthlyStart && r.RecognizedDate <= monthlyEnd)
            .ToListAsync(ct);
        var monthlyExps = await _db.Expenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= monthlyStart && e.ExpenseDate <= monthlyEnd)
            .ToListAsync(ct);

        var monthlyRevLookup = monthlyRevs
            .GroupBy(r => (r.RecognizedDate.Year, r.RecognizedDate.Month))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var monthlyExpLookup = monthlyExps
            .GroupBy(e => (e.ExpenseDate.Year, e.ExpenseDate.Month))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var monthlySeries = new List<ChartSeriesPointDto>();
        for (var cursor = monthlyStart; cursor <= monthlyEnd; cursor = cursor.AddMonths(1))
        {
            var key = (cursor.Year, cursor.Month);
            monthlySeries.Add(new ChartSeriesPointDto(
                $"{cursor.Year}-{cursor.Month:D2}",
                monthlyRevLookup.GetValueOrDefault(key),
                monthlyExpLookup.GetValueOrDefault(key)));
        }

        var bookingsInPeriod = await _db.Reservations.AsNoTracking()
            .Where(r => r.CheckInDate >= from && r.CheckInDate <= to)
            .ToListAsync(ct);

        var bySource = bookingsInPeriod
            .GroupBy(r => r.Source.ToString())
            .Select(g => new ChartSeriesPointDto(g.Key, g.Count()))
            .ToList();

        var bookingsByStatus = Enum.GetValues<ReservationStatus>()
            .Select(status =>
            {
                var matching = bookingsInPeriod.Where(r => r.Status == status).ToList();
                return new ChartSeriesPointDto(status.ToString(), matching.Count, matching.Sum(x => x.TotalAmount));
            })
            .ToList();

        var byRoomType = rooms
            .GroupBy(r => r.RoomType.Name)
            .Select(g => new ChartSeriesPointDto(g.Key, g.Count()))
            .ToList();

        var resRoomsInPeriod = await _db.ReservationRooms
            .Include(rr => rr.Reservation)
            .Include(rr => rr.RoomType)
            .AsNoTracking()
            .Where(rr => rr.CheckInDate <= to
                      && rr.CheckOutDate >= from
                      && rr.Reservation.Status != ReservationStatus.Cancelled
                      && rr.Reservation.Status != ReservationStatus.NoShow)
            .ToListAsync(ct);

        var roomTypePerformance = rooms
            .GroupBy(r => new { r.RoomTypeId, r.RoomType.Name })
            .Select(g =>
            {
                var rtTotal = g.Count();
                var rtOutOfService = g.Count(r => r.Status == RoomStatus.OutOfService);
                var rtServiceable = rtTotal - rtOutOfService;
                var rtOccupied = g.Count(r => r.Status == RoomStatus.Occupied);
                var rtOccPct = rtServiceable > 0
                    ? Math.Round((rtOccupied / (decimal)rtServiceable) * 100m, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                var rtBookings = resRoomsInPeriod.Where(rr => rr.RoomTypeId == g.Key.RoomTypeId).ToList();
                return new RoomTypePerformanceDto(
                    g.Key.Name,
                    rtTotal,
                    rtOccupied,
                    rtBookings.Select(b => b.ReservationId).Distinct().Count(),
                    rtBookings.Sum(b => b.TotalRoomAmount),
                    rtOccPct);
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.OccupiedRooms)
            .ToList();

        var serviceItems = await _db.ServiceOrderItems
            .Include(i => i.GuestService)
            .AsNoTracking()
            .Select(i => new { i.GuestService.Name, i.Quantity, i.LineTotal })
            .ToListAsync(ct);

        var topServices = serviceItems
            .GroupBy(i => i.Name)
            .Select(g => new ChartSeriesPointDto(g.Key, g.Sum(x => x.Quantity), g.Sum(x => x.LineTotal)))
            .OrderByDescending(x => x.Value)
            .Take(6)
            .ToList();

        return new DashboardSummaryDto(
            totalRooms, available, reserved, occupied, cleaning, maintenance, outOfService,
            occupancyPercent, currentGuests, todayBookings, expectedArrivalsToday, expectedDeparturesToday,
            totalRev, totalExp, totalRev - totalExp,
            pendingHk, openMaint, lowStock,
            revVsExp, bySource, byRoomType, topServices,
            monthlySeries, bookingsByStatus, roomTypePerformance);
    }
}
