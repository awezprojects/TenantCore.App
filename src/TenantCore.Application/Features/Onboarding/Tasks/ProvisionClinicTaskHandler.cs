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
    /// For a trial grant, the aggregate is the request itself. For a paid plan, this key matches
    /// exactly what OnboardingSelfHealer already enqueues ($"activate-subscription:{paymentId}"),
    /// since request.CurrentPaymentId is that same confirmed payment's id — a natural convergence
    /// point, not a coincidence, so either handler enqueuing first is fine.
    /// </summary>
    private static string ActivationKey(ClinicOnboardingRequest request) =>
        request.IsTrialGrant ? $"activate-subscription:trial:{request.Id}" : $"activate-subscription:{request.CurrentPaymentId}";

    /// <summary>
    /// Must match the aggregate ActivateSubscriptionTaskHandler dispatches on: the request itself
    /// for a trial grant, or the confirmed SubscriptionPayment for a paid plan. Passing the wrong
    /// one here previously routed every paid activation into the trial code path.
    /// </summary>
    private static (string AggregateType, Guid AggregateId) ActivationAggregate(ClinicOnboardingRequest request) =>
        request.IsTrialGrant
            ? (nameof(ClinicOnboardingRequest), request.Id)
            : (nameof(SubscriptionPayment), request.CurrentPaymentId!.Value);
}
