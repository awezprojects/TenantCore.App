using Microsoft.Extensions.Options;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Common.Workflow;

public sealed class WorkflowEnqueuer(IWorkflowTaskRepository workflowTaskRepository, IOptions<WorkflowOptions> options) : IWorkflowEnqueuer
{
    public async Task<WorkflowTask> EnqueueAsync(
        WorkflowTaskType taskType, string idempotencyKey, string aggregateType, Guid aggregateId,
        string? payloadJson = null, CancellationToken ct = default)
        => await workflowTaskRepository.EnqueueAsync(
            WorkflowTask.Enqueue(taskType, idempotencyKey, aggregateType, aggregateId, payloadJson, options.Value.MaxAttempts),
            ct);
}
