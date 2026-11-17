using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Auth;
using HotelPro.Domain.Entities.Identity;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using HotelPro.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HotelProDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditService _audit;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        HotelProDbContext db,
        IJwtTokenService jwt,
        IAuditService audit)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.Trim().ToUpperInvariant(), ct);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedDomainException("Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("Your account is deactivated. Please contact the administrator.");

        var (roles, permissions) = await GetUserRolesAndPermissionsAsync(user, ct);
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user, roles, permissions);
        var (rawRefresh, refreshHash, refreshExpires) = _jwt.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpires,
            CreatedByIp = ipAddress
        });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Login", "ApplicationUser", user.Id.ToString(), ct: ct);

        return new AuthResponse(
            accessToken,
            rawRefresh,
            expiresAt,
            new CurrentUserDto(user.Id, user.Email!, user.FullName, user.PreferredLanguage, roles, permissions)
        );
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var tokenHash = _jwt.HashToken(request.RefreshToken);
        var existingToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (existingToken is null)
            throw new UnauthorizedDomainException("Invalid refresh token.");

        if (existingToken.IsRevoked)
        {
            // Reuse detection: revoke all active tokens for this user
            var activeTokens = await _db.RefreshTokens
                .Where(rt => rt.UserId == existingToken.UserId && rt.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var t in activeTokens)
            {
                t.RevokedAt = DateTimeOffset.UtcNow;
                t.RevokedByIp = ipAddress;
                t.ReasonRevoked = "Attempted reuse of revoked refresh token.";
            }
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedDomainException("Refresh token has been revoked.");
        }

        if (existingToken.IsExpired)
            throw new UnauthorizedDomainException("Refresh token has expired.");

        if (!existingToken.User.IsActive)
            throw new ForbiddenException("Account is deactivated.");

        var (rawNewRefresh, newRefreshHash, newRefreshExpires) = _jwt.GenerateRefreshToken();
        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        existingToken.ReplacedByTokenHash = newRefreshHash;
        existingToken.ReasonRevoked = "Rotated";

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newRefreshHash,
            ExpiresAt = newRefreshExpires,
            CreatedByIp = ipAddress
        });

        var (roles, permissions) = await GetUserRolesAndPermissionsAsync(existingToken.User, ct);
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(existingToken.User, roles, permissions);

        await _db.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            rawNewRefresh,
            expiresAt,
            new CurrentUserDto(
                existingToken.User.Id,
                existingToken.User.Email!,
                existingToken.User.FullName,
                existingToken.User.PreferredLanguage,
                roles,
                permissions)
        );
    }

    public async Task LogoutAsync(string? refreshToken, string? ipAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = _jwt.HashToken(refreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);
        if (token is not null && !token.IsRevoked)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = "User logout";
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        var (roles, permissions) = await GetUserRolesAndPermissionsAsync(user, ct);
        return new CurrentUserDto(user.Id, user.Email!, user.FullName, user.PreferredLanguage, roles, permissions);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new BusinessRuleException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _audit.LogAsync("ChangePassword", "ApplicationUser", user.Id.ToString(), ct: ct);
    }

    private async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> GetUserRolesAndPermissionsAsync(
        ApplicationUser user,
        CancellationToken ct)
    {
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = await _db.RolePermissions
            .Where(rp => roles.Contains(rp.Role.Name!))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

        return (roles, permissions);
    }
}

