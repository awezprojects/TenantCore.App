using Microsoft.Extensions.Configuration;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Tasks;

public sealed class CreatePaymentLinkTaskHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IClinicOnboardingRequestRepository requestRepository,
    IPaymentGateway paymentGateway,
    IWorkflowEnqueuer workflowEnqueuer,
    IConfiguration configuration)
    : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.CreatePaymentLink;

    public async Task HandleAsync(Domain.Entities.WorkflowTask task, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(task.AggregateId, ct);
        if (payment == null)
            throw new PermanentWorkflowException($"SubscriptionPayment {task.AggregateId} not found.");

        // Idempotent — already done, or no longer relevant (superseded/cancelled/expired).
        if (payment.Status is SubscriptionPaymentStatus.LinkCreated or SubscriptionPaymentStatus.Paid
            or SubscriptionPaymentStatus.Superseded or SubscriptionPaymentStatus.Cancelled or SubscriptionPaymentStatus.Expired)
            return;

        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Razorpay is not configured.");

        var request = payment.OnboardingRequestId.HasValue
            ? await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct)
            : null;

        var expiryDays = configuration.GetValue("Razorpay:PaymentLinkExpiryDays", 7);
        var callbackUrl = configuration["Razorpay:CallbackUrl"] ?? string.Empty;
        var expireBy = DateTime.UtcNow.AddDays(expiryDays);
        var description = $"{payment.PlanName} subscription";

        var (result, link) = await paymentGateway.CreatePaymentLinkAsync(
            payment.Id.ToString("N"), payment.AmountInMinorUnits, payment.Currency, description,
            payment.PayerName, payment.PayerEmail, payment.PayerPhone, expireBy, callbackUrl, ct);

        if (!result.Success)
        {
            if (result.ErrorMessage == "DUPLICATE_REFERENCE")
            {
                // The earlier attempt's response was lost, but the link was actually created —
                // adopt it instead of creating a second one.
                var (findResult, existingLink) = await paymentGateway.FindPaymentLinkByReferenceAsync(payment.Id.ToString("N"), ct);
                if (!findResult.Success || existingLink == null)
                    throw new InvalidOperationException("Razorpay reports a duplicate reference but the existing link could not be found.");
                link = existingLink;
            }
            else if (result.IsTransient)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "Razorpay create payment link failed.");
            }
            else
            {
                throw new PermanentWorkflowException(result.ErrorMessage ?? "Razorpay create payment link failed permanently.");
            }
        }

        if (link == null)
            throw new InvalidOperationException("Razorpay did not return a payment link.");

        payment.SetLink(link.Id, link.ShortUrl, link.ExpireBy);
        request?.MarkAwaitingPayment();

        var clinicNameForEmail = request?.ClinicName ?? "your clinic";
        var emailTemplate = payment.Purpose == PaymentPurpose.Onboarding ? "PaymentLink" : "RenewalPaymentLink";
        var emailAggregateType = request != null ? nameof(ClinicOnboardingRequest) : nameof(SubscriptionPayment);
        var emailAggregateId = request?.Id ?? payment.Id;

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.SendEmail, $"email:payment-link:{payment.Id}", emailAggregateType, emailAggregateId,
            OnboardingEmailTemplates.BuildPayload(emailTemplate, payment.PayerEmail, new
            {
                ClinicName = clinicNameForEmail,
                PlanName = payment.PlanName,
                Amount = payment.Amount.ToString("N2"),
                Currency = payment.Currency,
                PaymentLinkUrl = payment.PaymentLinkUrl,
                ExpiresAt = payment.LinkExpiresAt?.ToString("dd MMM yyyy") ?? string.Empty
            }),
            ct);

        await paymentRepository.SaveChangesAsync(ct);
    }
}
