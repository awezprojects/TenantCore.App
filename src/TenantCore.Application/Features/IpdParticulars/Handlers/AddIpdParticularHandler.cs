using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdParticulars.Commands;
using TenantCore.Application.Features.IpdParticulars.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdParticulars.Handlers;

public sealed class AddIpdParticularHandler(
    IIpdRegistrationRepository ipdRepository,
    IParticularRepository particularRepository,
    IIpdParticularRepository ipdParticularRepository,
    IIpdPaymentRepository paymentRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<AddIpdParticularCommand, IpdParticularDto>
{
    public async Task<IpdParticularDto> Handle(AddIpdParticularCommand request, CancellationToken cancellationToken)
    {
        var registration = await ipdRepository.GetByIdAsync(request.IpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.IpdRegistrationId);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        if (registration.Status != IpdStatus.Admitted)
            throw new InvalidOperationException(UserMessages.IpdNotAdmitted);

        var particular = await particularRepository.GetByIdAsync(request.ParticularId, cancellationToken);
        if (particular is null || particular.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Particular), request.ParticularId);

        var ipdParticular = IpdParticular.Create(
            request.ApplicationId, registration.Id, particular.Id, particular.Name, request.Amount, request.AddedByUserId);
        await ipdParticularRepository.AddAsync(ipdParticular, cancellationToken);

        var payment = await paymentRepository.GetByRegistrationAsync(registration.Id, request.ApplicationId, cancellationToken);
        if (payment is not null)
        {
            payment.IncreaseParticularsTotal(ipdParticular.Amount);
            paymentRepository.Update(payment);
        }

        await ipdParticularRepository.SaveChangesAsync(cancellationToken);
        return IpdParticularTranslator.ToDto(ipdParticular);
    }
}
