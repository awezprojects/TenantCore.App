using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdReports.Handlers;

public sealed class GetIpdCensusReportHandler(
    IBedRepository bedRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdCensusReportQuery, IpdCensusReportDto>
{
    public async Task<IpdCensusReportDto> Handle(GetIpdCensusReportQuery request, CancellationToken cancellationToken)
    {
        if (!accessValidator.CanAccess(request.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var beds = (await bedRepository.GetByApplicationAsync(request.ApplicationId, cancellationToken))
            .Where(b => b.IsActive)
            .ToList();

        var wards = beds
            .GroupBy(b => (b.WardId, b.Ward.Name))
            .Select(g => new IpdCensusWardRowDto(
                g.Key.WardId, g.Key.Name, g.Count(), g.Count(b => b.IsOccupied), g.Count(b => !b.IsOccupied)))
            .OrderBy(w => w.WardName)
            .ToList();

        return new IpdCensusReportDto(
            beds.Count, beds.Count(b => b.IsOccupied), beds.Count(b => !b.IsOccupied), wards);
    }
}
