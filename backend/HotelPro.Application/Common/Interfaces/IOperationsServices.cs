using HotelPro.Application.Common;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Domain.Enums;

namespace HotelPro.Application.Common.Interfaces;

public interface IFrontDeskService
{
    Task<IReadOnlyList<CheckInRecordDto>> CheckInAsync(CheckInRequest request, CancellationToken ct = default);
    Task<CheckOutSummaryDto> GetCheckOutSummaryAsync(Guid reservationId, CancellationToken ct = default);
    Task<IReadOnlyList<CheckOutRecordDto>> CheckOutAsync(CheckOutRequest request, CancellationToken ct = default);
}

public interface IGuestServicesAppService
{
    Task<IReadOnlyList<GuestServiceDto>> GetCatalogAsync(bool includeInactive = true, CancellationToken ct = default);
    Task<GuestServiceDto> CreateServiceAsync(SaveGuestServiceRequest request, CancellationToken ct = default);
    Task<GuestServiceDto> UpdateServiceAsync(Guid id, SaveGuestServiceRequest request, CancellationToken ct = default);

    Task<PagedResult<ServiceOrderDto>> GetOrdersAsync(PaginationParams pagination, ServiceOrderStatus? status, Guid? reservationId, CancellationToken ct = default);
    Task<ServiceOrderDto> CreateOrderAsync(CreateServiceOrderRequest request, CancellationToken ct = default);
    Task<ServiceOrderDto> UpdateOrderStatusAsync(Guid id, UpdateServiceOrderStatusRequest request, CancellationToken ct = default);
}

public interface IHousekeepingService
{
    Task<PagedResult<HousekeepingTaskDto>> GetTasksAsync(PaginationParams pagination, HousekeepingStatus? status, TaskPriority? priority, CancellationToken ct = default);
    Task<HousekeepingTaskDto> CreateTaskAsync(CreateHousekeepingTaskRequest request, CancellationToken ct = default);
    Task<HousekeepingTaskDto> UpdateTaskAsync(Guid id, UpdateHousekeepingTaskRequest request, CancellationToken ct = default);
}

public interface IMaintenanceService
{
    Task<PagedResult<MaintenanceRequestDto>> GetRequestsAsync(PaginationParams pagination, MaintenanceStatus? status, TaskPriority? priority, CancellationToken ct = default);
    Task<MaintenanceRequestDto> CreateRequestAsync(CreateMaintenanceRequest request, CancellationToken ct = default);
    Task<MaintenanceRequestDto> UpdateRequestAsync(Guid id, UpdateMaintenanceRequest request, CancellationToken ct = default);
}

