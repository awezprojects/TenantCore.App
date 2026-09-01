using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Translators;

public static class ClinicFeatureFlagsTranslator
{
    public static ClinicFeatureFlagsDto ToDto(ClinicFeatureFlags entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        PrepaidOpdEnabled = entity.PrepaidOpdEnabled,
        UpdatedAt = entity.UpdatedAt
    };
}
