using MediatR;
using TenantCore.Application.Features.Obstetrics.Helpers;
using TenantCore.Application.Features.Obstetrics.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Handlers;

public sealed class GetUsgChartByPatientHandler(
    IObstetricPrescriptionDataRepository obstetricRepository,
    IClinicUsgTemplateRepository usgTemplateRepository)
    : IRequestHandler<GetUsgChartByPatientQuery, UsgChartDto>
{
    public async Task<UsgChartDto> Handle(GetUsgChartByPatientQuery request, CancellationToken cancellationToken)
    {
        var obstetricData = await obstetricRepository.GetMostRecentWithLmpByPatientIdAsync(
            request.PatientId, request.ApplicationId, cancellationToken);

        if (obstetricData is null || obstetricData.Lmp is null)
            return new UsgChartDto { PatientId = request.PatientId };

        var template = await usgTemplateRepository.GetByApplicationIdWithRowsAsync(request.ApplicationId, cancellationToken);

        IReadOnlyList<UsgTemplateRowDto> templateRows = template is { IsCustomized: true }
            ? template.Rows.OrderBy(r => r.RowOrder)
                           .Select(r => new UsgTemplateRowDto
                           {
                               RowOrder     = r.RowOrder,
                               WeekLabel    = r.WeekLabel,
                               LmpDayOffset = r.LmpDayOffset,
                               Activity     = r.Activity,
                               Indication   = r.Indication,
                           })
                           .ToList()
            : DefaultUsgTemplateDefinition.Rows;

        var rows = UsgDateCalculator.CalculateSchedule(obstetricData.Lmp.Value, templateRows);

        return new UsgChartDto
        {
            PatientId  = request.PatientId,
            Lmp        = obstetricData.Lmp,
            EddByLmp   = obstetricData.EddByLmp,
            EddByUsg   = obstetricData.EddByUsg,
            Rows       = rows,
        };
    }
}
