using MediatR;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class EnsureOpdPaymentHandler(
    IOpdPaymentRepository opdPaymentRepository,
    IOpdRegistrationRepository opdRegistrationRepository,
    IDoctorFeeConfigRepository doctorFeeConfigRepository) : IRequestHandler<EnsureOpdPaymentCommand, Guid>
{
    public async Task<Guid> Handle(EnsureOpdPaymentCommand request, CancellationToken cancellationToken)
    {
        var existing = await opdPaymentRepository.GetByOpdRegistrationIdAsync(request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        // The fee on the OPD registration is what reception sees, collects and displays as
        // "Paid Rs.X". The payment must mirror it — otherwise a doctor with no active fee
        // config yields a Rs.0 payment, the collected amount is recorded as Rs.0, and a
        // cancelled visit can never be refunded (NeedsRefund sees nothing to give back).
        // The doctor fee config is only a fallback when the registration carries no fee.
        var registration = await opdRegistrationRepository.GetByIdAsync(request.OpdRegistrationId, cancellationToken);
        decimal visitFee = registration?.Fee ?? 0m;
        if (visitFee <= 0)
        {
            var feeConfig = await doctorFeeConfigRepository.GetByDoctorProfileIdAsync(request.DoctorProfileId, request.ApplicationId, cancellationToken);
            visitFee = feeConfig?.IsActive == true ? feeConfig.VisitFee : 0;
        }

        var payment = OpdPayment.Create(request.ApplicationId, request.OpdRegistrationId, visitFee);
        await opdPaymentRepository.AddAsync(payment, cancellationToken);
        await opdPaymentRepository.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }
}
