using MediatR;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Onboarding.Handlers;

/// <summary>Covers failures with no onboarding request behind them — e.g. a renewal payment's ActivateSubscription task.</summary>
public sealed class RetryWorkflowTaskHandler(
    IWorkflowTaskRepository workflowTaskRepository,
    IClinicOnboardingRequestRepository requestRepository)
    : IRequestHandler<RetryWorkflowTaskCommand>
{
    public async Task Handle(RetryWorkflowTaskCommand command, CancellationToken ct)
    {
        var task = await workflowTaskRepository.GetByIdAsync(command.TaskId, ct);
        if (task == null)
            throw new NotFoundException(nameof(WorkflowTask), command.TaskId);

        if (task.Status != WorkflowTaskStatus.Failed)
            throw new InvalidOperationException($"Only a Failed task can be retried (current status: {task.Status}).");

        task.ResetForManualRetry();

        if (task.AggregateType == nameof(ClinicOnboardingRequest))
        {
            var request = await requestRepository.GetByIdAsync(task.AggregateId, ct);
            request?.ClearAttention();
        }

        await workflowTaskRepository.SaveChangesAsync(ct);
    }
}
