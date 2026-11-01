namespace TenantCore.Domain.Enums;

/// <summary>Internal to the durable workflow — never exposed to Shared/client.</summary>
public enum WorkflowTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Succeeded = 3,

    /// <summary>Terminal — either a permanent failure or attempts exhausted. Manually retryable via ResetForManualRetry.</summary>
    Failed = 4
}
