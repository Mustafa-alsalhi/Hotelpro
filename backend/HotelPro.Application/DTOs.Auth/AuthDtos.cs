namespace HotelPro.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    CurrentUserDto User
);

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    string PreferredLanguage,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);

