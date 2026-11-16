using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdDeposits.Commands;
using TenantCore.Application.Features.IpdDeposits.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdDeposits.Handlers;

public sealed class CollectIpdDepositHandler(
    IIpdRegistrationRepository ipdRepository,
    IIpdDepositRepository depositRepository,
    IIpdPaymentRepository paymentRepository,
    ICounterSessionRepository counterSessionRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<CollectIpdDepositCommand, IpdDepositDto>
{
    public async Task<IpdDepositDto> Handle(CollectIpdDepositCommand request, CancellationToken cancellationToken)
    {
        var registration = await ipdRepository.GetByIdAsync(request.IpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.IpdRegistrationId);

        if (!accessValidator.CanAccess(registration.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        if (registration.Status != IpdStatus.Admitted)
            throw new InvalidOperationException(UserMessages.IpdNotAdmitted);

        var activeSession = await counterSessionRepository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);

        var deposit = IpdDeposit.Create(
            request.ApplicationId, registration.Id, request.Amount, request.CollectedByUserId, activeSession?.Id, request.Notes);
        await depositRepository.AddAsync(deposit, cancellationToken);

        var payment = await paymentRepository.GetByRegistrationAsync(registration.Id, request.ApplicationId, cancellationToken);
        if (payment is not null)
        {
            payment.IncreaseDeposited(request.Amount);
            paymentRepository.Update(payment);
        }

        await depositRepository.SaveChangesAsync(cancellationToken);
        return IpdDepositTranslator.ToDto(deposit);
    }
}
