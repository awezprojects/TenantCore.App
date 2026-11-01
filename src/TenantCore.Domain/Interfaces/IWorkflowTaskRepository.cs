using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;

namespace TenantCore.Domain.Interfaces;

public interface IWorkflowTaskRepository : IRepository<WorkflowTask>
{
    /// <summary>No-op (returns the existing row) when a task with this IdempotencyKey already exists — enqueue must never throw on a race.</summary>
    Task<WorkflowTask> EnqueueAsync(WorkflowTask task, CancellationToken ct = default);

    /// <summary>Due Pending tasks, oldest first, up to batchSize. Does not lease — the caller calls TryLease and saves.</summary>
    Task<IReadOnlyList<WorkflowTask>> GetDueAsync(int batchSize, DateTime utcNow, CancellationToken ct = default);

    Task<IReadOnlyList<WorkflowTask>> GetOpenForAggregateAsync(string aggregateType, Guid aggregateId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowTask>> GetFailedForAggregateAsync(string aggregateType, Guid aggregateId, CancellationToken ct = default);
    Task<WorkflowTask?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
}
