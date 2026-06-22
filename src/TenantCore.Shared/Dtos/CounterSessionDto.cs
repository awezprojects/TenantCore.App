using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class CounterSessionDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public DateTime SessionDate { get; init; }
    public Guid OpenedByUserId { get; init; }
    public DateTime OpenedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public CounterSessionStatus Status { get; init; }
    public decimal TotalCollected { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetAmount { get; init; }
}

public class CounterSessionSummaryDto
{
    public Guid Id { get; init; }
    public DateTime SessionDate { get; init; }
    public DateTime OpenedAt { get; init; }
    public CounterSessionStatus Status { get; init; }
    public Guid OpenedByUserId { get; init; }
    public decimal TotalCollected { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetAmount { get; init; }
}

public sealed record OpenCounterSessionRequest
{
    public DateTime SessionDate { get; init; }
}

public sealed record CloseCounterSessionRequest;
