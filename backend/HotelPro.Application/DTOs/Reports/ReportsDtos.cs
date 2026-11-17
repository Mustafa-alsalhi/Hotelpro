namespace HotelPro.Application.DTOs.Reports;

public record OccupancyReportRowDto(
    DateOnly Date,
    int TotalServiceableRooms,
    int OccupiedRooms,
    int OutOfServiceRooms,
    decimal OccupancyRatePercent
);

public record OccupancyReportDto(
    DateOnly From,
    DateOnly To,
    decimal AverageOccupancyPercent,
    IReadOnlyList<OccupancyReportRowDto> Rows
);

