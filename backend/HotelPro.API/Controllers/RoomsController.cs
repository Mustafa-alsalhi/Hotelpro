using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _service;

    public RoomsController(IRoomService service) => _service = service;

    [HttpGet]
    [HasPermission(PermissionsCatalog.Rooms.View)]
    public async Task<ActionResult<PagedResult<RoomDto>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] int? floor,
        [FromQuery] Guid? roomTypeId,
        [FromQuery] RoomStatus? status,
        CancellationToken ct)
        => Ok(await _service.GetRoomsAsync(pagination, floor, roomTypeId, status, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionsCatalog.Rooms.View)]
    public async Task<ActionResult<RoomDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Rooms.Create)]
    public async Task<ActionResult<RoomDto>> Create(
        [FromBody] SaveRoomRequest request,
        [FromServices] IValidator<SaveRoomRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.Rooms.Edit)]
    public async Task<ActionResult<RoomDto>> Update(
        Guid id,
        [FromBody] SaveRoomRequest request,
        [FromServices] IValidator<SaveRoomRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateAsync(id, request, ct));
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission(PermissionsCatalog.Rooms.Edit)]
    public async Task<ActionResult<RoomDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeRoomStatusRequest request,
        CancellationToken ct)
        => Ok(await _service.ChangeStatusAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionsCatalog.Rooms.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

