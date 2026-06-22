using MediatR;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class CollectAmountHandoverHandler(
    IAmountHandoverRepository handoverRepository,
    ICounterSessionRepository sessionRepository) : IRequestHandler<CollectAmountHandoverCommand, Guid>
{
    public async Task<Guid> Handle(CollectAmountHandoverCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.Request.CounterSessionId, cancellationToken);
        if (session is null || session.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(CounterSession), request.Request.CounterSessionId);

        // Admin/doctor collects — handover is FROM the reception (session opener) TO the collector
        var handover = AmountHandover.Create(
            request.ApplicationId,
            request.Request.CounterSessionId,
            session.OpenedByUserId,         // reception handed over
            request.CollectedByUserId,       // admin/doctor collected
            request.CollectedByRoleName,
            request.Request.Amount,
            request.Request.Notes);

        await handoverRepository.AddAsync(handover, cancellationToken);
        handover.Accept();  // immediately accepted since admin/doctor initiated
        await handoverRepository.SaveChangesAsync(cancellationToken);
        return handover.Id;
    }
}
