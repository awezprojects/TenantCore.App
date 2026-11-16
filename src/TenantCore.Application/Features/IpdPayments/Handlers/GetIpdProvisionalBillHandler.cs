using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Queries;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Handlers;

// Always computed live, as-of now, and never persisted — "amount payable if discharged today."
public sealed class GetIpdProvisionalBillHandler(
    IIpdRegistrationRepository ipdRepository,
    IIpdPaymentRepository paymentRepository,
    IIpdBillCalculator billCalculator,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdProvisionalBillQuery, IpdProvisionalBillDto>
{
    public async Task<IpdProvisionalBillDto> Handle(GetIpdProvisionalBillQuery request, CancellationToken cancellationToken)
    {
        if (!accessValidator.CanAccess(request.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var registration = await ipdRepository.GetByIdAsync(request.IpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.IpdRegistrationId);

        var payment = await paymentRepository.GetByRegistrationAsync(request.IpdRegistrationId, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdPayment), request.IpdRegistrationId);

        var asOfUtc = DateTime.UtcNow;
        var roomCharges = await billCalculator.CalculateRoomChargesAsync(registration, asOfUtc, cancellationToken);
        return IpdPaymentTranslator.ToProvisionalBillDto(registration, payment, roomCharges, asOfUtc);
    }
}
