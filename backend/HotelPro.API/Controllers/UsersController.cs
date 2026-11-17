using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Users;
using HotelPro.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [HasPermission(PermissionsCatalog.Users.View)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] bool? isActive,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        return Ok(await _userService.GetUsersAsync(pagination, isActive, role, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionsCatalog.Users.View)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await _userService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    [HasPermission(PermissionsCatalog.Users.Create)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        [FromServices] IValidator<CreateUserRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var created = await _userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.Users.Edit)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        [FromServices] IValidator<UpdateUserRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _userService.UpdateAsync(id, request, ct));
    }

    [HttpPatch("{id:guid}/toggle-active")]
    [HasPermission(PermissionsCatalog.Users.ManageUsers)]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        await _userService.ToggleActiveAsync(id, ct);
        return NoContent();
    }
}

