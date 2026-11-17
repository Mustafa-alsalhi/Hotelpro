using HotelPro.Application.Common;
using HotelPro.Application.DTOs.HR;
using HotelPro.Application.DTOs.Inventory;
using HotelPro.Domain.Enums;

namespace HotelPro.Application.Common.Interfaces;

public interface IEmployeeService
{
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default);
    Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken ct = default);

    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(PaginationParams pagination, Guid? departmentId, EmployeeStatus? status, CancellationToken ct = default);
    Task<EmployeeDto> CreateEmployeeAsync(SaveEmployeeRequest request, CancellationToken ct = default);
    Task<EmployeeDto> UpdateEmployeeAsync(Guid id, SaveEmployeeRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(DateOnly date, Guid? employeeId, CancellationToken ct = default);
    Task<AttendanceRecordDto> RecordAttendanceAsync(RecordAttendanceRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetLeavesAsync(LeaveStatus? status, CancellationToken ct = default);
    Task<LeaveRequestDto> CreateLeaveAsync(CreateLeaveRequest request, CancellationToken ct = default);
    Task<LeaveRequestDto> UpdateLeaveStatusAsync(Guid id, LeaveStatus status, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<InventoryCategoryDto> CreateCategoryAsync(string name, CancellationToken ct = default);

    Task<PagedResult<InventoryItemDto>> GetItemsAsync(PaginationParams pagination, Guid? categoryId, bool? lowStockOnly, CancellationToken ct = default);
    Task<InventoryItemDto> CreateItemAsync(SaveInventoryItemRequest request, CancellationToken ct = default);
    Task<InventoryItemDto> UpdateItemAsync(Guid id, SaveInventoryItemRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken ct = default);
    Task<SupplierDto> CreateSupplierAsync(SaveSupplierRequest request, CancellationToken ct = default);

    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(PaginationParams pagination, PurchaseOrderStatus? status, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default);
    Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(PaginationParams pagination, Guid? itemId, CancellationToken ct = default);
    Task<StockTransactionDto> RecordTransactionAsync(CreateStockTransactionRequest request, CancellationToken ct = default);
}

