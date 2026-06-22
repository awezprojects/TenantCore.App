using MediatR;
using TenantCore.Application.Features.AmountHandovers.Queries;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class GetPendingAmountHandoversHandler(IAmountHandoverRepository repository) : IRequestHandler<GetPendingAmountHandoversQuery, IEnumerable<AmountHandoverSummaryDto>>
{
    public async Task<IEnumerable<AmountHandoverSummaryDto>> Handle(GetPendingAmountHandoversQuery request, CancellationToken cancellationToken)
    {
        var handovers = await repository.GetPendingAsync(request.ApplicationId, cancellationToken);
        return handovers.Select(AmountHandoverTranslator.ToSummaryDto);
    }
}
