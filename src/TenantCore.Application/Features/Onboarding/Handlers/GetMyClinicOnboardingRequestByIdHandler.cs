using MediatR;
using TenantCore.Application.Features.Onboarding.Queries;
using TenantCore.Application.Features.Onboarding.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Onboarding;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class GetMyClinicOnboardingRequestByIdHandler(
    IClinicOnboardingRequestRepository requestRepository,
    ISubscriptionPaymentRepository paymentRepository)
    : IRequestHandler<GetMyClinicOnboardingRequestByIdQuery, ClinicOnboardingRequestDto>
{
    public async Task<ClinicOnboardingRequestDto> Handle(GetMyClinicOnboardingRequestByIdQuery query, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdForUserAsync(query.Id, query.UserId, ct);
        if (request == null)
            throw new NotFoundException(nameof(ClinicOnboardingRequest), query.Id);

        SubscriptionPayment? payment = null;
        if (request.Status == ClinicOnboardingStatus.AwaitingPayment && request.CurrentPaymentId.HasValue)
            payment = await paymentRepository.GetByIdAsync(request.CurrentPaymentId.Value, ct);

        return ClinicOnboardingTranslator.ToDto(request, payment);
    }
}
