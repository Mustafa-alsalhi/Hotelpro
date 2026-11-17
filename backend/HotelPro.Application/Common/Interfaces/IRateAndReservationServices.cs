using HotelPro.Application.Common;
using HotelPro.Application.DTOs.Rates;
using HotelPro.Application.DTOs.Reservations;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Application.Common.Interfaces;

public interface IRateManagementService
{
    Task<IReadOnlyList<RatePlanDto>> GetRatePlansAsync(bool includeInactive = true, CancellationToken ct = default);
    Task<RatePlanDto> CreateRatePlanAsync(SaveRatePlanRequest request, CancellationToken ct = default);
    Task<RatePlanDto> UpdateRatePlanAsync(Guid id, SaveRatePlanRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<RoomRateDto>> GetRoomRatesAsync(Guid? roomTypeId, Guid? ratePlanId, CancellationToken ct = default);
    Task<RoomRateDto> CreateRoomRateAsync(SaveRoomRateRequest request, CancellationToken ct = default);
    Task DeleteRoomRateAsync(Guid id, CancellationToken ct = default);

    Task<RateQuoteDto> CalculateQuoteAsync(RateQuoteRequest request, CancellationToken ct = default);
}

public interface IReservationService
{
    Task<PagedResult<ReservationDto>> GetReservationsAsync(
        PaginationParams pagination,
        ReservationStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default);

    Task<ReservationDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RoomDto>> GetAvailableRoomsAsync(DateOnly checkIn, DateOnly checkOut, Guid? roomTypeId, CancellationToken ct = default);
    Task<BookingCalendarDto> GetCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<ReservationDto> CreateAsync(CreateReservationRequest request, CancellationToken ct = default);
    Task<ReservationDto> ConfirmAsync(Guid id, CancellationToken ct = default);
    Task<ReservationDto> UpdateStayAsync(Guid id, UpdateReservationStayRequest request, CancellationToken ct = default);
    Task<ReservationDto> ChangeRoomAsync(Guid id, ChangeReservationRoomRequest request, CancellationToken ct = default);
    Task<ReservationDto> CancelAsync(Guid id, CancelReservationRequest request, CancellationToken ct = default);
}

