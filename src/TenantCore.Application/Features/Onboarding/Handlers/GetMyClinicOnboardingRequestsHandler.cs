using MediatR;
using TenantCore.Application.Features.Onboarding.Queries;
using TenantCore.Application.Features.Onboarding.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Onboarding;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class GetMyClinicOnboardingRequestsHandler(
    IClinicOnboardingRequestRepository requestRepository,
    ISubscriptionPaymentRepository paymentRepository)
    : IRequestHandler<GetMyClinicOnboardingRequestsQuery, IEnumerable<ClinicOnboardingRequestDto>>
{
    public async Task<IEnumerable<ClinicOnboardingRequestDto>> Handle(GetMyClinicOnboardingRequestsQuery query, CancellationToken ct)
    {
        var requests = await requestRepository.GetForUserAsync(query.UserId, ct);
        var dtos = new List<ClinicOnboardingRequestDto>();

        foreach (var request in requests)
        {
            SubscriptionPayment? payment = null;
            if (request.Status == ClinicOnboardingStatus.AwaitingPayment && request.CurrentPaymentId.HasValue)
                payment = await paymentRepository.GetByIdAsync(request.CurrentPaymentId.Value, ct);

            dtos.Add(ClinicOnboardingTranslator.ToDto(request, payment));
        }

        return dtos;
    }
}
