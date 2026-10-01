using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Subscriptions.Commands;

/// <summary>
/// Clinic Admin's "I've paid — check now". Mirrors the onboarding equivalent: it never trusts the
/// caller, it only enqueues the same idempotent ConfirmPayment task the webhook would, which
/// re-checks with Razorpay directly.
/// </summary>
public sealed record CheckSubscriptionPaymentCommand(Guid ApplicationId, Guid PaymentId, Guid ActingUserId)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Subscription Payment Check";

    public string ActionLogContext => $"applicationId={ApplicationId}; paymentId={PaymentId}";
}
