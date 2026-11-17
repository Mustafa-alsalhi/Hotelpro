using HotelPro.Domain.Common;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Finance;

public class RevenueRecord : BaseEntity
{
    public RevenueSource Source { get; set; }
    public Guid? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly RecognizedDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public Guid RecordedByUserId { get; set; }
}

