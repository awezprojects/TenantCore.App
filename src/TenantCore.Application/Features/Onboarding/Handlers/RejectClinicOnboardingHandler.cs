using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class RejectClinicOnboardingHandler(
    IClinicOnboardingRequestRepository requestRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<RejectClinicOnboardingCommand>
{
    public async Task Handle(RejectClinicOnboardingCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(command.Id, ct);
        if (request == null)
            throw new NotFoundException(nameof(ClinicOnboardingRequest), command.Id);

        var paymentId = request.CurrentPaymentId;

        request.Reject(command.Reason, command.AdminUserId, command.AdminEmail);

        if (paymentId.HasValue)
        {
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.CancelPaymentLink, $"cancel-link:{paymentId.Value}", nameof(SubscriptionPayment), paymentId.Value, ct: ct);
        }

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.SendEmail, $"email:rejected:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
            OnboardingEmailTemplates.BuildPayload("Rejected", request.RequesterEmail, new { request.ClinicName, Reason = command.Reason }),
            ct);

        await requestRepository.SaveChangesAsync(ct);
    }
}
