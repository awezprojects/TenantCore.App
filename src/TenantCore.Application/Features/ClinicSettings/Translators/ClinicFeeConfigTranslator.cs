using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Translators;

public static class ClinicFeeConfigTranslator
{
    public static ClinicFeeConfigDto ToDto(ClinicFeeConfig entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        OpdFee = entity.OpdFee,
        UpdatedAt = entity.UpdatedAt
    };
}
