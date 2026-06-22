using MediatR;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class AcceptAmountHandoverHandler(IAmountHandoverRepository repository) : IRequestHandler<AcceptAmountHandoverCommand, AmountHandoverDto>
{
    public async Task<AmountHandoverDto> Handle(AcceptAmountHandoverCommand request, CancellationToken cancellationToken)
    {
        var handover = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (handover is null || handover.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(AmountHandover), request.Id);

        if (handover.Status != HandoverStatus.Pending)
            throw new InvalidOperationException("Only pending handovers can be accepted.");

        handover.Accept();
        repository.Update(handover);
        await repository.SaveChangesAsync(cancellationToken);
        return AmountHandoverTranslator.ToDto(handover);
    }
}
