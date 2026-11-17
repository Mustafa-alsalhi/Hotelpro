using HotelPro.Domain.Common;

namespace HotelPro.Domain.Entities.Rooms;

public class Amenity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "check_circle";
    public string Category { get; set; } = "General";

    public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
}

public class RoomType : SoftDeletableEntity
{
    public Guid HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int MaxAdults { get; set; } = 2;
    public int MaxChildren { get; set; } = 1;
    public int BedCount { get; set; } = 1;
    public decimal AreaSqm { get; set; } = 25;
    public bool IsActive { get; set; } = true;

    public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}

public class RoomTypeAmenity
{
    public Guid RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public Guid AmenityId { get; set; }
    public Amenity Amenity { get; set; } = null!;
}

