using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;

namespace TenantCore.Application.Common.Workflow;

/// <summary>
/// Builds and enqueues a WorkflowTask with the standard idempotency-key convention. Handlers
/// call this inside the same unit of work as the state change the task continues — the caller
/// still owns calling SaveChangesAsync once, at the end.
/// </summary>
public interface IWorkflowEnqueuer
{
    Task<WorkflowTask> EnqueueAsync(
        WorkflowTaskType taskType, string idempotencyKey, string aggregateType, Guid aggregateId,
        string? payloadJson = null, CancellationToken ct = default);
}
