using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Domain.Entities.Operations;
using GuestServiceEntity = HotelPro.Domain.Entities.Operations.GuestService;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class GuestServicesAppService : IGuestServicesAppService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GuestServicesAppService(HotelProDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<GuestServiceDto>> GetCatalogAsync(bool includeInactive = true, CancellationToken ct = default)
    {
        var query = _db.GuestServices.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new GuestServiceDto(s.Id, s.Name, s.Description, s.Category, s.Price, s.Taxable, s.IsActive))
            .ToListAsync(ct);
    }

    public async Task<GuestServiceDto> CreateServiceAsync(SaveGuestServiceRequest request, CancellationToken ct = default)
    {
        var entity = new GuestServiceEntity
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category,
            Price = request.Price,
            Taxable = request.Taxable,
            IsActive = request.IsActive,
            CreatedBy = _currentUser.Email
        };
        _db.GuestServices.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new GuestServiceDto(entity.Id, entity.Name, entity.Description, entity.Category, entity.Price, entity.Taxable, entity.IsActive);
    }

    public async Task<GuestServiceDto> UpdateServiceAsync(Guid id, SaveGuestServiceRequest request, CancellationToken ct = default)
    {
        var entity = await _db.GuestServices.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Service not found.");

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.Category = request.Category;
        entity.Price = request.Price;
        entity.Taxable = request.Taxable;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        return new GuestServiceDto(entity.Id, entity.Name, entity.Description, entity.Category, entity.Price, entity.Taxable, entity.IsActive);
    }

    public async Task<PagedResult<ServiceOrderDto>> GetOrdersAsync(
        PaginationParams pagination,
        ServiceOrderStatus? status,
        Guid? reservationId,
        CancellationToken ct = default)
    {
        var query = _db.ServiceOrders
            .Include(o => o.Reservation)
            .Include(o => o.Room)
            .Include(o => o.Guest)
            .Include(o => o.Items).ThenInclude(i => i.GuestService)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (reservationId.HasValue) query = query.Where(o => o.ReservationId == reservationId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.RequestedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ServiceOrderDto>
        {
            Items = items.Select(MapOrder).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<ServiceOrderDto> CreateOrderAsync(CreateServiceOrderRequest request, CancellationToken ct = default)
    {
        var res = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");

        if (res.Status != ReservationStatus.CheckedIn)
            throw new BusinessRuleException("Service orders can only be added to Checked-In reservations.");

        var settings = await _db.HotelSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var taxRate = settings?.TaxRatePercent ?? 0m;

        var serviceIds = request.Items.Select(i => i.GuestServiceId).Distinct().ToList();
        var services = await _db.GuestServices.Where(s => serviceIds.Contains(s.Id) && s.IsActive).ToDictionaryAsync(s => s.Id, ct);

        var orderCount = await _db.ServiceOrders.CountAsync(ct);
        var order = new ServiceOrder
        {
            OrderNumber = $"SRV-{DateTime.UtcNow:yyyyMMdd}-{(orderCount + 1):D4}",
            ReservationId = res.Id,
            RoomId = request.RoomId,
            GuestId = res.PrimaryGuestId,
            Status = ServiceOrderStatus.Pending,
            Notes = request.Notes?.Trim(),
            CreatedBy = _currentUser.Email
        };

        decimal subTotal = 0m;
        decimal taxTotal = 0m;

        foreach (var itemReq in request.Items)
        {
            if (!services.TryGetValue(itemReq.GuestServiceId, out var svc))
                throw new NotFoundException("Selected service is not available.");

            var lineSub = svc.Price * itemReq.Quantity;
            var lineTax = svc.Taxable ? Math.Round(lineSub * (taxRate / 100m), 2, MidpointRounding.AwayFromZero) : 0m;
            var lineTotal = lineSub + lineTax;

            subTotal += lineSub;
            taxTotal += lineTax;

            order.Items.Add(new ServiceOrderItem
            {
                ServiceOrderId = order.Id,
                GuestServiceId = svc.Id,
                Quantity = itemReq.Quantity,
                UnitPrice = svc.Price,
                TaxAmount = lineTax,
                LineTotal = lineTotal
            });
        }

        order.SubTotal = subTotal;
        order.TaxAmount = taxTotal;
        order.TotalAmount = subTotal + taxTotal;

        _db.ServiceOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.ServiceOrders
            .Include(o => o.Reservation).Include(o => o.Room).Include(o => o.Guest)
            .Include(o => o.Items).ThenInclude(i => i.GuestService)
            .AsNoTracking()
            .FirstAsync(o => o.Id == order.Id, ct);

        return MapOrder(saved);
    }

    public async Task<ServiceOrderDto> UpdateOrderStatusAsync(Guid id, UpdateServiceOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await _db.ServiceOrders
            .Include(o => o.Reservation).Include(o => o.Room).Include(o => o.Guest)
            .Include(o => o.Items).ThenInclude(i => i.GuestService)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Service order not found.");

        order.Status = request.Status;
        order.AssignedUserId = request.AssignedUserId ?? order.AssignedUserId;
        if (request.Status == ServiceOrderStatus.Completed)
            order.CompletedAt = DateTimeOffset.UtcNow;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        return MapOrder(order);
    }

    private static ServiceOrderDto MapOrder(ServiceOrder o) => new(
        o.Id, o.OrderNumber, o.ReservationId, o.Reservation.ReservationNumber,
        o.RoomId, o.Room.RoomNumber, o.GuestId, o.Guest.FullName,
        o.Status, o.SubTotal, o.TaxAmount, o.TotalAmount, o.AssignedUserId,
        o.RequestedAt, o.CompletedAt, o.Notes,
        o.Items.Select(i => new ServiceOrderItemDto(i.Id, i.GuestServiceId, i.GuestService.Name, i.Quantity, i.UnitPrice, i.TaxAmount, i.LineTotal)).ToList()
    );
}
