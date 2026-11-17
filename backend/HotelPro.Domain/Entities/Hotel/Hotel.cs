using HotelPro.Domain.Common;

namespace HotelPro.Domain.Entities.Hotel;

public class Hotel : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? LogoUrl { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }

    public HotelSettings? Settings { get; set; }
}

public class HotelSettings : AuditableEntity
{
    public Guid HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;

    public string CurrencyCode { get; set; } = "SAR";
    public string CurrencySymbol { get; set; } = "ر.س";
    public string TimeZoneId { get; set; } = "Arab Standard Time";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string DefaultLanguage { get; set; } = "ar";
    public TimeOnly DefaultCheckInTime { get; set; } = new(14, 0);
    public TimeOnly DefaultCheckOutTime { get; set; } = new(12, 0);
    public decimal TaxRatePercent { get; set; } = 15.00m;
    public decimal ServiceChargePercent { get; set; } = 0.00m;
    public bool RequireInspectionAfterCleaning { get; set; } = true;
    public bool AllowCheckoutWithBalancePermission { get; set; } = false;
    public int CancellationPolicyHours { get; set; } = 24;
    public decimal CancellationFeePercent { get; set; } = 0.00m;
}

