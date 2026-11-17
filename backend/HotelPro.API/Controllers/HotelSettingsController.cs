using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Hotel;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/hotel-settings")]
public class HotelSettingsController : ControllerBase
{
    private readonly IHotelSettingsService _service;

    public HotelSettingsController(IHotelSettingsService service) => _service = service;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<HotelSettingsDto>> Get(CancellationToken ct)
    {
        return Ok(await _service.GetSettingsAsync(ct));
    }

    [HttpPut]
    [HasPermission(PermissionsCatalog.Settings.ManageSettings)]
    public async Task<ActionResult<HotelSettingsDto>> Update(
        [FromBody] UpdateHotelSettingsRequest request,
        [FromServices] IValidator<UpdateHotelSettingsRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateSettingsAsync(request, ct));
    }
}

