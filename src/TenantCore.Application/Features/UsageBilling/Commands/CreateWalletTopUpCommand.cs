using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// A Clinic Admin topping up the clinic's prepaid wallet. Creates the Razorpay link's payment row;
/// the link itself is created by the same background task as every other payment, so a top-up needs
/// no separate payment path. Nothing is credited here — <c>CreditWalletTaskHandler</c> credits the
/// wallet only once the gateway confirms the payment.
/// </summary>
public sealed record CreateWalletTopUpCommand(Guid ApplicationId, decimal Amount, Guid ActingUserId)
    : IRequest<WalletTopUpPaymentDto>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Wallet Top-Up Requested";

    public string ActionLogContext => $"applicationId={ApplicationId}; amount={Amount}; userId={ActingUserId}";
}
