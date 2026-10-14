using MediatR;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

public sealed class GetActiveRegistrationFeePaymentHandler(ISubscriptionPaymentRepository paymentRepository)
    : IRequestHandler<GetActiveRegistrationFeePaymentQuery, RegistrationFeePaymentDto?>
{
    public async Task<RegistrationFeePaymentDto?> Handle(
        GetActiveRegistrationFeePaymentQuery query, CancellationToken ct)
    {
        var payment = await paymentRepository.GetLatestRegistrationFeePaymentAsync(query.ApplicationId, ct);
        return payment is null ? null : UsageBillingTranslator.ToDto(payment, asRegistrationFee: true);
    }
}
