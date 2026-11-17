using HotelPro.Application.Common;
using HotelPro.Application.DTOs.Finance;
using HotelPro.Domain.Enums;

namespace HotelPro.Application.Common.Interfaces;

public interface IBillingService
{
    Task<PagedResult<InvoiceDto>> GetInvoicesAsync(PaginationParams pagination, InvoiceStatus? status, CancellationToken ct = default);
    Task<InvoiceDto> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default);
    Task<InvoiceDto> IssueInvoiceForReservationAsync(IssueInvoiceRequest request, CancellationToken ct = default);
    Task<InvoiceDto> CancelInvoiceAsync(Guid id, CancelInvoiceRequest request, CancellationToken ct = default);

    Task<PagedResult<PaymentDto>> GetPaymentsAsync(PaginationParams pagination, Guid? invoiceId, CancellationToken ct = default);
    Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default);
    Task<PaymentDto> ReversePaymentAsync(Guid paymentId, ReversePaymentRequest request, CancellationToken ct = default);
}

public interface IFinanceService
{
    Task<IReadOnlyList<ExpenseCategoryDto>> GetExpenseCategoriesAsync(CancellationToken ct = default);
    Task<ExpenseCategoryDto> CreateExpenseCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default);

    Task<PagedResult<ExpenseDto>> GetExpensesAsync(PaginationParams pagination, Guid? categoryId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default);

    Task<PagedResult<RevenueRecordDto>> GetRevenuesAsync(PaginationParams pagination, RevenueSource? source, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<RevenueRecordDto> CreateRevenueAsync(CreateRevenueRequest request, CancellationToken ct = default);

    Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

