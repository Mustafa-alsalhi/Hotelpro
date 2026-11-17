namespace HotelPro.Domain.Enums;

public enum ServiceCategory
{
    RoomService = 1,
    Restaurant = 2,
    Laundry = 3,
    Spa = 4,
    Transportation = 5,
    AirportPickup = 6,
    MiniBar = 7,
    ExtraBed = 8,
    Other = 9
}

public enum ServiceOrderStatus
{
    Pending = 1,
    Accepted = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum HousekeepingTaskType
{
    CheckoutCleaning = 1,
    DailyCleaning = 2,
    DeepCleaning = 3,
    Inspection = 4
}

public enum HousekeepingStatus
{
    Pending = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    Inspected = 5
}

public enum TaskPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum MaintenanceStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    WaitingParts = 4,
    Completed = 5,
    Closed = 6
}

