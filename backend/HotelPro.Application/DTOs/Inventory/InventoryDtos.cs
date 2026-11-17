using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Inventory;

public record InventoryCategoryDto(Guid Id, string Name);

public record InventoryItemDto(
    Guid Id,
    string Sku,
    string Name,
    Guid CategoryId,
    string CategoryName,
    string UnitOfMeasure,
    decimal CurrentStock,
    decimal ReorderLevel,
    decimal UnitCost,
    bool IsLowStock,
    bool IsActive
);

public record SaveInventoryItemRequest(
    string Sku,
    string Name,
    Guid CategoryId,
    string UnitOfMeasure,
    decimal ReorderLevel,
    decimal UnitCost,
    bool IsActive
);

public record SupplierDto(
    Guid Id,
    string Name,
    string? ContactPerson,
    string Phone,
    string? Email,
    string? Address,
    bool IsActive
);

public record SaveSupplierRequest(
    string Name,
    string? ContactPerson,
    string Phone,
    string? Email,
    string? Address,
    bool IsActive
);

public record PurchaseOrderItemRequest(Guid InventoryItemId, decimal QuantityOrdered, decimal UnitCost);

public record CreatePurchaseOrderRequest(Guid SupplierId, DateOnly? ExpectedDate, IReadOnlyList<PurchaseOrderItemRequest> Items);

public record PurchaseOrderItemDto(Guid Id, Guid InventoryItemId, string ItemName, decimal QuantityOrdered, decimal QuantityReceived, decimal UnitCost, decimal LineTotal);

public record PurchaseOrderDto(
    Guid Id,
    string OrderNumber,
    Guid SupplierId,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    PurchaseOrderStatus Status,
    decimal TotalAmount,
    IReadOnlyList<PurchaseOrderItemDto> Items
);

public record StockTransactionDto(
    Guid Id,
    Guid InventoryItemId,
    string ItemName,
    StockTransactionType TransactionType,
    decimal Quantity,
    decimal BalanceAfter,
    Guid? DepartmentId,
    string? DepartmentName,
    string? ReferenceNumber,
    string? Notes,
    DateTimeOffset TransactionDate
);

public record CreateStockTransactionRequest(
    Guid InventoryItemId,
    StockTransactionType TransactionType,
    decimal Quantity,
    Guid? DepartmentId,
    string? ReferenceNumber,
    string? Notes
);

