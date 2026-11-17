using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Rates;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/rate-plans")]
public class RatePlansController : ControllerBase
{
    private readonly IRateManagementService _service;

    public RatePlansController(IRateManagementService service) => _service = service;

    [HttpGet]
    [HasPermission(PermissionsCatalog.Rates.View)]
    public async Task<ActionResult<IReadOnlyList<RatePlanDto>>> GetAll([FromQuery] bool includeInactive = true, CancellationToken ct = default)
        => Ok(await _service.GetRatePlansAsync(includeInactive, ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Rates.Create)]
    public async Task<ActionResult<RatePlanDto>> Create(
        [FromBody] SaveRatePlanRequest request,
        [FromServices] IValidator<SaveRatePlanRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateRatePlanAsync(request, ct));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.Rates.Edit)]
    public async Task<ActionResult<RatePlanDto>> Update(
        Guid id,
        [FromBody] SaveRatePlanRequest request,
        [FromServices] IValidator<SaveRatePlanRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateRatePlanAsync(id, request, ct));
    }

    [HttpGet("room-rates")]
    [HasPermission(PermissionsCatalog.Rates.View)]
    public async Task<ActionResult<IReadOnlyList<RoomRateDto>>> GetRoomRates(
        [FromQuery] Guid? roomTypeId,
        [FromQuery] Guid? ratePlanId,
        CancellationToken ct)
        => Ok(await _service.GetRoomRatesAsync(roomTypeId, ratePlanId, ct));

    [HttpPost("room-rates")]
    [HasPermission(PermissionsCatalog.Rates.Create)]
    public async Task<ActionResult<RoomRateDto>> CreateRoomRate(
        [FromBody] SaveRoomRateRequest request,
        [FromServices] IValidator<SaveRoomRateRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateRoomRateAsync(request, ct));
    }

    [HttpDelete("room-rates/{id:guid}")]
    [HasPermission(PermissionsCatalog.Rates.Delete)]
    public async Task<IActionResult> DeleteRoomRate(Guid id, CancellationToken ct)
    {
        await _service.DeleteRoomRateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("quote")]
    [HasPermission(PermissionsCatalog.Reservations.View)]
    public async Task<ActionResult<RateQuoteDto>> CalculateQuote([FromBody] RateQuoteRequest request, CancellationToken ct)
        => Ok(await _service.CalculateQuoteAsync(request, ct));
}

