using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PregnancyTenures.Queries;
using TenantCore.Application.Features.PregnancyTenures.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Handlers;

public sealed class GetOverdueEddPatientsHandler(
    IPregnancyTenureRepository repository,
    ILogger<GetOverdueEddPatientsHandler> logger)
    : IRequestHandler<GetOverdueEddPatientsQuery, IEnumerable<PregnancyTenureSummaryDto>>
{
    public async Task<IEnumerable<PregnancyTenureSummaryDto>> Handle(GetOverdueEddPatientsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting overdue EDD patients for application {ApplicationId}", request.ApplicationId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tenures = await repository.GetAllOverdueAsync(request.ApplicationId, today, cancellationToken);

        return tenures.Select(PregnancyTenureTranslator.ToSummaryDto);
    }
}
