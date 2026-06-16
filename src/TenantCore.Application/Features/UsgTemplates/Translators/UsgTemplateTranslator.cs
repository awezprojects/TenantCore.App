using TenantCore.Application.Features.Obstetrics.Helpers;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.UsgTemplates.Translators;

public static class UsgTemplateTranslator
{
    public static ClinicUsgTemplateDto ToDto(ClinicUsgTemplate template) => new()
    {
        ApplicationId = template.ApplicationId,
        IsCustomized  = template.IsCustomized,
        Rows          = template.Rows
            .OrderBy(r => r.RowOrder)
            .Select(RowToDto)
            .ToList(),
    };

    public static ClinicUsgTemplateDto ToDefaultDto(Guid applicationId) => new()
    {
        ApplicationId = applicationId,
        IsCustomized  = false,
        Rows          = DefaultUsgTemplateDefinition.Rows,
    };

    public static UsgTemplateRowDto RowToDto(UsgTemplateRow row) => new()
    {
        RowOrder      = row.RowOrder,
        WeekLabel     = row.WeekLabel,
        LmpDayOffset  = row.LmpDayOffset,
        Activity      = row.Activity,
        Indication    = row.Indication,
    };

    public static UsgTemplateRow RowFromDto(UsgTemplateRowDto dto, Guid templateId) => new()
    {
        ClinicUsgTemplateId = templateId,
        RowOrder            = dto.RowOrder,
        WeekLabel           = dto.WeekLabel,
        LmpDayOffset        = dto.LmpDayOffset,
        Activity            = dto.Activity,
        Indication          = dto.Indication,
    };
}
