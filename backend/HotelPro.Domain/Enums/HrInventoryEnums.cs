namespace HotelPro.Domain.Enums;

public enum EmployeeStatus
{
    Active = 1,
    OnLeave = 2,
    Suspended = 3,
    Terminated = 4
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    HalfDay = 4,
    OnLeave = 5
}

public enum LeaveStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Ordered = 2,
    Received = 3,
    Cancelled = 4
}

public enum StockTransactionType
{
    PurchaseIn = 1,
    DepartmentIssue = 2,
    AdjustmentIn = 3,
    AdjustmentOut = 4
}

