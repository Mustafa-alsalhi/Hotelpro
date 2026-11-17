using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/housekeeping")]
public class HousekeepingController : ControllerBase
{
    private readonly IHousekeepingService _service;

    public HousekeepingController(IHousekeepingService service) => _service = service;

    [HttpGet]
    [HasPermission(PermissionsCatalog.Housekeeping.View)]
    public async Task<ActionResult<PagedResult<HousekeepingTaskDto>>> GetTasks(
        [FromQuery] PaginationParams pagination,
        [FromQuery] HousekeepingStatus? status,
        [FromQuery] TaskPriority? priority,
        CancellationToken ct)
        => Ok(await _service.GetTasksAsync(pagination, status, priority, ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Housekeeping.Create)]
    public async Task<ActionResult<HousekeepingTaskDto>> CreateTask(
        [FromBody] CreateHousekeepingTaskRequest request,
        CancellationToken ct)
        => Ok(await _service.CreateTaskAsync(request, ct));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.Housekeeping.Edit)]
    public async Task<ActionResult<HousekeepingTaskDto>> UpdateTask(
        Guid id,
        [FromBody] UpdateHousekeepingTaskRequest request,
        CancellationToken ct)
        => Ok(await _service.UpdateTaskAsync(id, request, ct));
}

