using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Guests;
using HotelPro.Domain.Entities.Guests;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class GuestService : IGuestService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public GuestService(HotelProDbContext db, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<GuestDto>> GetGuestsAsync(PaginationParams pagination, bool? isVip, CancellationToken ct = default)
    {
        var query = _db.Guests
            .Include(g => g.Documents)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(g =>
                g.FullName.ToLower().Contains(term) ||
                g.Phone.Contains(term) ||
                (g.Email != null && g.Email.ToLower().Contains(term)) ||
                g.Documents.Any(d => d.DocumentNumber.ToLower().Contains(term)));
        }

        if (isVip.HasValue)
            query = query.Where(g => g.IsVip == isVip.Value);

        query = pagination.SortBy?.ToLower() switch
        {
            "fullname" => pagination.SortDesc ? query.OrderByDescending(g => g.FullName) : query.OrderBy(g => g.FullName),
            _ => pagination.SortDesc ? query.OrderBy(g => g.CreatedAt) : query.OrderByDescending(g => g.CreatedAt)
        };

        var totalCount = await query.CountAsync(ct);
        var guests = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<GuestDto>
        {
            Items = guests.Select(Map).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<GuestDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var guest = await _db.Guests
            .Include(g => g.Documents)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException("Guest not found.");

        return Map(guest);
    }

    public async Task<GuestDto> CreateAsync(SaveGuestRequest request, CancellationToken ct = default)
    {
        var guest = new Guest
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim(),
            Nationality = request.Nationality?.Trim(),
            DateOfBirth = request.DateOfBirth,
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim(),
            IsVip = request.IsVip,
            CreatedBy = _currentUser.Email
        };

        foreach (var doc in request.Documents)
        {
            guest.Documents.Add(new GuestDocument
            {
                GuestId = guest.Id,
                DocumentType = doc.DocumentType,
                DocumentNumber = doc.DocumentNumber.Trim(),
                IssuingCountry = doc.IssuingCountry?.Trim(),
                ExpiryDate = doc.ExpiryDate
            });
        }

        _db.Guests.Add(guest);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CreateGuest", "Guest", guest.Id.ToString(), newValues: new { guest.FullName, guest.Phone }, ct: ct);

        return await GetByIdAsync(guest.Id, ct);
    }

    public async Task<GuestDto> UpdateAsync(Guid id, SaveGuestRequest request, CancellationToken ct = default)
    {
        var guest = await _db.Guests
            .Include(g => g.Documents)
            .FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException("Guest not found.");

        guest.FullName = request.FullName.Trim();
        guest.Phone = request.Phone.Trim();
        guest.Email = request.Email?.Trim();
        guest.Nationality = request.Nationality?.Trim();
        guest.DateOfBirth = request.DateOfBirth;
        guest.Address = request.Address?.Trim();
        guest.Notes = request.Notes?.Trim();
        guest.IsVip = request.IsVip;
        guest.UpdatedAt = DateTimeOffset.UtcNow;
        guest.UpdatedBy = _currentUser.Email;

        _db.GuestDocuments.RemoveRange(guest.Documents);
        foreach (var doc in request.Documents)
        {
            _db.GuestDocuments.Add(new GuestDocument
            {
                GuestId = guest.Id,
                DocumentType = doc.DocumentType,
                DocumentNumber = doc.DocumentNumber.Trim(),
                IssuingCountry = doc.IssuingCountry?.Trim(),
                ExpiryDate = doc.ExpiryDate
            });
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UpdateGuest", "Guest", guest.Id.ToString(), newValues: new { guest.FullName, guest.Phone }, ct: ct);

        return await GetByIdAsync(guest.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException("Guest not found.");

        guest.IsDeleted = true;
        guest.DeletedAt = DateTimeOffset.UtcNow;
        guest.DeletedBy = _currentUser.Email;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("DeleteGuest", "Guest", id.ToString(), ct: ct);
    }

    private static GuestDto Map(Guest g) => new(
        g.Id, g.FullName, g.Phone, g.Email, g.Nationality, g.DateOfBirth,
        g.Address, g.Notes, g.IsVip, g.CreatedAt,
        g.Documents.Select(d => new GuestDocumentDto(d.Id, d.DocumentType, d.DocumentNumber, d.IssuingCountry, d.ExpiryDate)).ToList()
    );
}

