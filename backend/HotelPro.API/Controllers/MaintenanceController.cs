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
[Route("api/v1/maintenance")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _service;

    public MaintenanceController(IMaintenanceService service) => _service = service;

    [HttpGet]
    [HasPermission(PermissionsCatalog.Maintenance.View)]
    public async Task<ActionResult<PagedResult<MaintenanceRequestDto>>> GetRequests(
        [FromQuery] PaginationParams pagination,
        [FromQuery] MaintenanceStatus? status,
        [FromQuery] TaskPriority? priority,
        CancellationToken ct)
        => Ok(await _service.GetRequestsAsync(pagination, status, priority, ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Maintenance.Create)]
    public async Task<ActionResult<MaintenanceRequestDto>> CreateRequest(
        [FromBody] CreateMaintenanceRequest request,
        [FromServices] IValidator<CreateMaintenanceRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateRequestAsync(request, ct));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.Maintenance.Edit)]
    public async Task<ActionResult<MaintenanceRequestDto>> UpdateRequest(
        Guid id,
        [FromBody] UpdateMaintenanceRequest request,
        CancellationToken ct)
        => Ok(await _service.UpdateRequestAsync(id, request, ct));
}

