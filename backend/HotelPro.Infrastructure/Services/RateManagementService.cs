using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Rates;
using HotelPro.Domain.Entities.Rates;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class RateManagementService : IRateManagementService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public RateManagementService(HotelProDbContext db, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<RatePlanDto>> GetRatePlansAsync(bool includeInactive = true, CancellationToken ct = default)
    {
        var query = _db.RatePlans.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(rp => rp.IsActive);

        return await query
            .OrderBy(rp => rp.Name)
            .Select(rp => new RatePlanDto(
                rp.Id, rp.Name, rp.Code, rp.Description, rp.IsRefundable,
                rp.WeekendMultiplier, rp.LongStayMinNights, rp.LongStayDiscountPercent,
                rp.CorporateCode, rp.IsActive))
            .ToListAsync(ct);
    }

    public async Task<RatePlanDto> CreateRatePlanAsync(SaveRatePlanRequest request, CancellationToken ct = default)
    {
        var hotelId = await _db.Hotels.Select(h => h.Id).FirstOrDefaultAsync(ct);
        if (hotelId == Guid.Empty)
            throw new BusinessRuleException("Hotel must be initialized first.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.RatePlans.AnyAsync(rp => rp.HotelId == hotelId && rp.Code == code, ct))
            throw new ConflictException($"Rate plan code '{code}' already exists.");

        var entity = new RatePlan
        {
            HotelId = hotelId,
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description?.Trim(),
            IsRefundable = request.IsRefundable,
            WeekendMultiplier = request.WeekendMultiplier,
            LongStayMinNights = request.LongStayMinNights,
            LongStayDiscountPercent = request.LongStayDiscountPercent,
            CorporateCode = request.CorporateCode?.Trim(),
            IsActive = request.IsActive,
            CreatedBy = _currentUser.Email
        };

        _db.RatePlans.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CreateRatePlan", "RatePlan", entity.Id.ToString(), newValues: new { entity.Name, entity.Code }, ct: ct);

        return new RatePlanDto(
            entity.Id, entity.Name, entity.Code, entity.Description, entity.IsRefundable,
            entity.WeekendMultiplier, entity.LongStayMinNights, entity.LongStayDiscountPercent,
            entity.CorporateCode, entity.IsActive);
    }

    public async Task<RatePlanDto> UpdateRatePlanAsync(Guid id, SaveRatePlanRequest request, CancellationToken ct = default)
    {
        var entity = await _db.RatePlans.FirstOrDefaultAsync(rp => rp.Id == id, ct)
            ?? throw new NotFoundException("Rate plan not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.RatePlans.AnyAsync(rp => rp.Id != id && rp.HotelId == entity.HotelId && rp.Code == code, ct))
            throw new ConflictException($"Rate plan code '{code}' already exists.");

        entity.Name = request.Name.Trim();
        entity.Code = code;
        entity.Description = request.Description?.Trim();
        entity.IsRefundable = request.IsRefundable;
        entity.WeekendMultiplier = request.WeekendMultiplier;
        entity.LongStayMinNights = request.LongStayMinNights;
        entity.LongStayDiscountPercent = request.LongStayDiscountPercent;
        entity.CorporateCode = request.CorporateCode?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UpdateRatePlan", "RatePlan", entity.Id.ToString(), newValues: new { entity.Name, entity.Code }, ct: ct);

        return new RatePlanDto(
            entity.Id, entity.Name, entity.Code, entity.Description, entity.IsRefundable,
            entity.WeekendMultiplier, entity.LongStayMinNights, entity.LongStayDiscountPercent,
            entity.CorporateCode, entity.IsActive);
    }

    public async Task<IReadOnlyList<RoomRateDto>> GetRoomRatesAsync(Guid? roomTypeId, Guid? ratePlanId, CancellationToken ct = default)
    {
        var query = _db.RoomRates
            .Include(rr => rr.RoomType)
            .Include(rr => rr.RatePlan)
            .AsNoTracking()
            .AsQueryable();

        if (roomTypeId.HasValue) query = query.Where(rr => rr.RoomTypeId == roomTypeId.Value);
        if (ratePlanId.HasValue) query = query.Where(rr => rr.RatePlanId == ratePlanId.Value);

        return await query
            .OrderByDescending(rr => rr.StartDate)
            .Select(rr => new RoomRateDto(
                rr.Id, rr.RoomTypeId, rr.RoomType.Name, rr.RatePlanId, rr.RatePlan.Name,
                rr.StartDate, rr.EndDate, rr.PricePerNight, rr.Priority))
            .ToListAsync(ct);
    }

    public async Task<RoomRateDto> CreateRoomRateAsync(SaveRoomRateRequest request, CancellationToken ct = default)
    {
        var rt = await _db.RoomTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.RoomTypeId, ct)
            ?? throw new NotFoundException("Room type not found.");
        var rp = await _db.RatePlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.RatePlanId, ct)
            ?? throw new NotFoundException("Rate plan not found.");

        var entity = new RoomRate
        {
            RoomTypeId = request.RoomTypeId,
            RatePlanId = request.RatePlanId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PricePerNight = request.PricePerNight,
            Priority = request.Priority,
            CreatedBy = _currentUser.Email
        };

        _db.RoomRates.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new RoomRateDto(
            entity.Id, entity.RoomTypeId, rt.Name, entity.RatePlanId, rp.Name,
            entity.StartDate, entity.EndDate, entity.PricePerNight, entity.Priority);
    }

    public async Task DeleteRoomRateAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.RoomRates.FirstOrDefaultAsync(rr => rr.Id == id, ct)
            ?? throw new NotFoundException("Room rate not found.");
        _db.RoomRates.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RateQuoteDto> CalculateQuoteAsync(RateQuoteRequest request, CancellationToken ct = default)
    {
        if (request.CheckOutDate <= request.CheckInDate)
            throw new BusinessRuleException("Check-out date must be after check-in date.");

        var roomType = await _db.RoomTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.RoomTypeId, ct)
            ?? throw new NotFoundException("Room type not found.");
        var ratePlan = await _db.RatePlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.RatePlanId, ct)
            ?? throw new NotFoundException("Rate plan not found.");
        var settings = await _db.HotelSettings.AsNoTracking().FirstOrDefaultAsync(ct);

        var seasonalRates = await _db.RoomRates.AsNoTracking()
            .Where(rr => rr.RoomTypeId == request.RoomTypeId
                         && rr.RatePlanId == request.RatePlanId
                         && rr.StartDate < request.CheckOutDate
                         && rr.EndDate >= request.CheckInDate)
            .OrderByDescending(rr => rr.Priority)
            .ToListAsync(ct);

        var nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
        var breakdown = new List<NightlyRateBreakdownDto>();
        decimal singleRoomSubTotal = 0m;

        for (var i = 0; i < nights; i++)
        {
            var date = request.CheckInDate.AddDays(i);
            var seasonal = seasonalRates.FirstOrDefault(sr => date >= sr.StartDate && date <= sr.EndDate);
            var baseRate = seasonal?.PricePerNight ?? roomType.BasePrice;
            var isWeekend = date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
            var multiplier = isWeekend ? ratePlan.WeekendMultiplier : 1.00m;
            var nightRate = Math.Round(baseRate * multiplier, 2, MidpointRounding.AwayFromZero);

            singleRoomSubTotal += nightRate;
            breakdown.Add(new NightlyRateBreakdownDto(date, baseRate, multiplier, nightRate));
        }

        var subTotal = Math.Round(singleRoomSubTotal * Math.Max(1, request.RoomsCount), 2, MidpointRounding.AwayFromZero);
        var longStayDiscount = nights >= ratePlan.LongStayMinNights && ratePlan.LongStayDiscountPercent > 0
            ? Math.Round(subTotal * (ratePlan.LongStayDiscountPercent / 100m), 2, MidpointRounding.AwayFromZero)
            : 0m;

        var taxableBase = Math.Max(0m, subTotal - longStayDiscount - request.ManualDiscountAmount);
        var taxRate = settings?.TaxRatePercent ?? 0m;
        var serviceRate = settings?.ServiceChargePercent ?? 0m;

        var serviceCharge = Math.Round(taxableBase * (serviceRate / 100m), 2, MidpointRounding.AwayFromZero);
        var taxAmount = Math.Round((taxableBase + serviceCharge) * (taxRate / 100m), 2, MidpointRounding.AwayFromZero);
        var grandTotal = taxableBase + serviceCharge + taxAmount;
        var avgRate = nights > 0 ? Math.Round(singleRoomSubTotal / nights, 2, MidpointRounding.AwayFromZero) : 0m;

        return new RateQuoteDto(
            nights, avgRate, subTotal, longStayDiscount, request.ManualDiscountAmount,
            taxAmount, serviceCharge, grandTotal, breakdown);
    }
}

