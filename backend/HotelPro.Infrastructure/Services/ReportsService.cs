using System.Text;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Reports;
using HotelPro.Domain.Enums;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class ReportsService : IReportsService
{
    private readonly HotelProDbContext _db;

    public ReportsService(HotelProDbContext db) => _db = db;

    public async Task<OccupancyReportDto> GetOccupancyReportAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rooms = await _db.Rooms.AsNoTracking().ToListAsync(ct);
        var outOfServiceCount = rooms.Count(r => r.Status == RoomStatus.OutOfService);
        var serviceable = Math.Max(0, rooms.Count - outOfServiceCount);

        var activeReservations = await _db.ReservationRooms
            .Include(rr => rr.Reservation)
            .AsNoTracking()
            .Where(rr => rr.Reservation.Status != ReservationStatus.Cancelled
                         && rr.Reservation.Status != ReservationStatus.NoShow
                         && rr.CheckInDate <= to
                         && rr.CheckOutDate > from)
            .ToListAsync(ct);

        var rows = new List<OccupancyReportRowDto>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var occupiedOnDay = activeReservations
                .Where(rr => rr.CheckInDate <= d && rr.CheckOutDate > d)
                .Select(rr => rr.RoomId)
                .Distinct()
                .Count();

            var rate = serviceable > 0
                ? Math.Round((occupiedOnDay / (decimal)serviceable) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;

            rows.Add(new OccupancyReportRowDto(d, serviceable, occupiedOnDay, outOfServiceCount, rate));
        }

        var avg = rows.Count > 0 ? Math.Round(rows.Average(r => r.OccupancyRatePercent), 2, MidpointRounding.AwayFromZero) : 0m;
        return new OccupancyReportDto(from, to, avg, rows);
    }

    public async Task<byte[]> ExportOccupancyCsvAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var report = await GetOccupancyReportAsync(from, to, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Date,ServiceableRooms,OccupiedRooms,OutOfServiceRooms,OccupancyRatePercent");
        foreach (var row in report.Rows)
        {
            sb.AppendLine($"{row.Date:O},{row.TotalServiceableRooms},{row.OccupiedRooms},{row.OutOfServiceRooms},{row.OccupancyRatePercent:F2}");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportReservationsCsvAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var list = await _db.Reservations
            .Include(r => r.PrimaryGuest)
            .AsNoTracking()
            .Where(r => r.CheckInDate >= from && r.CheckInDate <= to)
            .OrderBy(r => r.CheckInDate)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("ReservationNumber,GuestName,CheckInDate,CheckOutDate,Nights,Status,Source,TotalAmount");
        foreach (var r in list)
        {
            sb.AppendLine($"\"{r.ReservationNumber}\",\"{r.PrimaryGuest.FullName}\",{r.CheckInDate:O},{r.CheckOutDate:O},{r.Nights},{r.Status},{r.Source},{r.TotalAmount:F2}");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportFinancialCsvAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var revenues = await _db.RevenueRecords.AsNoTracking()
            .Where(r => r.RecognizedDate >= from && r.RecognizedDate <= to)
            .ToListAsync(ct);
        var expenses = await _db.Expenses.Include(e => e.Category).AsNoTracking()
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Type,Date,CategoryOrSource,Description,Amount");
        foreach (var r in revenues)
            sb.AppendLine($"Revenue,{r.RecognizedDate:O},{r.Source},\"{r.Description}\",{r.Amount:F2}");
        foreach (var e in expenses)
            sb.AppendLine($"Expense,{e.ExpenseDate:O},\"{e.Category.Name}\",\"{e.Description}\",-{e.Amount:F2}");

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}

