using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Roles;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [HasPermission(PermissionsCatalog.Roles.View)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _roleService.GetRolesAsync(ct));
    }

    [HttpGet("permissions")]
    [HasPermission(PermissionsCatalog.Roles.View)]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetAllPermissions(CancellationToken ct)
    {
        return Ok(await _roleService.GetAllPermissionsAsync(ct));
    }

    [HttpPost]
    [HasPermission(PermissionsCatalog.Roles.Create)]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] CreateRoleRequest request,
        [FromServices] IValidator<CreateRoleRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _roleService.CreateRoleAsync(request, ct));
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(PermissionsCatalog.Roles.Edit)]
    public async Task<ActionResult<RoleDto>> UpdatePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken ct)
    {
        return Ok(await _roleService.UpdateRolePermissionsAsync(id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionsCatalog.Roles.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _roleService.DeleteRoleAsync(id, ct);
        return NoContent();
    }
}

