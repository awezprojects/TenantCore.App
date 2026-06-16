using TenantCore.Shared.Dtos;
using Config = TenantCore.Domain.Entities.PrescriptionConfig;

namespace TenantCore.Application.Features.PrescriptionConfig.Translators;

public static class PrescriptionConfigTranslator
{
    public static PrescriptionConfigDto ToDto(Config config) => new()
    {
        ApplicationId = config.ApplicationId,
        DefaultLanguage = config.DefaultLanguage,
        PrintMarginTop = config.PrintMarginTop,
        PrintMarginRight = config.PrintMarginRight,
        PrintMarginBottom = config.PrintMarginBottom,
        PrintMarginLeft = config.PrintMarginLeft,
        HideClinicHeader = config.HideClinicHeader
    };
}
