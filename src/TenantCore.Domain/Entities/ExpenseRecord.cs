using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class ExpenseRecord : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid ExpenseCategoryId { get; private set; }
    public string CategoryName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public Guid? CounterSessionId { get; private set; }
    public decimal PaidAmount { get; private set; }

    private ExpenseRecord() { }

    public void Pay(decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Payment amount must be greater than zero.");
        PaidAmount = Math.Min(PaidAmount + amount, Amount);
        SetUpdatedAt();
    }

    public void UpdateAmount(decimal amount, string? notes)
    {
        if (amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");
        if (amount < PaidAmount) throw new InvalidOperationException("Cannot reduce the bill amount below what has already been paid.");
        Amount = amount;
        Notes = notes;
        SetUpdatedAt();
    }

    public static ExpenseRecord Create(
        Guid applicationId,
        Guid expenseCategoryId,
        string categoryName,
        decimal amount,
        string? notes,
        Guid recordedByUserId,
        Guid? counterSessionId) => new()
    {
        ApplicationId = applicationId,
        ExpenseCategoryId = expenseCategoryId,
        CategoryName = categoryName,
        Amount = amount,
        Notes = notes,
        RecordedByUserId = recordedByUserId,
        RecordedAt = DateTime.UtcNow,
        CounterSessionId = counterSessionId,
        CreatedAt = DateTime.UtcNow
    };
}
