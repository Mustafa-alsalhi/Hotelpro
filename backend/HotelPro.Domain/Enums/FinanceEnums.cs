namespace HotelPro.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5
}

public enum InvoiceItemType
{
    RoomCharge = 1,
    ServiceCharge = 2,
    Tax = 3,
    Fee = 4,
    Discount = 5
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Other = 4
}

public enum RevenueSource
{
    Rooms = 1,
    Services = 2,
    Restaurant = 3,
    Other = 4
}

