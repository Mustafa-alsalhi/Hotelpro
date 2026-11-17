using HotelPro.Domain.Common;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Guests;

public class Guest : SoftDeletableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Nationality { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsVip { get; set; }

    public ICollection<GuestDocument> Documents { get; set; } = new List<GuestDocument>();
}

public class GuestDocument : BaseEntity
{
    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string? IssuingCountry { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

