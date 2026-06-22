using MediatR;
using TenantCore.Application.Features.AmountHandovers.Queries;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class GetAmountHandoversBySessionHandler(IAmountHandoverRepository repository) : IRequestHandler<GetAmountHandoversBySessionQuery, IEnumerable<AmountHandoverSummaryDto>>
{
    public async Task<IEnumerable<AmountHandoverSummaryDto>> Handle(GetAmountHandoversBySessionQuery request, CancellationToken cancellationToken)
    {
        var handovers = await repository.GetBySessionIdAsync(request.CounterSessionId, request.ApplicationId, cancellationToken);
        return handovers.Select(AmountHandoverTranslator.ToSummaryDto);
    }
}
