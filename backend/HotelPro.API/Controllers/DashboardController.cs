using HotelPro.API.Authorization;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Dashboard;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("summary")]
    [HasPermission(PermissionsCatalog.Dashboard.View)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var start = from ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _service.GetSummaryAsync(start, end, ct));
    }
}

