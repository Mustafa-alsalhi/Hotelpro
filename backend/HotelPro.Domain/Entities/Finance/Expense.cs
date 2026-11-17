using HotelPro.Domain.Common;
using HotelPro.Domain.Enums;

namespace HotelPro.Domain.Entities.Finance;

public class ExpenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class Expense : AuditableEntity
{
    public string ExpenseNumber { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public Guid RecordedByUserId { get; set; }
    public string? AttachmentPath { get; set; }
    public string? Notes { get; set; }
}

