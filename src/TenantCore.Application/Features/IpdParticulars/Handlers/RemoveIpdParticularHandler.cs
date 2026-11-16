using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdParticulars.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.IpdParticulars.Handlers;

public sealed class RemoveIpdParticularHandler(
    IIpdParticularRepository ipdParticularRepository,
    IIpdPaymentRepository paymentRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<RemoveIpdParticularCommand>
{
    public async Task Handle(RemoveIpdParticularCommand request, CancellationToken cancellationToken)
    {
        var ipdParticular = await ipdParticularRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdParticular), request.Id);

        // Cross-tenant access is reported as 404, not 401/403 — never reveal that a particular
        // with this id exists at all to a caller who can't reach its clinic.
        if (ipdParticular.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(IpdParticular), request.Id);

        if (!accessValidator.CanAccess(ipdParticular.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var payment = await paymentRepository.GetByRegistrationAsync(ipdParticular.IpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is not null)
        {
            payment.DecreaseParticularsTotal(ipdParticular.Amount);
            paymentRepository.Update(payment);
        }

        ipdParticularRepository.Delete(ipdParticular);
        await ipdParticularRepository.SaveChangesAsync(cancellationToken);
    }
}
