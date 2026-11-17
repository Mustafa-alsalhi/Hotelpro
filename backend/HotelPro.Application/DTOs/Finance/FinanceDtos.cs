using HotelPro.Domain.Enums;

namespace HotelPro.Application.DTOs.Finance;

public record ExpenseCategoryDto(Guid Id, string Name, string? Description);

public record CreateExpenseCategoryRequest(string Name, string? Description);

public record ExpenseDto(
    Guid Id,
    string ExpenseNumber,
    Guid CategoryId,
    string CategoryName,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    PaymentMethod PaymentMethod,
    string? Notes
);

public record CreateExpenseRequest(
    Guid CategoryId,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    PaymentMethod PaymentMethod,
    string? Notes
);

public record RevenueRecordDto(
    Guid Id,
    RevenueSource Source,
    Guid? ReferenceId,
    string Description,
    decimal Amount,
    DateOnly RecognizedDate
);

public record CreateRevenueRequest(
    RevenueSource Source,
    string Description,
    decimal Amount,
    DateOnly RecognizedDate
);

public record FinancialSummaryDto(
    DateOnly From,
    DateOnly To,
    decimal InvoicedTotal,
    decimal CollectedPaymentsTotal,
    decimal RecognizedRevenuesTotal,
    decimal TotalExpenses,
    decimal NetOperatingResult
);

