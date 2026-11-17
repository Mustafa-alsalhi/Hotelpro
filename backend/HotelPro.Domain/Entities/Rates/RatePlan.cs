using HotelPro.Domain.Common;

namespace HotelPro.Domain.Entities.Rates;

public class RatePlan : SoftDeletableEntity
{
    public Guid HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRefundable { get; set; } = true;
    public decimal WeekendMultiplier { get; set; } = 1.00m;
    public int LongStayMinNights { get; set; } = 7;
    public decimal LongStayDiscountPercent { get; set; } = 0.00m;
    public string? CorporateCode { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RoomRate> RoomRates { get; set; } = new List<RoomRate>();
}

