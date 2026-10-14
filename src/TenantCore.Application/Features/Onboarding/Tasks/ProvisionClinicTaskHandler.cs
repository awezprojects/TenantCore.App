using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Onboarding.Tasks;

public sealed class ProvisionClinicTaskHandler(
    IClinicOnboardingRequestRepository requestRepository,
    IAuthProvisioningService authProvisioningService,
    IWorkflowEnqueuer workflowEnqueuer)
    : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.ProvisionClinic;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(task.AggregateId, ct);
        if (request == null)
            throw new PermanentWorkflowException($"ClinicOnboardingRequest {task.AggregateId} not found.");

        if (request.ProvisionedApplicationId.HasValue)
        {
            // Already provisioned (a previous attempt succeeded but crashed before the
            // ActivateSubscription enqueue committed) — just make sure that step is queued.
            var (aggType, aggId) = ActivationAggregate(request);
            await workflowEnqueuer.EnqueueAsync(WorkflowTaskType.ActivateSubscription, ActivationKey(request), aggType, aggId, ct: ct);
            await requestRepository.SaveChangesAsync(ct);
            return;
        }

        var outcome = await authProvisioningService.ProvisionClinicAsync(
            request.Id, request.RequestedByUserId,
            request.ClinicName, request.EffectiveClinicCode, request.Address, request.ClinicContactNumber,
            request.DoctorName, request.MedicalRegistrationNumber, request.OfficialEmail, request.Website, ct);

        switch (outcome.Kind)
        {
            case ProvisioningOutcomeKind.Success:
            {
                request.SetProvisionedClinic(outcome.ApplicationId!.Value);
                var (aggType, aggId) = ActivationAggregate(request);
                await workflowEnqueuer.EnqueueAsync(WorkflowTaskType.ActivateSubscription, ActivationKey(request), aggType, aggId, ct: ct);
                await requestRepository.SaveChangesAsync(ct);
                break;
            }

            case ProvisioningOutcomeKind.Transient:
                throw new InvalidOperationException(outcome.ErrorMessage ?? "Auth provisioning failed transiently.");

            case ProvisioningOutcomeKind.Permanent:
            default:
                throw new PermanentWorkflowException(outcome.ErrorMessage ?? "Auth provisioning failed permanently.");
        }
    }

    /// <summary>
    /// Returns the workflow dedup key for the ActivateSubscription task.
    /// Trial grants and direct-provisioned clinics (Subscription / PAYG approved without an upfront
    /// payment) both use the request as the aggregate; paid-plan clinics use the payment.
    /// </summary>
    private static string ActivationKey(ClinicOnboardingRequest request)
    {
        if (request.IsTrialGrant)        return $"activate-subscription:trial:{request.Id}";
        if (!request.CurrentPaymentId.HasValue) return $"activate-subscription:direct:{request.Id}";
        return $"activate-subscription:{request.CurrentPaymentId}";
    }

    /// <summary>
    /// Must match the aggregate ActivateSubscriptionTaskHandler dispatches on. The handler routes
    /// on AggregateType: ClinicOnboardingRequest → HandleTrialGrantAsync (covers trial grants, PAYG
    /// and direct-provisioned subscriptions); SubscriptionPayment → HandlePaymentBasedAsync.
    /// </summary>
    private static (string AggregateType, Guid AggregateId) ActivationAggregate(ClinicOnboardingRequest request)
    {
        if (request.IsTrialGrant || !request.CurrentPaymentId.HasValue)
            return (nameof(ClinicOnboardingRequest), request.Id);
        return (nameof(SubscriptionPayment), request.CurrentPaymentId!.Value);
    }
}
