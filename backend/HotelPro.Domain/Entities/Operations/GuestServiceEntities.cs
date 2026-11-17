using HotelPro.Domain.Common;
using HotelPro.Domain.Entities.Guests;
using HotelPro.Domain.Entities.Reservations;
using HotelPro.Domain.Entities.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Operations;

public class GuestService : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceCategory Category { get; set; }
    public decimal Price { get; set; }
    public bool Taxable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class ServiceOrder : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public ServiceOrderStatus Status { get; set; } = ServiceOrderStatus.Pending;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? AssignedUserId { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<ServiceOrderItem> Items { get; set; } = new List<ServiceOrderItem>();
}

public class ServiceOrderItem : BaseEntity
{
    public Guid ServiceOrderId { get; set; }
    public ServiceOrder ServiceOrder { get; set; } = null!;

    public Guid GuestServiceId { get; set; }
    public GuestService GuestService { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

