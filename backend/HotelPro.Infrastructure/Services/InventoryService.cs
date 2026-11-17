using System.Data;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Inventory;
using HotelPro.Domain.Entities.Inventory;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public InventoryService(HotelProDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InventoryCategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => await _db.InventoryCategories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new InventoryCategoryDto(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<InventoryCategoryDto> CreateCategoryAsync(string name, CancellationToken ct = default)
    {
        var clean = name.Trim();
        if (await _db.InventoryCategories.AnyAsync(c => c.Name == clean, ct))
            throw new ConflictException($"Category '{clean}' already exists.");

        var cat = new InventoryCategory { Name = clean };
        _db.InventoryCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return new InventoryCategoryDto(cat.Id, cat.Name);
    }

    public async Task<PagedResult<InventoryItemDto>> GetItemsAsync(
        PaginationParams pagination,
        Guid? categoryId,
        bool? lowStockOnly,
        CancellationToken ct = default)
    {
        var query = _db.InventoryItems.Include(i => i.Category).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var term = pagination.Search.Trim().ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(term) || i.Sku.ToLower().Contains(term));
        }
        if (categoryId.HasValue) query = query.Where(i => i.CategoryId == categoryId.Value);
        if (lowStockOnly == true) query = query.Where(i => i.CurrentStock <= i.ReorderLevel);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(i => i.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<InventoryItemDto>
        {
            Items = items.Select(MapItem).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<InventoryItemDto> CreateItemAsync(SaveInventoryItemRequest request, CancellationToken ct = default)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await _db.InventoryItems.AnyAsync(i => i.Sku == sku, ct))
            throw new ConflictException($"SKU '{sku}' already exists.");

        var cat = await _db.InventoryCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new NotFoundException("Inventory category not found.");

        var item = new InventoryItem
        {
            Sku = sku,
            Name = request.Name.Trim(),
            CategoryId = cat.Id,
            UnitOfMeasure = request.UnitOfMeasure.Trim(),
            CurrentStock = 0m, // Always starts at 0; modified only via StockTransactions
            ReorderLevel = request.ReorderLevel,
            UnitCost = request.UnitCost,
            IsActive = request.IsActive,
            CreatedBy = _currentUser.Email
        };

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(ct);
        item.Category = cat;
        return MapItem(item);
    }

    public async Task<InventoryItemDto> UpdateItemAsync(Guid id, SaveInventoryItemRequest request, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException("Inventory item not found.");

        item.Name = request.Name.Trim();
        item.CategoryId = request.CategoryId;
        item.UnitOfMeasure = request.UnitOfMeasure.Trim();
        item.ReorderLevel = request.ReorderLevel;
        item.UnitCost = request.UnitCost;
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        return MapItem(item);
    }

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken ct = default)
        => await _db.Suppliers.AsNoTracking().OrderBy(s => s.Name)
            .Select(s => new SupplierDto(s.Id, s.Name, s.ContactPerson, s.Phone, s.Email, s.Address, s.IsActive))
            .ToListAsync(ct);

    public async Task<SupplierDto> CreateSupplierAsync(SaveSupplierRequest request, CancellationToken ct = default)
    {
        var s = new Supplier
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = request.IsActive,
            CreatedBy = _currentUser.Email
        };
        _db.Suppliers.Add(s);
        await _db.SaveChangesAsync(ct);
        return new SupplierDto(s.Id, s.Name, s.ContactPerson, s.Phone, s.Email, s.Address, s.IsActive);
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(
        PaginationParams pagination,
        PurchaseOrderStatus? status,
        CancellationToken ct = default)
    {
        var query = _db.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.InventoryItem)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue) query = query.Where(po => po.Status == status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(po => po.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<PurchaseOrderDto>
        {
            Items = items.Select(MapPo).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, ct)
            ?? throw new NotFoundException("Supplier not found.");

        var count = await _db.PurchaseOrders.CountAsync(ct);
        var po = new PurchaseOrder
        {
            OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}",
            SupplierId = supplier.Id,
            ExpectedDate = request.ExpectedDate,
            Status = PurchaseOrderStatus.Ordered,
            CreatedBy = _currentUser.Email
        };

        decimal total = 0m;
        foreach (var itemReq in request.Items)
        {
            var lineTotal = Math.Round(itemReq.QuantityOrdered * itemReq.UnitCost, 2, MidpointRounding.AwayFromZero);
            total += lineTotal;
            po.Items.Add(new PurchaseOrderItem
            {
                PurchaseOrderId = po.Id,
                InventoryItemId = itemReq.InventoryItemId,
                QuantityOrdered = itemReq.QuantityOrdered,
                QuantityReceived = 0m,
                UnitCost = itemReq.UnitCost,
                LineTotal = lineTotal
            });
        }
        po.TotalAmount = total;

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.PurchaseOrders
            .Include(x => x.Supplier).Include(x => x.Items).ThenInclude(i => i.InventoryItem)
            .AsNoTracking().FirstAsync(x => x.Id == po.Id, ct);
        return MapPo(saved);
    }

    public async Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(Guid id, CancellationToken ct = default)
    {
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var po = await _db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Purchase order not found.");

        if (po.Status == PurchaseOrderStatus.Received)
            throw new BusinessRuleException("Purchase order is already received.");

        foreach (var item in po.Items)
        {
            item.QuantityReceived = item.QuantityOrdered;
            item.InventoryItem.CurrentStock += item.QuantityOrdered;
            item.InventoryItem.UnitCost = item.UnitCost;

            _db.StockTransactions.Add(new StockTransaction
            {
                InventoryItemId = item.InventoryItemId,
                TransactionType = StockTransactionType.PurchaseIn,
                Quantity = item.QuantityOrdered,
                BalanceAfter = item.InventoryItem.CurrentStock,
                ReferenceNumber = po.OrderNumber,
                Notes = $"Received from {po.Supplier.Name}",
                PerformedByUserId = _currentUser.UserId ?? Guid.Empty
            });
        }

        po.Status = PurchaseOrderStatus.Received;
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return MapPo(po);
    }

    public async Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(PaginationParams pagination, Guid? itemId, CancellationToken ct = default)
    {
        var query = _db.StockTransactions
            .Include(t => t.InventoryItem)
            .Include(t => t.Department)
            .AsNoTracking()
            .AsQueryable();

        if (itemId.HasValue) query = query.Where(t => t.InventoryItemId == itemId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.TransactionDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<StockTransactionDto>
        {
            Items = items.Select(t => new StockTransactionDto(
                t.Id, t.InventoryItemId, t.InventoryItem.Name, t.TransactionType,
                t.Quantity, t.BalanceAfter, t.DepartmentId, t.Department?.Name,
                t.ReferenceNumber, t.Notes, t.TransactionDate)).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<StockTransactionDto> RecordTransactionAsync(CreateStockTransactionRequest request, CancellationToken ct = default)
    {
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, ct)
            ?? throw new NotFoundException("Inventory item not found.");

        var isOutgoing = request.TransactionType is StockTransactionType.DepartmentIssue or StockTransactionType.AdjustmentOut;
        if (isOutgoing && item.CurrentStock < request.Quantity)
            throw new BusinessRuleException($"Insufficient stock for '{item.Name}'. Available: {item.CurrentStock}, requested: {request.Quantity}.");

        item.CurrentStock = isOutgoing ? item.CurrentStock - request.Quantity : item.CurrentStock + request.Quantity;

        var st = new StockTransaction
        {
            InventoryItemId = item.Id,
            TransactionType = request.TransactionType,
            Quantity = request.Quantity,
            BalanceAfter = item.CurrentStock,
            DepartmentId = request.DepartmentId,
            ReferenceNumber = request.ReferenceNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            PerformedByUserId = _currentUser.UserId ?? Guid.Empty
        };

        _db.StockTransactions.Add(st);
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return new StockTransactionDto(
            st.Id, item.Id, item.Name, st.TransactionType, st.Quantity, st.BalanceAfter,
            st.DepartmentId, null, st.ReferenceNumber, st.Notes, st.TransactionDate);
    }

    private static InventoryItemDto MapItem(InventoryItem i) => new(
        i.Id, i.Sku, i.Name, i.CategoryId, i.Category.Name, i.UnitOfMeasure,
        i.CurrentStock, i.ReorderLevel, i.UnitCost, i.CurrentStock <= i.ReorderLevel, i.IsActive);

    private static PurchaseOrderDto MapPo(PurchaseOrder po) => new(
        po.Id, po.OrderNumber, po.SupplierId, po.Supplier.Name, po.OrderDate, po.ExpectedDate, po.Status, po.TotalAmount,
        po.Items.Select(i => new PurchaseOrderItemDto(i.Id, i.InventoryItemId, i.InventoryItem.Name, i.QuantityOrdered, i.QuantityReceived, i.UnitCost, i.LineTotal)).ToList());
}

