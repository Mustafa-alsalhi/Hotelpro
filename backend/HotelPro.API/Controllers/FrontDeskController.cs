using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/front-desk")]
public class FrontDeskController : ControllerBase
{
    private readonly IFrontDeskService _service;

    public FrontDeskController(IFrontDeskService service) => _service = service;

    [HttpPost("check-in")]
    [HasPermission(PermissionsCatalog.Reservations.CheckIn)]
    public async Task<ActionResult<IReadOnlyList<CheckInRecordDto>>> CheckIn(
        [FromBody] CheckInRequest request,
        [FromServices] IValidator<CheckInRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CheckInAsync(request, ct));
    }

    [HttpGet("check-out/{reservationId:guid}/summary")]
    [HasPermission(PermissionsCatalog.Reservations.CheckOut)]
    public async Task<ActionResult<CheckOutSummaryDto>> GetCheckOutSummary(Guid reservationId, CancellationToken ct)
        => Ok(await _service.GetCheckOutSummaryAsync(reservationId, ct));

    [HttpPost("check-out")]
    [HasPermission(PermissionsCatalog.Reservations.CheckOut)]
    public async Task<ActionResult<IReadOnlyList<CheckOutRecordDto>>> CheckOut(
        [FromBody] CheckOutRequest request,
        CancellationToken ct)
        => Ok(await _service.CheckOutAsync(request, ct));
}

