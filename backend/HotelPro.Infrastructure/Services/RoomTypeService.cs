using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Entities.Rooms;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class RoomTypeService : IRoomTypeService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public RoomTypeService(HotelProDbContext db, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<RoomTypeDto>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default)
    {
        var query = _db.RoomTypes
            .Include(rt => rt.RoomTypeAmenities)
            .ThenInclude(rta => rta.Amenity)
            .AsNoTracking()
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(rt => rt.IsActive);

        var list = await query.OrderBy(rt => rt.BasePrice).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<RoomTypeDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var rt = await _db.RoomTypes
            .Include(x => x.RoomTypeAmenities)
            .ThenInclude(x => x.Amenity)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Room type not found.");

        return Map(rt);
    }

    public async Task<RoomTypeDto> CreateAsync(SaveRoomTypeRequest request, CancellationToken ct = default)
    {
        var hotelId = await _db.Hotels.Select(h => h.Id).FirstOrDefaultAsync(ct);
        if (hotelId == Guid.Empty)
            throw new BusinessRuleException("Hotel must be initialized first.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.RoomTypes.AnyAsync(rt => rt.HotelId == hotelId && rt.Code == code, ct))
            throw new ConflictException($"Room type code '{code}' already exists.");

        var rt = new RoomType
        {
            HotelId = hotelId,
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description.Trim(),
            BasePrice = request.BasePrice,
            MaxAdults = request.MaxAdults,
            MaxChildren = request.MaxChildren,
            BedCount = request.BedCount,
            AreaSqm = request.AreaSqm,
            IsActive = request.IsActive,
            CreatedBy = _currentUser.Email
        };

        foreach (var amenityId in request.AmenityIds.Distinct())
            rt.RoomTypeAmenities.Add(new RoomTypeAmenity { RoomTypeId = rt.Id, AmenityId = amenityId });

        _db.RoomTypes.Add(rt);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CreateRoomType", "RoomType", rt.Id.ToString(), newValues: new { rt.Name, rt.Code, rt.BasePrice }, ct: ct);

        return await GetByIdAsync(rt.Id, ct);
    }

    public async Task<RoomTypeDto> UpdateAsync(Guid id, SaveRoomTypeRequest request, CancellationToken ct = default)
    {
        var rt = await _db.RoomTypes
            .Include(x => x.RoomTypeAmenities)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Room type not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.RoomTypes.AnyAsync(x => x.Id != id && x.HotelId == rt.HotelId && x.Code == code, ct))
            throw new ConflictException($"Room type code '{code}' already exists.");

        rt.Name = request.Name.Trim();
        rt.Code = code;
        rt.Description = request.Description.Trim();
        rt.BasePrice = request.BasePrice;
        rt.MaxAdults = request.MaxAdults;
        rt.MaxChildren = request.MaxChildren;
        rt.BedCount = request.BedCount;
        rt.AreaSqm = request.AreaSqm;
        rt.IsActive = request.IsActive;
        rt.UpdatedAt = DateTimeOffset.UtcNow;
        rt.UpdatedBy = _currentUser.Email;

        _db.RoomTypeAmenities.RemoveRange(rt.RoomTypeAmenities);
        foreach (var amenityId in request.AmenityIds.Distinct())
            _db.RoomTypeAmenities.Add(new RoomTypeAmenity { RoomTypeId = rt.Id, AmenityId = amenityId });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UpdateRoomType", "RoomType", rt.Id.ToString(), newValues: new { rt.Name, rt.BasePrice }, ct: ct);

        return await GetByIdAsync(rt.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rt = await _db.RoomTypes.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Room type not found.");

        if (await _db.Rooms.AnyAsync(r => r.RoomTypeId == id, ct))
            throw new BusinessRuleException("Cannot delete a room type that has assigned rooms.");

        rt.IsDeleted = true;
        rt.DeletedAt = DateTimeOffset.UtcNow;
        rt.DeletedBy = _currentUser.Email;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("DeleteRoomType", "RoomType", id.ToString(), ct: ct);
    }

    private static RoomTypeDto Map(RoomType rt) => new(
        rt.Id, rt.Name, rt.Code, rt.Description, rt.BasePrice,
        rt.MaxAdults, rt.MaxChildren, rt.BedCount, rt.AreaSqm, rt.IsActive,
        rt.RoomTypeAmenities.Select(a => new AmenityDto(a.Amenity.Id, a.Amenity.Name, a.Amenity.Icon, a.Amenity.Category)).ToList()
    );
}

public class AmenityService : IAmenityService
{
    private readonly HotelProDbContext _db;

    public AmenityService(HotelProDbContext db) => _db = db;

    public async Task<IReadOnlyList<AmenityDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Amenities
            .AsNoTracking()
            .OrderBy(a => a.Category).ThenBy(a => a.Name)
            .Select(a => new AmenityDto(a.Id, a.Name, a.Icon, a.Category))
            .ToListAsync(ct);

    public async Task<AmenityDto> CreateAsync(CreateAmenityRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _db.Amenities.AnyAsync(a => a.Name == name, ct))
            throw new ConflictException($"Amenity '{name}' already exists.");

        var entity = new Amenity
        {
            Name = name,
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "check_circle" : request.Icon.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim()
        };
        _db.Amenities.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new AmenityDto(entity.Id, entity.Name, entity.Icon, entity.Category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Amenities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("Amenity not found.");
        _db.Amenities.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}

