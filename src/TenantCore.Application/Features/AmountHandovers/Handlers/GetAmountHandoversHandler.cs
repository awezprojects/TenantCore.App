using MediatR;
using TenantCore.Application.Features.AmountHandovers.Queries;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Handlers;

public sealed class GetAmountHandoversHandler(IAmountHandoverRepository repository)
    : IRequestHandler<GetAmountHandoversQuery, IEnumerable<AmountHandoverSummaryDto>>
{
    public async Task<IEnumerable<AmountHandoverSummaryDto>> Handle(GetAmountHandoversQuery request, CancellationToken cancellationToken)
    {
        var offsetMinutes = request.UtcOffsetMinutes;
        DateTime? fromUtc = request.From.HasValue
            ? request.From.Value.Date.AddMinutes(-offsetMinutes)
            : null;
        DateTime? toUtc = request.To.HasValue
            ? request.To.Value.Date.AddDays(1).AddMinutes(-offsetMinutes)
            : null;

        var handovers = await repository.GetByDateRangeAsync(request.ApplicationId, fromUtc, toUtc, cancellationToken);
        return handovers.Select(AmountHandoverTranslator.ToSummaryDto);
    }
}
