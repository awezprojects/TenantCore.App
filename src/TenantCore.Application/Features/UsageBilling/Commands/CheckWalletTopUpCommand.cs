using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// "I've paid — check now" for a wallet top-up, so a clinic does not have to wait for the webhook or
/// the reconciliation sweep. Never trusts the caller: it enqueues the same idempotent ConfirmPayment
/// task, which re-checks the link's real status with Razorpay.
/// </summary>
public sealed record CheckWalletTopUpCommand(Guid ApplicationId, Guid PaymentId)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Wallet Top-Up Checked";

    public string ActionLogContext => $"applicationId={ApplicationId}; paymentId={PaymentId}";
}
