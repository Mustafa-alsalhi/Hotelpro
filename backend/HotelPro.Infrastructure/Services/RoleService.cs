using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Roles;
using HotelPro.Domain.Entities.Identity;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly HotelProDbContext _db;
    private readonly IAuditService _audit;

    public RoleService(RoleManager<ApplicationRole> roleManager, HotelProDbContext db, IAuditService audit)
    {
        _roleManager = roleManager;
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        return await _db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Name!,
                r.Description,
                r.IsSystemRole,
                r.RolePermissions.Select(rp => rp.Permission.Code).OrderBy(c => c).ToList()
            ))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        return await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Action, p.Description))
            .ToListAsync(ct);
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        if (await _roleManager.RoleExistsAsync(request.Name.Trim()))
            throw new ConflictException($"Role '{request.Name}' already exists.");

        var role = new ApplicationRole
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            IsSystemRole = false
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new BusinessRuleException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await SyncRolePermissionsAsync(role.Id, request.PermissionCodes, ct);
        await _audit.LogAsync("CreateRole", "ApplicationRole", role.Id.ToString(), newValues: new { role.Name, request.PermissionCodes }, ct: ct);

        return (await GetRolesAsync(ct)).First(r => r.Id == role.Id);
    }

    public async Task<RoleDto> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new NotFoundException("Role not found.");

        role.Description = request.Description.Trim();
        await _roleManager.UpdateAsync(role);

        await SyncRolePermissionsAsync(role.Id, request.PermissionCodes, ct);
        await _audit.LogAsync("UpdateRolePermissions", "ApplicationRole", role.Id.ToString(), newValues: new { role.Name, request.PermissionCodes }, ct: ct);

        return (await GetRolesAsync(ct)).First(r => r.Id == role.Id);
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new NotFoundException("Role not found.");

        if (role.IsSystemRole)
            throw new BusinessRuleException("System roles cannot be deleted.");

        var hasUsers = await _db.UserRoles.AnyAsync(ur => ur.RoleId == roleId, ct);
        if (hasUsers)
            throw new BusinessRuleException("Cannot delete a role that is assigned to users.");

        await _roleManager.DeleteAsync(role);
        await _audit.LogAsync("DeleteRole", "ApplicationRole", roleId.ToString(), ct: ct);
    }

    private async Task SyncRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken ct)
    {
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        _db.RolePermissions.RemoveRange(existing);

        var permissions = await _db.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .ToListAsync(ct);

        foreach (var permission in permissions)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}

