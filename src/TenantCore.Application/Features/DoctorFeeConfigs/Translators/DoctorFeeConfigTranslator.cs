using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Translators;

public static class DoctorFeeConfigTranslator
{
    public static DoctorFeeConfig ToEntity(CreateDoctorFeeConfigCommand command)
        => DoctorFeeConfig.Create(command.ApplicationId, command.Request.DoctorProfileId, command.Request.VisitFee);

    public static DoctorFeeConfigDto ToDto(DoctorFeeConfig entity, string doctorName = "") => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        DoctorProfileId = entity.DoctorProfileId,
        DoctorName = doctorName,
        VisitFee = entity.VisitFee,
        IsActive = entity.IsActive
    };

    public static DoctorFeeConfigSummaryDto ToSummaryDto(DoctorFeeConfig entity, string doctorName = "") => new()
    {
        Id = entity.Id,
        DoctorProfileId = entity.DoctorProfileId,
        DoctorName = doctorName,
        VisitFee = entity.VisitFee,
        IsActive = entity.IsActive
    };
}
