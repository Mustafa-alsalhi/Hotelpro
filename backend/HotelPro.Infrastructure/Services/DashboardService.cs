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

        var bookingsInPeriod = await _db.Reservations.AsNoTracking()
            .Where(r => r.CheckInDate >= from && r.CheckInDate <= to)
            .ToListAsync(ct);

        var bySource = bookingsInPeriod
            .GroupBy(r => r.Source.ToString())
            .Select(g => new ChartSeriesPointDto(g.Key, g.Count()))
            .ToList();

        var byRoomType = rooms
            .GroupBy(r => r.RoomType.Name)
            .Select(g => new ChartSeriesPointDto(g.Key, g.Count()))
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
            revVsExp, bySource, byRoomType, topServices);
    }
}
