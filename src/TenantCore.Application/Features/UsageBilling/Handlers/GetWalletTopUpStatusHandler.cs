using MediatR;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

public sealed class GetWalletTopUpStatusHandler(ISubscriptionPaymentRepository paymentRepository)
    : IRequestHandler<GetWalletTopUpStatusQuery, WalletTopUpPaymentDto?>
{
    public async Task<WalletTopUpPaymentDto?> Handle(GetWalletTopUpStatusQuery query, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdForClinicAsync(query.PaymentId, query.ApplicationId, ct);

        // Another clinic's payment, or a different purpose entirely: treat both as not found
        // rather than leaking anything about them.
        return payment is null || payment.Purpose != PaymentPurpose.WalletTopUp
            ? null
            : UsageBillingTranslator.ToDto(payment);
    }
}
