namespace HotelPro.Application.DTOs.Roles;

public record PermissionDto(
    Guid Id,
    string Code,
    string Module,
    string Action,
    string Description
);

public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    IReadOnlyList<string> Permissions
);

public record CreateRoleRequest(
    string Name,
    string Description,
    IReadOnlyList<string> PermissionCodes
);

public record UpdateRolePermissionsRequest(
    string Description,
    IReadOnlyList<string> PermissionCodes
);

