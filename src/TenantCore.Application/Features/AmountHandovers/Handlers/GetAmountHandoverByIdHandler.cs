using MediatR;
using TenantCore.Application.Features.AmountHandovers.Queries;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class GetAmountHandoverByIdHandler(IAmountHandoverRepository repository) : IRequestHandler<GetAmountHandoverByIdQuery, AmountHandoverDto>
{
    public async Task<AmountHandoverDto> Handle(GetAmountHandoverByIdQuery request, CancellationToken cancellationToken)
    {
        var handover = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (handover is null || handover.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(AmountHandover), request.Id);

        return AmountHandoverTranslator.ToDto(handover);
    }
}
