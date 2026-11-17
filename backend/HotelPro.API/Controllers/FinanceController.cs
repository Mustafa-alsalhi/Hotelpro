using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Finance;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/finance")]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _service;

    public FinanceController(IFinanceService service) => _service = service;

    [HttpGet("expense-categories")]
    [HasPermission(PermissionsCatalog.Finance.View)]
    public async Task<ActionResult<IReadOnlyList<ExpenseCategoryDto>>> GetCategories(CancellationToken ct)
        => Ok(await _service.GetExpenseCategoriesAsync(ct));

    [HttpPost("expense-categories")]
    [HasPermission(PermissionsCatalog.Finance.Create)]
    public async Task<ActionResult<ExpenseCategoryDto>> CreateCategory([FromBody] CreateExpenseCategoryRequest request, CancellationToken ct)
        => Ok(await _service.CreateExpenseCategoryAsync(request, ct));

    [HttpGet("expenses")]
    [HasPermission(PermissionsCatalog.Finance.View)]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> GetExpenses(
        [FromQuery] PaginationParams pagination,
        [FromQuery] Guid? categoryId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
        => Ok(await _service.GetExpensesAsync(pagination, categoryId, from, to, ct));

    [HttpPost("expenses")]
    [HasPermission(PermissionsCatalog.Finance.Create)]
    public async Task<ActionResult<ExpenseDto>> CreateExpense(
        [FromBody] CreateExpenseRequest request,
        [FromServices] IValidator<CreateExpenseRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateExpenseAsync(request, ct));
    }

    [HttpGet("revenues")]
    [HasPermission(PermissionsCatalog.Finance.View)]
    public async Task<ActionResult<PagedResult<RevenueRecordDto>>> GetRevenues(
        [FromQuery] PaginationParams pagination,
        [FromQuery] RevenueSource? source,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
        => Ok(await _service.GetRevenuesAsync(pagination, source, from, to, ct));

    [HttpPost("revenues")]
    [HasPermission(PermissionsCatalog.Finance.Create)]
    public async Task<ActionResult<RevenueRecordDto>> CreateRevenue(
        [FromBody] CreateRevenueRequest request,
        [FromServices] IValidator<CreateRevenueRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.CreateRevenueAsync(request, ct));
    }

    [HttpGet("summary")]
    [HasPermission(PermissionsCatalog.Finance.ViewReports)]
    public async Task<ActionResult<FinancialSummaryDto>> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var start = from ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _service.GetFinancialSummaryAsync(start, end, ct));
    }
}

