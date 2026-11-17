using HotelPro.Application.Common;
using HotelPro.Application.DTOs.Guests;
using HotelPro.Application.DTOs.Hotel;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Application.Common.Interfaces;

public interface IHotelSettingsService
{
    Task<HotelSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<HotelSettingsDto> UpdateSettingsAsync(UpdateHotelSettingsRequest request, CancellationToken ct = default);
}

public interface IRoomTypeService
{
    Task<IReadOnlyList<RoomTypeDto>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default);
    Task<RoomTypeDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoomTypeDto> CreateAsync(SaveRoomTypeRequest request, CancellationToken ct = default);
    Task<RoomTypeDto> UpdateAsync(Guid id, SaveRoomTypeRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IAmenityService
{
    Task<IReadOnlyList<AmenityDto>> GetAllAsync(CancellationToken ct = default);
    Task<AmenityDto> CreateAsync(CreateAmenityRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IRoomService
{
    Task<PagedResult<RoomDto>> GetRoomsAsync(
        PaginationParams pagination,
        int? floor,
        Guid? roomTypeId,
        RoomStatus? status,
        CancellationToken ct = default);

    Task<RoomDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoomDto> CreateAsync(SaveRoomRequest request, CancellationToken ct = default);
    Task<RoomDto> UpdateAsync(Guid id, SaveRoomRequest request, CancellationToken ct = default);
    Task<RoomDto> ChangeStatusAsync(Guid id, ChangeRoomStatusRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IGuestService
{
    Task<PagedResult<GuestDto>> GetGuestsAsync(PaginationParams pagination, bool? isVip, CancellationToken ct = default);
    Task<GuestDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GuestDto> CreateAsync(SaveGuestRequest request, CancellationToken ct = default);
    Task<GuestDto> UpdateAsync(Guid id, SaveGuestRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

