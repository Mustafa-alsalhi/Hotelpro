namespace HotelPro.Application.DTOs.Rates;

public record RateQuoteRequest(
    Guid RoomTypeId,
    Guid RatePlanId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int RoomsCount = 1,
    decimal ManualDiscountAmount = 0
);

public record NightlyRateBreakdownDto(
    DateOnly Date,
    decimal BaseRate,
    decimal AppliedMultiplier,
    decimal NightRate
);

public record RateQuoteDto(
    int Nights,
    decimal AverageNightlyRate,
    decimal SubTotal,
    decimal LongStayDiscount,
    decimal ManualDiscount,
    decimal TaxAmount,
    decimal ServiceChargeAmount,
    decimal GrandTotal,
    IReadOnlyList<NightlyRateBreakdownDto> Breakdown
);

