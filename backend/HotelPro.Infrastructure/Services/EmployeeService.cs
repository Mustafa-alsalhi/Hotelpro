using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.HR;
using HotelPro.Domain.Entities.HR;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EmployeeService(HotelProDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default)
        => await _db.Departments.AsNoTracking().OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Code, d.Description))
            .ToListAsync(ct);

    public async Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Departments.AnyAsync(d => d.Code == code, ct))
            throw new ConflictException($"Department code '{code}' already exists.");

        var dept = new Department { Name = request.Name.Trim(), Code = code, Description = request.Description?.Trim() };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync(ct);
        return new DepartmentDto(dept.Id, dept.Name, dept.Code, dept.Description);
    }

    public async Task<PagedResult<EmployeeDto>> GetEmployeesAsync(
        PaginationParams pagination,
        Guid? departmentId,
        EmployeeStatus? status,
        CancellationToken ct = default)
    {
        var query = _db.Employees.Include(e => e.Department).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(e =>
                e.FullName.ToLower().Contains(term) ||
                e.EmployeeCode.ToLower().Contains(term) ||
                e.Phone.Contains(term));
        }
        if (departmentId.HasValue) query = query.Where(e => e.DepartmentId == departmentId.Value);
        if (status.HasValue) query = query.Where(e => e.Status == status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(e => e.FullName)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<EmployeeDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(SaveEmployeeRequest request, CancellationToken ct = default)
    {
        var code = request.EmployeeCode.Trim().ToUpperInvariant();
        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == code, ct))
            throw new ConflictException($"Employee code '{code}' already exists.");

        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, ct)
            ?? throw new NotFoundException("Department not found.");

        var emp = new Employee
        {
            EmployeeCode = code,
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim(),
            JobTitle = request.JobTitle.Trim(),
            DepartmentId = dept.Id,
            HireDate = request.HireDate,
            BaseSalary = request.BaseSalary,
            Status = request.Status,
            EmergencyContactName = request.EmergencyContactName?.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
            UserId = request.UserId,
            CreatedBy = _currentUser.Email
        };

        _db.Employees.Add(emp);
        await _db.SaveChangesAsync(ct);
        emp.Department = dept;
        return Map(emp);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(Guid id, SaveEmployeeRequest request, CancellationToken ct = default)
    {
        var emp = await _db.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Employee not found.");

        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, ct)
            ?? throw new NotFoundException("Department not found.");

        emp.FullName = request.FullName.Trim();
        emp.Phone = request.Phone.Trim();
        emp.Email = request.Email.Trim();
        emp.JobTitle = request.JobTitle.Trim();
        emp.DepartmentId = dept.Id;
        emp.Department = dept;
        emp.HireDate = request.HireDate;
        emp.BaseSalary = request.BaseSalary;
        emp.Status = request.Status;
        emp.EmergencyContactName = request.EmergencyContactName?.Trim();
        emp.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        emp.UserId = request.UserId;
        emp.UpdatedAt = DateTimeOffset.UtcNow;
        emp.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        return Map(emp);
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(DateOnly date, Guid? employeeId, CancellationToken ct = default)
    {
        var query = _db.AttendanceRecords.Include(a => a.Employee).AsNoTracking().Where(a => a.Date == date);
        if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);

        return await query.OrderBy(a => a.Employee.FullName)
            .Select(a => new AttendanceRecordDto(a.Id, a.EmployeeId, a.Employee.FullName, a.Date, a.CheckInTime, a.CheckOutTime, a.Status, a.Notes))
            .ToListAsync(ct);
    }

    public async Task<AttendanceRecordDto> RecordAttendanceAsync(RecordAttendanceRequest request, CancellationToken ct = default)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new NotFoundException("Employee not found.");

        var existing = await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.Date == request.Date, ct);
        if (existing is null)
        {
            existing = new AttendanceRecord
            {
                EmployeeId = request.EmployeeId,
                Date = request.Date,
                CheckInTime = request.CheckInTime,
                CheckOutTime = request.CheckOutTime,
                Status = request.Status,
                Notes = request.Notes?.Trim()
            };
            _db.AttendanceRecords.Add(existing);
        }
        else
        {
            existing.CheckInTime = request.CheckInTime ?? existing.CheckInTime;
            existing.CheckOutTime = request.CheckOutTime ?? existing.CheckOutTime;
            existing.Status = request.Status;
            existing.Notes = request.Notes?.Trim() ?? existing.Notes;
        }

        await _db.SaveChangesAsync(ct);
        return new AttendanceRecordDto(existing.Id, emp.Id, emp.FullName, existing.Date, existing.CheckInTime, existing.CheckOutTime, existing.Status, existing.Notes);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetLeavesAsync(LeaveStatus? status, CancellationToken ct = default)
    {
        var query = _db.LeaveRequests.Include(l => l.Employee).AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(l => l.Status == status.Value);

        return await query.OrderByDescending(l => l.CreatedAt)
            .Select(l => new LeaveRequestDto(l.Id, l.EmployeeId, l.Employee.FullName, l.LeaveType, l.StartDate, l.EndDate, l.Status, l.Reason))
            .ToListAsync(ct);
    }

    public async Task<LeaveRequestDto> CreateLeaveAsync(CreateLeaveRequest request, CancellationToken ct = default)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new NotFoundException("Employee not found.");

        var leave = new LeaveRequest
        {
            EmployeeId = emp.Id,
            LeaveType = request.LeaveType.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason?.Trim(),
            CreatedBy = _currentUser.Email
        };
        _db.LeaveRequests.Add(leave);
        await _db.SaveChangesAsync(ct);
        return new LeaveRequestDto(leave.Id, emp.Id, emp.FullName, leave.LeaveType, leave.StartDate, leave.EndDate, leave.Status, leave.Reason);
    }

    public async Task<LeaveRequestDto> UpdateLeaveStatusAsync(Guid id, LeaveStatus status, CancellationToken ct = default)
    {
        var leave = await _db.LeaveRequests.Include(l => l.Employee).FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("Leave request not found.");

        leave.Status = status;
        leave.ApprovedByUserId = _currentUser.UserId;
        await _db.SaveChangesAsync(ct);
        return new LeaveRequestDto(leave.Id, leave.EmployeeId, leave.Employee.FullName, leave.LeaveType, leave.StartDate, leave.EndDate, leave.Status, leave.Reason);
    }

    private static EmployeeDto Map(Employee e) => new(
        e.Id, e.EmployeeCode, e.FullName, e.Phone, e.Email, e.JobTitle,
        e.DepartmentId, e.Department.Name, e.HireDate, e.BaseSalary, e.Status,
        e.EmergencyContactName, e.EmergencyContactPhone, e.UserId);
}

