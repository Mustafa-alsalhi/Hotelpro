using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/room-types")]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomTypeService _service;

    public RoomTypesController(IRoomTypeService service) => _service = service;

    [HttpGet]
    [HasPermission(PermissionsCatalog.Rooms.View)]
    public async Task<ActionResult<IReadOnlyList<RoomTypeDto>>> GetAll([FromQuery] bool includeInactive = true, CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(includeInactive, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionsCatalog.Rooms.View)]
    public async Task<ActionResult<RoomTypeDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Rooms.Create)]
    public async Task<ActionResult<RoomTypeDto>> Create(
        [FromBody] SaveRoomTypeRequest request,
        [FromServices] IValidator<SaveRoomTypeRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.Rooms.Edit)]
    public async Task<ActionResult<RoomTypeDto>> Update(
        Guid id,
        [FromBody] SaveRoomTypeRequest request,
        [FromServices] IValidator<SaveRoomTypeRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateAsync(id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionsCatalog.Rooms.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/amenities")]
public class AmenitiesController : ControllerBase
{
    private readonly IAmenityService _service;

    public AmenitiesController(IAmenityService service) => _service = service;

    [HttpGet]
    [HasPermission(PermissionsCatalog.Rooms.View)]
    public async Task<ActionResult<IReadOnlyList<AmenityDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Rooms.Create)]
    public async Task<ActionResult<AmenityDto>> Create([FromBody] CreateAmenityRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionsCatalog.Rooms.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

