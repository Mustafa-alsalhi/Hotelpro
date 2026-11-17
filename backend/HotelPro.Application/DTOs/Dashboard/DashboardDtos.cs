namespace HotelPro.Application.DTOs.Dashboard;

public record ChartSeriesPointDto(string Label, decimal Value, decimal SecondaryValue = 0);

public record DashboardSummaryDto(
    int TotalRooms,
    int AvailableRooms,
    int ReservedRooms,
    int OccupiedRooms,
    int CleaningRooms,
    int MaintenanceRooms,
    int OutOfServiceRooms,
    decimal OccupancyRatePercent,
    int CurrentInHouseGuests,
    int TodayBookings,
    int ExpectedArrivalsToday,
    int ExpectedDeparturesToday,
    decimal TotalRevenues,
    decimal TotalExpenses,
    decimal NetOperatingResult,
    int PendingHousekeepingTasks,
    int OpenMaintenanceRequests,
    int LowStockItemsCount,
    IReadOnlyList<ChartSeriesPointDto> RevenueVsExpenseSeries,
    IReadOnlyList<ChartSeriesPointDto> BookingsBySource,
    IReadOnlyList<ChartSeriesPointDto> RoomTypeDistribution,
    IReadOnlyList<ChartSeriesPointDto> TopServices
);

