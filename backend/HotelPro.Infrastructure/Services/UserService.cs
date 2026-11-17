using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Users;
using HotelPro.Domain.Entities.Identity;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly HotelProDbContext _db;
    private readonly IAuditService _audit;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        HotelProDbContext db,
        IAuditService audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(
        PaginationParams pagination,
        bool? isActive,
        string? role,
        CancellationToken ct = default)
    {
        var query = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleEntity = await _roleManager.FindByNameAsync(role);
            if (roleEntity is null)
                return new PagedResult<UserDto> { PageNumber = pagination.PageNumber, PageSize = pagination.PageSize };

            var userIdsInRole = _db.UserRoles
                .Where(ur => ur.RoleId == roleEntity.Id)
                .Select(ur => ur.UserId);
            query = query.Where(u => userIdsInRole.Contains(u.Id));
        }

        query = pagination.SortBy?.ToLower() switch
        {
            "fullname" => pagination.SortDesc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            "email" => pagination.SortDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            _ => pagination.SortDesc ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt)
        };

        var totalCount = await query.CountAsync(ct);
        var users = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();
        var userRolesLookup = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, r.Name }
        ).ToListAsync(ct);

        var items = users.Select(u => new UserDto(
            u.Id,
            u.Email!,
            u.FullName,
            u.PhoneNumber,
            u.IsActive,
            u.PreferredLanguage,
            u.EmployeeId,
            u.CreatedAt,
            userRolesLookup.Where(x => x.UserId == u.Id).Select(x => x.Name!).ToList()
        )).ToList();

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException("User not found.");

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        return new UserDto(
            user.Id,
            user.Email!,
            user.FullName,
            user.PhoneNumber,
            user.IsActive,
            user.PreferredLanguage,
            user.EmployeeId,
            user.CreatedAt,
            roles);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new ConflictException($"A user with email '{request.Email}' already exists.");

        foreach (var role in request.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                throw new BusinessRuleException($"Role '{role}' does not exist.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            PreferredLanguage = request.PreferredLanguage,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new BusinessRuleException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
        if (!roleResult.Succeeded)
            throw new BusinessRuleException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));

        await _audit.LogAsync("CreateUser", "ApplicationUser", user.Id.ToString(), newValues: new { user.Email, user.FullName, request.Roles }, ct: ct);

        return await GetByIdAsync(user.Id, ct);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException("User not found.");

        foreach (var role in request.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                throw new BusinessRuleException($"Role '{role}' does not exist.");
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.PreferredLanguage = request.PreferredLanguage;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BusinessRuleException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

        var currentRoles = await _userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(request.Roles).ToList();
        var toAdd = request.Roles.Except(currentRoles).ToList();

        if (toRemove.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Count > 0)
            await _userManager.AddToRolesAsync(user, toAdd);

        await _audit.LogAsync("UpdateUser", "ApplicationUser", user.Id.ToString(), newValues: new { user.FullName, user.IsActive, request.Roles }, ct: ct);

        return await GetByIdAsync(user.Id, ct);
    }

    public async Task ToggleActiveAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException("User not found.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        await _audit.LogAsync("ToggleUserActive", "ApplicationUser", user.Id.ToString(), newValues: new { user.IsActive }, ct: ct);
    }
}

