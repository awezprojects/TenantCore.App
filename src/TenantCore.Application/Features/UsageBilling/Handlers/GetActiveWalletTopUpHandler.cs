using MediatR;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

public sealed class GetActiveWalletTopUpHandler(ISubscriptionPaymentRepository paymentRepository)
    : IRequestHandler<GetActiveWalletTopUpQuery, WalletTopUpPaymentDto?>
{
    public async Task<WalletTopUpPaymentDto?> Handle(GetActiveWalletTopUpQuery query, CancellationToken ct)
    {
        var payment = await paymentRepository.GetOpenLinkForClinicAsync(query.ApplicationId, ct);

        // GetOpenLinkForClinicAsync is shared across every "one open clinic link" purpose
        // (renewal, admin-assigned, wallet top-up, registration fee) — only hand back a wallet
        // top-up here, never someone else's open link.
        return payment is null || payment.Purpose != PaymentPurpose.WalletTopUp
            ? null
            : UsageBillingTranslator.ToDto(payment);
    }
}
