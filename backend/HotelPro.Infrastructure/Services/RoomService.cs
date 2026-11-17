using System.Text.Json;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Entities.Rooms;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class RoomService : IRoomService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public RoomService(HotelProDbContext db, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<RoomDto>> GetRoomsAsync(
        PaginationParams pagination,
        int? floor,
        Guid? roomTypeId,
        RoomStatus? status,
        CancellationToken ct = default)
    {
        var query = _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(r =>
                r.RoomNumber.ToLower().Contains(term) ||
                r.RoomType.Name.ToLower().Contains(term));
        }

        if (floor.HasValue)
            query = query.Where(r => r.Floor == floor.Value);

        if (roomTypeId.HasValue)
            query = query.Where(r => r.RoomTypeId == roomTypeId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        query = pagination.SortBy?.ToLower() switch
        {
            "floor" => pagination.SortDesc ? query.OrderByDescending(r => r.Floor) : query.OrderBy(r => r.Floor),
            "status" => pagination.SortDesc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            _ => pagination.SortDesc ? query.OrderByDescending(r => r.RoomNumber) : query.OrderBy(r => r.RoomNumber)
        };

        var totalCount = await query.CountAsync(ct);
        var rooms = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<RoomDto>
        {
            Items = rooms.Select(Map).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<RoomDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Room not found.");

        return Map(room);
    }

    public async Task<RoomDto> CreateAsync(SaveRoomRequest request, CancellationToken ct = default)
    {
        var hotelId = await _db.Hotels.Select(h => h.Id).FirstOrDefaultAsync(ct);
        if (hotelId == Guid.Empty)
            throw new BusinessRuleException("Hotel must be initialized first.");

        if (!await _db.RoomTypes.AnyAsync(rt => rt.Id == request.RoomTypeId, ct))
            throw new NotFoundException("Room type not found.");

        var number = request.RoomNumber.Trim();
        if (await _db.Rooms.AnyAsync(r => r.HotelId == hotelId && r.RoomNumber == number, ct))
            throw new ConflictException($"Room '{number}' already exists.");

        var room = new Room
        {
            HotelId = hotelId,
            RoomTypeId = request.RoomTypeId,
            RoomNumber = number,
            Floor = request.Floor,
            MaxCapacity = request.MaxCapacity,
            BedCount = request.BedCount,
            BasePriceOverride = request.BasePriceOverride,
            Status = RoomStatus.Available,
            Description = request.Description.Trim(),
            Notes = request.Notes?.Trim(),
            ImageUrlsJson = JsonSerializer.Serialize(request.ImageUrls ?? []),
            CreatedBy = _currentUser.Email
        };

        foreach (var amenityId in request.AmenityIds.Distinct())
            room.RoomAmenities.Add(new RoomAmenity { RoomId = room.Id, AmenityId = amenityId });

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CreateRoom", "Room", room.Id.ToString(), newValues: new { room.RoomNumber, room.Floor }, ct: ct);

        return await GetByIdAsync(room.Id, ct);
    }

    public async Task<RoomDto> UpdateAsync(Guid id, SaveRoomRequest request, CancellationToken ct = default)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomAmenities)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Room not found.");

        var number = request.RoomNumber.Trim();
        if (await _db.Rooms.AnyAsync(r => r.Id != id && r.HotelId == room.HotelId && r.RoomNumber == number, ct))
            throw new ConflictException($"Room '{number}' already exists.");

        room.RoomTypeId = request.RoomTypeId;
        room.RoomNumber = number;
        room.Floor = request.Floor;
        room.MaxCapacity = request.MaxCapacity;
        room.BedCount = request.BedCount;
        room.BasePriceOverride = request.BasePriceOverride;
        room.Description = request.Description.Trim();
        room.Notes = request.Notes?.Trim();
        room.ImageUrlsJson = JsonSerializer.Serialize(request.ImageUrls ?? []);
        room.UpdatedAt = DateTimeOffset.UtcNow;
        room.UpdatedBy = _currentUser.Email;

        _db.RoomAmenities.RemoveRange(room.RoomAmenities);
        foreach (var amenityId in request.AmenityIds.Distinct())
            _db.RoomAmenities.Add(new RoomAmenity { RoomId = room.Id, AmenityId = amenityId });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UpdateRoom", "Room", room.Id.ToString(), newValues: new { room.RoomNumber, room.Floor }, ct: ct);

        return await GetByIdAsync(room.Id, ct);
    }

    public async Task<RoomDto> ChangeStatusAsync(Guid id, ChangeRoomStatusRequest request, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Room not found.");

        // Enforce stay/reservation lifecycle rules: cannot manually override Occupied or Reserved
        if (room.Status == RoomStatus.Occupied)
            throw new BusinessRuleException("Occupied rooms can only change status through Check-Out.");

        if (request.Status is RoomStatus.Occupied or RoomStatus.Reserved)
            throw new BusinessRuleException("Rooms can only become Reserved or Occupied via Reservation / Check-In workflows.");

        var oldStatus = room.Status;
        room.Status = request.Status;
        room.UpdatedAt = DateTimeOffset.UtcNow;
        room.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ChangeRoomStatus", "Room", room.Id.ToString(),
            oldValues: new { Status = oldStatus.ToString() },
            newValues: new { Status = request.Status.ToString(), request.Reason }, ct: ct);

        return await GetByIdAsync(room.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Room not found.");

        if (room.Status is RoomStatus.Occupied or RoomStatus.Reserved)
            throw new BusinessRuleException("Cannot delete a room that is currently Occupied or Reserved.");

        room.IsDeleted = true;
        room.DeletedAt = DateTimeOffset.UtcNow;
        room.DeletedBy = _currentUser.Email;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("DeleteRoom", "Room", id.ToString(), ct: ct);
    }

    private static RoomDto Map(Room r)
    {
        IReadOnlyList<string> images;
        try { images = JsonSerializer.Deserialize<List<string>>(r.ImageUrlsJson) ?? []; }
        catch { images = []; }

        return new RoomDto(
            r.Id,
            r.RoomTypeId,
            r.RoomType.Name,
            r.RoomNumber,
            r.Floor,
            r.MaxCapacity,
            r.BedCount,
            r.BasePriceOverride ?? r.RoomType.BasePrice,
            r.BasePriceOverride,
            r.Status,
            r.Description,
            r.Notes,
            images,
            r.RoomAmenities.Select(ra => new AmenityDto(ra.Amenity.Id, ra.Amenity.Name, ra.Amenity.Icon, ra.Amenity.Category)).ToList()
        );
    }
}

