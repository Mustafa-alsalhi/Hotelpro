using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/guest-services")]
public class GuestServicesController : ControllerBase
{
    private readonly IGuestServicesAppService _service;

    public GuestServicesController(IGuestServicesAppService service) => _service = service;

    [HttpGet("catalog")]
    [HasPermission(PermissionsCatalog.GuestServices.View)]
    public async Task<ActionResult<IReadOnlyList<GuestServiceDto>>> GetCatalog([FromQuery] bool includeInactive = true, CancellationToken ct = default)
        => Ok(await _service.GetCatalogAsync(includeInactive, ct));

    [HttpPost("catalog")]
    [HasPermission(PermissionsCatalog.GuestServices.Create)]
    public async Task<ActionResult<GuestServiceDto>> CreateService(
        [FromBody] SaveGuestServiceRequest request,
        [FromServices] IValidator<SaveGuestServiceRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateServiceAsync(request, ct));
    }

    [HttpPut("catalog/{id:guid}")]
    [HasPermission(PermissionsCatalog.GuestServices.Edit)]
    public async Task<ActionResult<GuestServiceDto>> UpdateService(
        Guid id,
        [FromBody] SaveGuestServiceRequest request,
        [FromServices] IValidator<SaveGuestServiceRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateServiceAsync(id, request, ct));
    }

    [HttpGet("orders")]
    [HasPermission(PermissionsCatalog.GuestServices.View)]
    public async Task<ActionResult<PagedResult<ServiceOrderDto>>> GetOrders(
        [FromQuery] PaginationParams pagination,
        [FromQuery] ServiceOrderStatus? status,
        [FromQuery] Guid? reservationId,
        CancellationToken ct)
        => Ok(await _service.GetOrdersAsync(pagination, status, reservationId, ct));

    [HttpPost("orders")]
    [HasPermission(PermissionsCatalog.GuestServices.Create)]
    public async Task<ActionResult<ServiceOrderDto>> CreateOrder(
        [FromBody] CreateServiceOrderRequest request,
        [FromServices] IValidator<CreateServiceOrderRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateOrderAsync(request, ct));
    }

    [HttpPatch("orders/{id:guid}/status")]
    [HasPermission(PermissionsCatalog.GuestServices.Edit)]
    public async Task<ActionResult<ServiceOrderDto>> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateServiceOrderStatusRequest request,
        CancellationToken ct)
        => Ok(await _service.UpdateOrderStatusAsync(id, request, ct));
}

