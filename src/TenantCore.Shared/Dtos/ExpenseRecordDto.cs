namespace TenantCore.Shared.Dtos;

public class ExpenseRecordDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid ExpenseCategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal PaidAmount { get; init; }
    public string? Notes { get; init; }
    public Guid RecordedByUserId { get; init; }
    public DateTime RecordedAt { get; init; }
    public Guid? CounterSessionId { get; init; }
}

public class ExpenseRecordSummaryDto
{
    public Guid Id { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal PaidAmount { get; init; }
    public string? Notes { get; init; }
    public DateTime RecordedAt { get; init; }
}

public sealed record CreateExpenseRecordRequest
{
    public Guid ExpenseCategoryId { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
    public Guid? CounterSessionId { get; init; }
}

public sealed record PayExpenseRecordRequest
{
    public decimal PayAmount { get; init; }
}

public sealed record UpdateExpenseRecordRequest
{
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
}
