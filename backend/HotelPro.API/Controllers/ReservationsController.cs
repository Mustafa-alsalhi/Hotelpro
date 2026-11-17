using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Reservations;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _service;

    public ReservationsController(IReservationService service) => _service = service;

    [HttpGet]
    [HasPermission(PermissionsCatalog.Reservations.View)]
    public async Task<ActionResult<PagedResult<ReservationDto>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] ReservationStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken ct)
        => Ok(await _service.GetReservationsAsync(pagination, status, fromDate, toDate, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionsCatalog.Reservations.View)]
    public async Task<ActionResult<ReservationDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpGet("available-rooms")]
    [HasPermission(PermissionsCatalog.Reservations.View)]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAvailableRooms(
        [FromQuery] DateOnly checkIn,
        [FromQuery] DateOnly checkOut,
        [FromQuery] Guid? roomTypeId,
        CancellationToken ct)
        => Ok(await _service.GetAvailableRoomsAsync(checkIn, checkOut, roomTypeId, ct));

    [HttpGet("calendar")]
    [HasPermission(PermissionsCatalog.Reservations.View)]
    public async Task<ActionResult<BookingCalendarDto>> GetCalendar(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
        => Ok(await _service.GetCalendarAsync(from, to, ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Reservations.Create)]
    public async Task<ActionResult<ReservationDto>> Create(
        [FromBody] CreateReservationRequest request,
        [FromServices] IValidator<CreateReservationRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/confirm")]
    [HasPermission(PermissionsCatalog.Reservations.Approve)]
    public async Task<ActionResult<ReservationDto>> Confirm(Guid id, CancellationToken ct)
        => Ok(await _service.ConfirmAsync(id, ct));

    [HttpPut("{id:guid}/stay")]
    [HasPermission(PermissionsCatalog.Reservations.Edit)]
    public async Task<ActionResult<ReservationDto>> UpdateStay(
        Guid id,
        [FromBody] UpdateReservationStayRequest request,
        [FromServices] IValidator<UpdateReservationStayRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateStayAsync(id, request, ct));
    }

    [HttpPost("{id:guid}/change-room")]
    [HasPermission(PermissionsCatalog.Reservations.Edit)]
    public async Task<ActionResult<ReservationDto>> ChangeRoom(
        Guid id,
        [FromBody] ChangeReservationRoomRequest request,
        CancellationToken ct)
        => Ok(await _service.ChangeRoomAsync(id, request, ct));

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionsCatalog.Reservations.Cancel)]
    public async Task<ActionResult<ReservationDto>> Cancel(
        Guid id,
        [FromBody] CancelReservationRequest request,
        CancellationToken ct)
        => Ok(await _service.CancelAsync(id, request, ct));
}

