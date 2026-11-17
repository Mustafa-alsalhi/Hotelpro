using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Rooms;

namespace HotelPro.Domain.Entities.Rates;

public class RoomRate : AuditableEntity
{
    public Guid RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public Guid RatePlanId { get; set; }
    public RatePlan RatePlan { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal PricePerNight { get; set; }
    public int Priority { get; set; } = 1;
}

