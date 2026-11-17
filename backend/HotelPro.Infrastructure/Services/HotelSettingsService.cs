using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Hotel;
using HotelPro.Domain.Entities.Hotel;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class HotelSettingsService : IHotelSettingsService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public HotelSettingsService(HotelProDbContext db, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<HotelSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var hotel = await _db.Hotels
            .Include(h => h.Settings)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Hotel configuration not found.");

        var s = hotel.Settings ?? new HotelSettings { HotelId = hotel.Id };
        return Map(hotel, s);
    }

    public async Task<HotelSettingsDto> UpdateSettingsAsync(UpdateHotelSettingsRequest request, CancellationToken ct = default)
    {
        var hotel = await _db.Hotels
            .Include(h => h.Settings)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Hotel configuration not found.");

        hotel.Name = request.Name.Trim();
        hotel.LegalName = request.LegalName?.Trim();
        hotel.LogoUrl = request.LogoUrl?.Trim();
        hotel.Address = request.Address.Trim();
        hotel.Phone = request.Phone.Trim();
        hotel.Email = request.Email.Trim();
        hotel.TaxNumber = request.TaxNumber?.Trim();
        hotel.UpdatedAt = DateTimeOffset.UtcNow;
        hotel.UpdatedBy = _currentUser.Email;

        if (hotel.Settings is null)
        {
            hotel.Settings = new HotelSettings { HotelId = hotel.Id };
            _db.HotelSettings.Add(hotel.Settings);
        }

        var s = hotel.Settings;
        s.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        s.CurrencySymbol = request.CurrencySymbol.Trim();
        s.TimeZoneId = request.TimeZoneId.Trim();
        s.DateFormat = request.DateFormat.Trim();
        s.DefaultLanguage = request.DefaultLanguage;
        s.DefaultCheckInTime = request.DefaultCheckInTime;
        s.DefaultCheckOutTime = request.DefaultCheckOutTime;
        s.TaxRatePercent = request.TaxRatePercent;
        s.ServiceChargePercent = request.ServiceChargePercent;
        s.RequireInspectionAfterCleaning = request.RequireInspectionAfterCleaning;
        s.AllowCheckoutWithBalancePermission = request.AllowCheckoutWithBalancePermission;
        s.CancellationPolicyHours = request.CancellationPolicyHours;
        s.CancellationFeePercent = request.CancellationFeePercent;
        s.UpdatedAt = DateTimeOffset.UtcNow;
        s.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UpdateHotelSettings", "HotelSettings", hotel.Id.ToString(), newValues: request, ct: ct);

        return Map(hotel, s);
    }

    private static HotelSettingsDto Map(Hotel h, HotelSettings s) => new(
        h.Id, h.Name, h.LegalName, h.LogoUrl, h.Address, h.Phone, h.Email, h.TaxNumber,
        s.CurrencyCode, s.CurrencySymbol, s.TimeZoneId, s.DateFormat, s.DefaultLanguage,
        s.DefaultCheckInTime, s.DefaultCheckOutTime, s.TaxRatePercent, s.ServiceChargePercent,
        s.RequireInspectionAfterCleaning, s.AllowCheckoutWithBalancePermission,
        s.CancellationPolicyHours, s.CancellationFeePercent
    );
}

