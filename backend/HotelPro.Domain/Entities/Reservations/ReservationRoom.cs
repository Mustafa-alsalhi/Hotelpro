using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Rooms;

namespace HotelPro.Domain.Entities.Reservations;

public class ReservationRoom : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public decimal AgreedNightlyRate { get; set; }
    public int Nights { get; set; }
    public decimal TotalRoomAmount { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
}

