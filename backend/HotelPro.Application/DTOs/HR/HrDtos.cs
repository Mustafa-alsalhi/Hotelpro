using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.HR;

public record DepartmentDto(Guid Id, string Name, string Code, string? Description);

public record SaveDepartmentRequest(string Name, string Code, string? Description);

public record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Phone,
    string Email,
    string JobTitle,
    Guid DepartmentId,
    string DepartmentName,
    DateOnly HireDate,
    decimal BaseSalary,
    EmployeeStatus Status,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    Guid? UserId
);

public record SaveEmployeeRequest(
    string EmployeeCode,
    string FullName,
    string Phone,
    string Email,
    string JobTitle,
    Guid DepartmentId,
    DateOnly HireDate,
    decimal BaseSalary,
    EmployeeStatus Status,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    Guid? UserId
);

public record AttendanceRecordDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    TimeOnly? CheckInTime,
    TimeOnly? CheckOutTime,
    AttendanceStatus Status,
    string? Notes
);

public record RecordAttendanceRequest(
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly? CheckInTime,
    TimeOnly? CheckOutTime,
    AttendanceStatus Status,
    string? Notes
);

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    LeaveStatus Status,
    string? Reason
);

public record CreateLeaveRequest(
    Guid EmployeeId,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason
);

