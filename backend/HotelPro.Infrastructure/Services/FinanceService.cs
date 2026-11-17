using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Finance;
using HotelPro.Domain.Entities.Finance;
using HotelPro.Domain.Enums;
using HotelPro.Domain.Exceptions;
using HotelPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Services;

public class FinanceService : IFinanceService
{
    private readonly HotelProDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FinanceService(HotelProDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetExpenseCategoriesAsync(CancellationToken ct = default)
        => await _db.ExpenseCategories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryDto(c.Id, c.Name, c.Description))
            .ToListAsync(ct);

    public async Task<ExpenseCategoryDto> CreateExpenseCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _db.ExpenseCategories.AnyAsync(c => c.Name == name, ct))
            throw new ConflictException($"Expense category '{name}' already exists.");

        var cat = new ExpenseCategory { Name = name, Description = request.Description?.Trim() };
        _db.ExpenseCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return new ExpenseCategoryDto(cat.Id, cat.Name, cat.Description);
    }

    public async Task<PagedResult<ExpenseDto>> GetExpensesAsync(
        PaginationParams pagination,
        Guid? categoryId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct = default)
    {
        var query = _db.Expenses.Include(e => e.Category).AsNoTracking().AsQueryable();
        if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId.Value);
        if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value);
        if (to.HasValue) query = query.Where(e => e.ExpenseDate <= to.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(e => e.ExpenseDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ExpenseDto>
        {
            Items = items.Select(e => new ExpenseDto(
                e.Id, e.ExpenseNumber, e.CategoryId, e.Category.Name, e.Description,
                e.Amount, e.ExpenseDate, e.PaymentMethod, e.Notes)).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default)
    {
        var cat = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new NotFoundException("Expense category not found.");

        var count = await _db.Expenses.CountAsync(ct);
        var exp = new Expense
        {
            ExpenseNumber = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}",
            CategoryId = cat.Id,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            PaymentMethod = request.PaymentMethod,
            RecordedByUserId = _currentUser.UserId ?? Guid.Empty,
            Notes = request.Notes?.Trim(),
            CreatedBy = _currentUser.Email
        };

        _db.Expenses.Add(exp);
        await _db.SaveChangesAsync(ct);

        return new ExpenseDto(exp.Id, exp.ExpenseNumber, exp.CategoryId, cat.Name, exp.Description, exp.Amount, exp.ExpenseDate, exp.PaymentMethod, exp.Notes);
    }

    public async Task<PagedResult<RevenueRecordDto>> GetRevenuesAsync(
        PaginationParams pagination,
        RevenueSource? source,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct = default)
    {
        var query = _db.RevenueRecords.AsNoTracking().AsQueryable();
        if (source.HasValue) query = query.Where(r => r.Source == source.Value);
        if (from.HasValue) query = query.Where(r => r.RecognizedDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.RecognizedDate <= to.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.RecognizedDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<RevenueRecordDto>
        {
            Items = items.Select(r => new RevenueRecordDto(r.Id, r.Source, r.ReferenceId, r.Description, r.Amount, r.RecognizedDate)).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<RevenueRecordDto> CreateRevenueAsync(CreateRevenueRequest request, CancellationToken ct = default)
    {
        var rec = new RevenueRecord
        {
            Source = request.Source,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            RecognizedDate = request.RecognizedDate,
            RecordedByUserId = _currentUser.UserId ?? Guid.Empty
        };

        _db.RevenueRecords.Add(rec);
        await _db.SaveChangesAsync(ct);
        return new RevenueRecordDto(rec.Id, rec.Source, rec.ReferenceId, rec.Description, rec.Amount, rec.RecognizedDate);
    }

    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var invoicedAmounts = await _db.Invoices
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.IssueDate >= from && i.IssueDate <= to)
            .Select(i => i.GrandTotal)
            .ToListAsync(ct);
        var invoicedTotal = invoicedAmounts.Sum();

        var fromDt = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toDt = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var paymentAmounts = await _db.Payments
            .Where(p => p.PaymentDate >= fromDt && p.PaymentDate <= toDt)
            .Select(p => p.Amount)
            .ToListAsync(ct);
        var collectedPayments = paymentAmounts.Sum();

        var revenueAmounts = await _db.RevenueRecords
            .Where(r => r.RecognizedDate >= from && r.RecognizedDate <= to)
            .Select(r => r.Amount)
            .ToListAsync(ct);
        var recognizedRevenues = revenueAmounts.Sum();

        var expenseAmounts = await _db.Expenses
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .Select(e => e.Amount)
            .ToListAsync(ct);
        var totalExpenses = expenseAmounts.Sum();

        return new FinancialSummaryDto(
            from,
            to,
            invoicedTotal,
            collectedPayments,
            recognizedRevenues,
            totalExpenses,
            recognizedRevenues - totalExpenses
        );
    }
}

