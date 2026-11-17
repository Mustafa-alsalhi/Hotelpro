namespace HotelPro.Application.DTOs.Rates;

public record RatePlanDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsRefundable,
    decimal WeekendMultiplier,
    int LongStayMinNights,
    decimal LongStayDiscountPercent,
    string? CorporateCode,
    bool IsActive
);

public record SaveRatePlanRequest(
    string Name,
    string Code,
    string? Description,
    bool IsRefundable,
    decimal WeekendMultiplier,
    int LongStayMinNights,
    decimal LongStayDiscountPercent,
    string? CorporateCode,
    bool IsActive
);

public record RoomRateDto(
    Guid Id,
    Guid RoomTypeId,
    string RoomTypeName,
    Guid RatePlanId,
    string RatePlanName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PricePerNight,
    int Priority
);

public record SaveRoomRateRequest(
    Guid RoomTypeId,
    Guid RatePlanId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PricePerNight,
    int Priority
);

