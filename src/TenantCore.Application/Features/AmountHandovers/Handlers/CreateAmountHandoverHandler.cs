using MediatR;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class CreateAmountHandoverHandler(
    IAmountHandoverRepository handoverRepository,
    ICounterSessionRepository sessionRepository) : IRequestHandler<CreateAmountHandoverCommand, Guid>
{
    public async Task<Guid> Handle(CreateAmountHandoverCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.Request.CounterSessionId, cancellationToken);
        if (session is null || session.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(CounterSession), request.Request.CounterSessionId);

        var handover = AmountHandoverTranslator.ToEntity(request);
        await handoverRepository.AddAsync(handover, cancellationToken);
        await handoverRepository.SaveChangesAsync(cancellationToken);
        return handover.Id;
    }
}
