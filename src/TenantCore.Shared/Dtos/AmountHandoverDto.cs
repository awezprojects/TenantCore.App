using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class AmountHandoverDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid CounterSessionId { get; init; }
    public Guid HandedOverByUserId { get; init; }
    public Guid HandedOverToUserId { get; init; }
    public string HandedOverToRoleName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
    public HandoverStatus Status { get; init; }
    public DateTime HandedOverAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
    public string? ResolutionNotes { get; init; }
}

public class AmountHandoverSummaryDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public HandoverStatus Status { get; init; }
    public DateTime HandedOverAt { get; init; }
    public Guid HandedOverByUserId { get; init; }
    public Guid HandedOverToUserId { get; init; }
    public string HandedOverToRoleName { get; init; } = string.Empty;
}

public sealed record CreateAmountHandoverRequest
{
    public Guid CounterSessionId { get; init; }
    public Guid HandedOverToUserId { get; init; }
    public string HandedOverToRoleName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
}

public sealed record ResolveAmountHandoverRequest
{
    public string? ResolutionNotes { get; init; }
}

public sealed record CollectAmountHandoverRequest
{
    public Guid CounterSessionId { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
}
