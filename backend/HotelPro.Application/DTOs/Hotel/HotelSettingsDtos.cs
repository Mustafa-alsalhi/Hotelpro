namespace HotelPro.Application.DTOs.Hotel;

public record HotelSettingsDto(
    Guid HotelId,
    string Name,
    string? LegalName,
    string? LogoUrl,
    string Address,
    string Phone,
    string Email,
    string? TaxNumber,
    string CurrencyCode,
    string CurrencySymbol,
    string TimeZoneId,
    string DateFormat,
    string DefaultLanguage,
    TimeOnly DefaultCheckInTime,
    TimeOnly DefaultCheckOutTime,
    decimal TaxRatePercent,
    decimal ServiceChargePercent,
    bool RequireInspectionAfterCleaning,
    bool AllowCheckoutWithBalancePermission,
    int CancellationPolicyHours,
    decimal CancellationFeePercent
);

public record UpdateHotelSettingsRequest(
    string Name,
    string? LegalName,
    string? LogoUrl,
    string Address,
    string Phone,
    string Email,
    string? TaxNumber,
    string CurrencyCode,
    string CurrencySymbol,
    string TimeZoneId,
    string DateFormat,
    string DefaultLanguage,
    TimeOnly DefaultCheckInTime,
    TimeOnly DefaultCheckOutTime,
    decimal TaxRatePercent,
    decimal ServiceChargePercent,
    bool RequireInspectionAfterCleaning,
    bool AllowCheckoutWithBalancePermission,
    int CancellationPolicyHours,
    decimal CancellationFeePercent
);

