using TenantCore.Domain.Common;
using TenantCore.Domain.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// One durable, retryable unit of work — the transactional-outbox row a handler inserts in the
/// same SaveChanges as the state change it continues. Not tenant-scoped (internal plumbing).
/// See plan/clinic-trial-razorpay-subscriptions/PLAN.md "Reliability Design".
/// </summary>
public class WorkflowTask : AuditableEntity
{
    public WorkflowTaskType TaskType { get; private set; }

    /// <summary>Unique — guarantees the same task is never enqueued twice (e.g. "provision:{requestId}").</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }

    /// <summary>Only SendEmail tasks use this — template id + recipient + model, as JSON.</summary>
    public string? PayloadJson { get; private set; }

    public WorkflowTaskStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTime NextAttemptAt { get; private set; }

    public DateTime? LockedUntil { get; private set; }
    public string? LockedBy { get; private set; }

    public string? LastError { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private WorkflowTask() { }

    public static WorkflowTask Enqueue(
        WorkflowTaskType taskType, string idempotencyKey, string aggregateType, Guid aggregateId,
        string? payloadJson = null, int maxAttempts = 72) => new()
        {
            Id = Guid.NewGuid(),
            TaskType = taskType,
            IdempotencyKey = idempotencyKey,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            PayloadJson = payloadJson,
            Status = WorkflowTaskStatus.Pending,
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            NextAttemptAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// True when the lease was acquired. Leasable when Pending and due, OR when InProgress with
    /// an expired lease — the latter is what lets another instance recover a task after the
    /// instance that was running it crashed mid-attempt, per the reliability design.
    /// </summary>
    public bool TryLease(string instanceId, DateTime leaseUntil, DateTime utcNow)
    {
        var leaseExpired = !LockedUntil.HasValue || LockedUntil.Value <= utcNow;

        if (Status == WorkflowTaskStatus.Pending)
        {
            if (NextAttemptAt > utcNow || !leaseExpired)
                return false;
        }
        else if (Status == WorkflowTaskStatus.InProgress)
        {
            if (!leaseExpired)
                return false;
        }
        else
        {
            return false; // Succeeded or Failed — never leasable
        }

        Status = WorkflowTaskStatus.InProgress;
        LockedBy = instanceId;
        LockedUntil = leaseUntil;
        AttemptCount += 1;
        SetUpdatedAt();
        return true;
    }

    public void Succeed()
    {
        Status = WorkflowTaskStatus.Succeeded;
        CompletedAt = DateTime.UtcNow;
        LockedBy = null;
        LockedUntil = null;
        LastError = null;
        SetUpdatedAt();
    }

    public void ScheduleRetry(string error, DateTime nextAttemptAt)
    {
        Status = WorkflowTaskStatus.Pending;
        LastError = Truncate(error);
        NextAttemptAt = nextAttemptAt;
        LockedBy = null;
        LockedUntil = null;
        SetUpdatedAt();
    }

    /// <summary>Terminal — a permanent error, or attempts exhausted. Only a manual retry can move it forward again.</summary>
    public void Fail(string error)
    {
        Status = WorkflowTaskStatus.Failed;
        LastError = Truncate(error);
        CompletedAt = DateTime.UtcNow;
        LockedBy = null;
        LockedUntil = null;
        SetUpdatedAt();
    }

    public void ResetForManualRetry()
    {
        Status = WorkflowTaskStatus.Pending;
        AttemptCount = 0;
        NextAttemptAt = DateTime.UtcNow;
        LockedBy = null;
        LockedUntil = null;
        LastError = null;
        CompletedAt = null;
        SetUpdatedAt();
    }

    public bool ExceedsMaxAttempts => AttemptCount >= MaxAttempts;

    private static string Truncate(string value) => value.Length <= 2000 ? value : value[..2000];
}
