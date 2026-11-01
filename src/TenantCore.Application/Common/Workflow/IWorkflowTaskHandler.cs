using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;

namespace TenantCore.Application.Common.Workflow;

/// <summary>
/// One handler per WorkflowTaskType, resolved by the processor from a fresh DI scope per
/// attempt. Throw <see cref="Domain.Exceptions.PermanentWorkflowException"/> for a non-retryable
/// failure — any other exception is treated as transient and retried with backoff.
/// </summary>
public interface IWorkflowTaskHandler
{
    WorkflowTaskType TaskType { get; }

    Task HandleAsync(WorkflowTask task, CancellationToken ct);
}
