using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Subscriptions.Commands;

/// <summary>Not clinic-scoped — authenticity comes entirely from the HMAC signature, verified before anything is persisted.</summary>
public sealed record RecordPaymentWebhookCommand(string RawBody, string Signature, string EventId)
    : IRequest, IBusinessAction
{
    public string ActionName => "Razorpay Webhook Received";
}
