using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Guests;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/guests")]
public class GuestsController : ControllerBase
{
    private readonly IGuestService _service;

    public GuestsController(IGuestService service) => _service = service;

    [HttpGet]
    [HasPermission(PermissionsCatalog.Guests.View)]
    public async Task<ActionResult<PagedResult<GuestDto>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] bool? isVip,
        CancellationToken ct)
        => Ok(await _service.GetGuestsAsync(pagination, isVip, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionsCatalog.Guests.View)]
    public async Task<ActionResult<GuestDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Guests.Create)]
    public async Task<ActionResult<GuestDto>> Create(
        [FromBody] SaveGuestRequest request,
        [FromServices] IValidator<SaveGuestRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.Guests.Edit)]
    public async Task<ActionResult<GuestDto>> Update(
        Guid id,
        [FromBody] SaveGuestRequest request,
        [FromServices] IValidator<SaveGuestRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateAsync(id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionsCatalog.Guests.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

