using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Rooms;

public record AmenityDto(Guid Id, string Name, string Icon, string Category);

public record CreateAmenityRequest(string Name, string Icon, string Category);

public record RoomTypeDto(
    Guid Id,
    string Name,
    string Code,
    string Description,
    decimal BasePrice,
    int MaxAdults,
    int MaxChildren,
    int BedCount,
    decimal AreaSqm,
    bool IsActive,
    IReadOnlyList<AmenityDto> Amenities
);

public record SaveRoomTypeRequest(
    string Name,
    string Code,
    string Description,
    decimal BasePrice,
    int MaxAdults,
    int MaxChildren,
    int BedCount,
    decimal AreaSqm,
    bool IsActive,
    IReadOnlyList<Guid> AmenityIds
);

public record RoomDto(
    Guid Id,
    Guid RoomTypeId,
    string RoomTypeName,
    string RoomNumber,
    int Floor,
    int MaxCapacity,
    int BedCount,
    decimal EffectivePrice,
    decimal? BasePriceOverride,
    RoomStatus Status,
    string Description,
    string? Notes,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<AmenityDto> Amenities
);

public record SaveRoomRequest(
    Guid RoomTypeId,
    string RoomNumber,
    int Floor,
    int MaxCapacity,
    int BedCount,
    decimal? BasePriceOverride,
    string Description,
    string? Notes,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<Guid> AmenityIds
);

public record ChangeRoomStatusRequest(RoomStatus Status, string? Reason);

