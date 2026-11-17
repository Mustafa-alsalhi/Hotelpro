using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.HR;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service) => _service = service;

    [HttpGet("departments")]
    [HasPermission(PermissionsCatalog.Employees.View)]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetDepartments(CancellationToken ct)
        => Ok(await _service.GetDepartmentsAsync(ct));

    [HttpPost("departments")]
    [HasPermission(PermissionsCatalog.Employees.Create)]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] SaveDepartmentRequest request, CancellationToken ct)
        => Ok(await _service.CreateDepartmentAsync(request, ct));

    [HttpGet]
    [HasPermission(PermissionsCatalog.Employees.View)]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> GetEmployees(
        [FromQuery] PaginationParams pagination,
        [FromQuery] Guid? departmentId,
        [FromQuery] EmployeeStatus? status,
        CancellationToken ct)
        => Ok(await _service.GetEmployeesAsync(pagination, departmentId, status, ct));

    [HttpPost]
    [HasPermission(PermissionsCatalog.Employees.Create)]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee(
        [FromBody] SaveEmployeeRequest request,
        [FromServices] IValidator<SaveEmployeeRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateEmployeeAsync(request, ct));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.Employees.Edit)]
    public async Task<ActionResult<EmployeeDto>> UpdateEmployee(
        Guid id,
        [FromBody] SaveEmployeeRequest request,
        [FromServices] IValidator<SaveEmployeeRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateEmployeeAsync(id, request, ct));
    }

    [HttpGet("attendance")]
    [HasPermission(PermissionsCatalog.Employees.View)]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordDto>>> GetAttendance([FromQuery] DateOnly date, [FromQuery] Guid? employeeId, CancellationToken ct)
        => Ok(await _service.GetAttendanceAsync(date, employeeId, ct));

    [HttpPost("attendance")]
    [HasPermission(PermissionsCatalog.Employees.Edit)]
    public async Task<ActionResult<AttendanceRecordDto>> RecordAttendance([FromBody] RecordAttendanceRequest request, CancellationToken ct)
        => Ok(await _service.RecordAttendanceAsync(request, ct));

    [HttpGet("leaves")]
    [HasPermission(PermissionsCatalog.Employees.View)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetLeaves([FromQuery] LeaveStatus? status, CancellationToken ct)
        => Ok(await _service.GetLeavesAsync(status, ct));

    [HttpPost("leaves")]
    [HasPermission(PermissionsCatalog.Employees.Create)]
    public async Task<ActionResult<LeaveRequestDto>> CreateLeave([FromBody] CreateLeaveRequest request, CancellationToken ct)
        => Ok(await _service.CreateLeaveAsync(request, ct));

    [HttpPatch("leaves/{id:guid}/status")]
    [HasPermission(PermissionsCatalog.Employees.Edit)]
    public async Task<ActionResult<LeaveRequestDto>> UpdateLeaveStatus(Guid id, [FromQuery] LeaveStatus status, CancellationToken ct)
        => Ok(await _service.UpdateLeaveStatusAsync(id, status, ct));
}

