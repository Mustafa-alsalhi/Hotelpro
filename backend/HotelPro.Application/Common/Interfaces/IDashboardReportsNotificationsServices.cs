using HotelPro.Application.DTOs.Dashboard;
using HotelPro.Application.DTOs.Notifications;
using HotelPro.Application.DTOs.Reports;

namespace HotelPro.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

public interface IReportsService
{
    Task<OccupancyReportDto> GetOccupancyReportAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<byte[]> ExportOccupancyCsvAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<byte[]> ExportReservationsCsvAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<byte[]> ExportFinancialCsvAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(int take = 25, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);
    Task MarkAsReadAsync(Guid id, CancellationToken ct = default);
    Task MarkAllAsReadAsync(CancellationToken ct = default);
    Task CreateAsync(string title, string message, string type = "Info", Guid? targetUserId = null, string? targetRole = null, CancellationToken ct = default);
}

