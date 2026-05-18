using TenantCore.Shared.Dtos;
using Config = TenantCore.Domain.Entities.PrescriptionConfig;

namespace TenantCore.Application.Features.PrescriptionConfig.Translators;

public static class PrescriptionConfigTranslator
{
    public static PrescriptionConfigDto ToDto(Config config) => new()
    {
        ApplicationId = config.ApplicationId,
        DefaultLanguage = config.DefaultLanguage
    };
}
