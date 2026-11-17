using HotelPro.API.Authorization;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Reports;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _service;

    public ReportsController(IReportsService service) => _service = service;

    [HttpGet("occupancy")]
    [HasPermission(PermissionsCatalog.Reports.ViewReports)]
    public async Task<ActionResult<OccupancyReportDto>> GetOccupancy(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var start = from ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _service.GetOccupancyReportAsync(start, end, ct));
    }

    [HttpGet("export/occupancy-csv")]
    [HasPermission(PermissionsCatalog.Reports.ViewReports)]
    public async Task<IActionResult> ExportOccupancy([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var bytes = await _service.ExportOccupancyCsvAsync(from, to, ct);
        return File(bytes, "text/csv", $"occupancy-report-{from:O}-{to:O}.csv");
    }

    [HttpGet("export/reservations-csv")]
    [HasPermission(PermissionsCatalog.Reports.ViewReports)]
    public async Task<IActionResult> ExportReservations([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var bytes = await _service.ExportReservationsCsvAsync(from, to, ct);
        return File(bytes, "text/csv", $"reservations-report-{from:O}-{to:O}.csv");
    }

    [HttpGet("export/financial-csv")]
    [HasPermission(PermissionsCatalog.Reports.ViewReports)]
    public async Task<IActionResult> ExportFinancial([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var bytes = await _service.ExportFinancialCsvAsync(from, to, ct);
        return File(bytes, "text/csv", $"financial-report-{from:O}-{to:O}.csv");
    }
}

