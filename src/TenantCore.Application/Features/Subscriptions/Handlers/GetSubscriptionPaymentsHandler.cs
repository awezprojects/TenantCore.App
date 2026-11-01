using MediatR;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class GetSubscriptionPaymentsHandler(ISubscriptionPaymentRepository paymentRepository)
    : IRequestHandler<GetSubscriptionPaymentsQuery, IEnumerable<SubscriptionPaymentDto>>
{
    public async Task<IEnumerable<SubscriptionPaymentDto>> Handle(GetSubscriptionPaymentsQuery query, CancellationToken ct)
    {
        var payments = await paymentRepository.GetForClinicAsync(query.ApplicationId, ct);
        return payments.Select(SubscriptionPaymentTranslator.ToDto);
    }
}
