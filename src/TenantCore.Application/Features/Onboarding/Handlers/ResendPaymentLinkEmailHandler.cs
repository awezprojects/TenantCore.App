using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class ResendPaymentLinkEmailHandler(
    IClinicOnboardingRequestRepository requestRepository,
    ISubscriptionPaymentRepository paymentRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<ResendPaymentLinkEmailCommand>
{
    public async Task Handle(ResendPaymentLinkEmailCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(command.Id, ct);
        if (request == null)
            throw new NotFoundException(nameof(ClinicOnboardingRequest), command.Id);

        if (request.Status != ClinicOnboardingStatus.AwaitingPayment || !request.CurrentPaymentId.HasValue)
            throw new InvalidOperationException("This request is not awaiting payment.");

        var payment = await paymentRepository.GetByIdAsync(request.CurrentPaymentId.Value, ct);
        if (payment == null || string.IsNullOrEmpty(payment.PaymentLinkUrl))
            throw new InvalidOperationException("No payment link exists yet for this request.");

        // A fresh idempotency suffix — unlike every other email, a resend is meant to send again.
        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.SendEmail, $"email:payment-link:{payment.Id}:resend:{Guid.NewGuid()}", nameof(ClinicOnboardingRequest), request.Id,
            OnboardingEmailTemplates.BuildPayload("PaymentLink", request.RequesterEmail, new
            {
                request.ClinicName,
                PlanName = payment.PlanName,
                Amount = payment.Amount.ToString("N2"),
                Currency = payment.Currency,
                PaymentLinkUrl = payment.PaymentLinkUrl,
                ExpiresAt = payment.LinkExpiresAt?.ToString("dd MMM yyyy") ?? string.Empty
            }),
            ct);

        await requestRepository.SaveChangesAsync(ct);
    }
}
