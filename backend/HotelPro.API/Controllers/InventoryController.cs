using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Inventory;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service) => _service = service;

    [HttpGet("categories")]
    [HasPermission(PermissionsCatalog.Inventory.View)]
    public async Task<ActionResult<IReadOnlyList<InventoryCategoryDto>>> GetCategories(CancellationToken ct)
        => Ok(await _service.GetCategoriesAsync(ct));

    [HttpPost("categories")]
    [HasPermission(PermissionsCatalog.Inventory.Create)]
    public async Task<ActionResult<InventoryCategoryDto>> CreateCategory([FromBody] string name, CancellationToken ct)
        => Ok(await _service.CreateCategoryAsync(name, ct));

    [HttpGet("items")]
    [HasPermission(PermissionsCatalog.Inventory.View)]
    public async Task<ActionResult<PagedResult<InventoryItemDto>>> GetItems(
        [FromQuery] PaginationParams pagination,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? lowStockOnly,
        CancellationToken ct)
        => Ok(await _service.GetItemsAsync(pagination, categoryId, lowStockOnly, ct));

    [HttpPost("items")]
    [HasPermission(PermissionsCatalog.Inventory.Create)]
    public async Task<ActionResult<InventoryItemDto>> CreateItem(
        [FromBody] SaveInventoryItemRequest request,
        [FromServices] IValidator<SaveInventoryItemRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateItemAsync(request, ct));
    }

    [HttpPut("items/{id:guid}")]
    [HasPermission(PermissionsCatalog.Inventory.Edit)]
    public async Task<ActionResult<InventoryItemDto>> UpdateItem(
        Guid id,
        [FromBody] SaveInventoryItemRequest request,
        [FromServices] IValidator<SaveInventoryItemRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateItemAsync(id, request, ct));
    }

    [HttpGet("suppliers")]
    [HasPermission(PermissionsCatalog.Inventory.View)]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> GetSuppliers(CancellationToken ct)
        => Ok(await _service.GetSuppliersAsync(ct));

    [HttpPost("suppliers")]
    [HasPermission(PermissionsCatalog.Inventory.Create)]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] SaveSupplierRequest request, CancellationToken ct)
        => Ok(await _service.CreateSupplierAsync(request, ct));

    [HttpGet("purchase-orders")]
    [HasPermission(PermissionsCatalog.Inventory.View)]
    public async Task<ActionResult<PagedResult<PurchaseOrderDto>>> GetPurchaseOrders(
        [FromQuery] PaginationParams pagination,
        [FromQuery] PurchaseOrderStatus? status,
        CancellationToken ct)
        => Ok(await _service.GetPurchaseOrdersAsync(pagination, status, ct));

    [HttpPost("purchase-orders")]
    [HasPermission(PermissionsCatalog.Inventory.Create)]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
        => Ok(await _service.CreatePurchaseOrderAsync(request, ct));

    [HttpPost("purchase-orders/{id:guid}/receive")]
    [HasPermission(PermissionsCatalog.Inventory.Approve)]
    public async Task<ActionResult<PurchaseOrderDto>> ReceivePurchaseOrder(Guid id, CancellationToken ct)
        => Ok(await _service.ReceivePurchaseOrderAsync(id, ct));

    [HttpGet("transactions")]
    [HasPermission(PermissionsCatalog.Inventory.View)]
    public async Task<ActionResult<PagedResult<StockTransactionDto>>> GetTransactions(
        [FromQuery] PaginationParams pagination,
        [FromQuery] Guid? itemId,
        CancellationToken ct)
        => Ok(await _service.GetTransactionsAsync(pagination, itemId, ct));

    [HttpPost("transactions")]
    [HasPermission(PermissionsCatalog.Inventory.Edit)]
    public async Task<ActionResult<StockTransactionDto>> RecordTransaction(
        [FromBody] CreateStockTransactionRequest request,
        [FromServices] IValidator<CreateStockTransactionRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.RecordTransactionAsync(request, ct));
    }
}

