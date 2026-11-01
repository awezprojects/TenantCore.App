using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class ApproveClinicOnboardingHandler(
    IClinicOnboardingRequestRepository requestRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<ApproveClinicOnboardingCommand>
{
    public async Task Handle(ApproveClinicOnboardingCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(command.Id, ct);
        if (request == null)
            throw new NotFoundException(nameof(ClinicOnboardingRequest), command.Id);

        // All approvals provision directly — no upfront subscription payment.
        // The clinic owner creates a registration-fee payment link from inside the app after activation.
        if (command.GrantTrial)
            request.ApproveWithTrial(command.ClinicCode, command.ReviewNote, command.AdminUserId, command.AdminEmail);
        else
            request.ApproveDirect(command.ClinicCode, command.ReviewNote, command.AdminUserId, command.AdminEmail);

        if (command.HasBillingIntent)
            StoreBillingIntent(request, command);

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id, ct: ct);

        await requestRepository.SaveChangesAsync(ct);
    }

    private static void StoreBillingIntent(ClinicOnboardingRequest request, ApproveClinicOnboardingCommand command) =>
        request.SetBillingApprovalIntent(
            command.BillingModeOverride,
            command.RegistrationFeeOverride,
            command.FreeTestDaysOverride,
            command.PayAsYouGoOpdRate,
            command.PayAsYouGoIpdAdmissionRate,
            command.PayAsYouGoOtSurgeryRate,
            command.PayAsYouGoIpdDischargeRate,
            command.PayAsYouGoInitialWalletCredit);
}
