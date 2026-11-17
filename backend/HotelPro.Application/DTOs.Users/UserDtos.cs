namespace HotelPro.Application.DTOs.Users;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    string PreferredLanguage,
    Guid? EmployeeId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Roles
);

public record CreateUserRequest(
    string Email,
    string FullName,
    string Password,
    string? PhoneNumber,
    string PreferredLanguage,
    IReadOnlyList<string> Roles
);

public record UpdateUserRequest(
    string FullName,
    string? PhoneNumber,
    string PreferredLanguage,
    bool IsActive,
    IReadOnlyList<string> Roles
);

