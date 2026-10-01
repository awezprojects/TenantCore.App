using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Cancels an open clinic payment link with no replacement. If the clinic turns out to have paid
/// it in the meantime, CancelPaymentLinkTaskHandler keeps that payment and activates the term —
/// money that arrived is never discarded.
/// </summary>
public sealed class CancelClinicPaymentLinkHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<CancelClinicPaymentLinkCommand>
{
    public async Task Handle(CancelClinicPaymentLinkCommand command, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdForClinicAsync(command.PaymentId, command.ApplicationId, ct);
        if (payment is null)
            throw new NotFoundException(nameof(SubscriptionPayment), command.PaymentId);

        // Onboarding links belong to the onboarding request's own flow (reject / change amount),
        // not to a clinic that already exists.
        if (!payment.IsClinicLink)
            throw new InvalidOperationException("Only a renewal or admin-assigned link can be cancelled here.");

        if (!payment.IsOpen)
            throw new InvalidOperationException($"This payment is {payment.Status} — there is no open link to cancel.");

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, $"cancel-link:{payment.Id}",
            nameof(SubscriptionPayment), payment.Id, ct: ct);

        await paymentRepository.SaveChangesAsync(ct);
    }
}
