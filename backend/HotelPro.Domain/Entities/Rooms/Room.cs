using System.ComponentModel.DataAnnotations;
using HotelPro.Domain.Common;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Rooms;

public class Room : SoftDeletableEntity
{
    public Guid HotelId { get; set; }
    public Guid RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int MaxCapacity { get; set; }
    public int BedCount { get; set; }
    public decimal? BasePriceOverride { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string ImageUrlsJson { get; set; } = "[]";

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
}

public class RoomAmenity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid AmenityId { get; set; }
    public Amenity Amenity { get; set; } = null!;
}

