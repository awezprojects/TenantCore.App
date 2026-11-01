using MediatR;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class RetryClinicOnboardingHandler(
    IClinicOnboardingRequestRepository requestRepository,
    IWorkflowTaskRepository workflowTaskRepository,
    IOnboardingSelfHealer selfHealer)
    : IRequestHandler<RetryClinicOnboardingCommand>
{
    public async Task Handle(RetryClinicOnboardingCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(command.Id, ct);
        if (request == null)
            throw new NotFoundException(nameof(ClinicOnboardingRequest), command.Id);

        if (!string.IsNullOrWhiteSpace(command.ClinicCode))
            request.OverrideClinicCode(command.ClinicCode);

        request.ClearAttention();

        var failedTasks = await workflowTaskRepository.GetFailedForAggregateAsync(nameof(ClinicOnboardingRequest), request.Id, ct);
        if (failedTasks.Count > 0)
        {
            foreach (var task in failedTasks)
                task.ResetForManualRetry();

            await requestRepository.SaveChangesAsync(ct);
            return;
        }

        // No Failed task on the request itself — the stuck step may be on its current payment
        // instead (e.g. CreatePaymentLink failed). Save the clinic-code override and attention
        // clear first, then let the self-healer work out and re-enqueue the correct next step.
        await requestRepository.SaveChangesAsync(ct);
        await selfHealer.HealRequestAsync(request.Id, ct);
    }
}
