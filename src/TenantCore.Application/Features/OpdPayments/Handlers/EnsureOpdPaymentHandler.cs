using MediatR;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class EnsureOpdPaymentHandler(
    IOpdPaymentRepository opdPaymentRepository,
    IDoctorFeeConfigRepository doctorFeeConfigRepository) : IRequestHandler<EnsureOpdPaymentCommand, Guid>
{
    public async Task<Guid> Handle(EnsureOpdPaymentCommand request, CancellationToken cancellationToken)
    {
        var existing = await opdPaymentRepository.GetByOpdRegistrationIdAsync(request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var feeConfig = await doctorFeeConfigRepository.GetByDoctorProfileIdAsync(request.DoctorProfileId, request.ApplicationId, cancellationToken);
        var visitFee = feeConfig?.IsActive == true ? feeConfig.VisitFee : 0;

        var payment = OpdPayment.Create(request.ApplicationId, request.OpdRegistrationId, visitFee);
        await opdPaymentRepository.AddAsync(payment, cancellationToken);
        await opdPaymentRepository.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }
}
