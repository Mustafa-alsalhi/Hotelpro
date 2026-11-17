using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Guests;

public record GuestDocumentDto(
    Guid Id,
    DocumentType DocumentType,
    string DocumentNumber,
    string? IssuingCountry,
    DateOnly? ExpiryDate
);

public record SaveGuestDocumentRequest(
    DocumentType DocumentType,
    string DocumentNumber,
    string? IssuingCountry,
    DateOnly? ExpiryDate
);

public record GuestDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    string? Nationality,
    DateOnly? DateOfBirth,
    string? Address,
    string? Notes,
    bool IsVip,
    DateTimeOffset CreatedAt,
    IReadOnlyList<GuestDocumentDto> Documents
);

public record SaveGuestRequest(
    string FullName,
    string Phone,
    string? Email,
    string? Nationality,
    DateOnly? DateOfBirth,
    string? Address,
    string? Notes,
    bool IsVip,
    IReadOnlyList<SaveGuestDocumentRequest> Documents
);

