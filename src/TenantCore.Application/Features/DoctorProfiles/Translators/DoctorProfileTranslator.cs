using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorProfiles.Translators;

public static class DoctorProfileTranslator
{
    public static DoctorProfileDto ToDto(DoctorProfile entity) => new()
    {
        Id                     = entity.Id,
        UserId                 = entity.UserId,
        RegistrationNumber     = entity.RegistrationNumber,
        IsRegistrationVerified = entity.IsRegistrationVerified,
        Specialty              = entity.Specialty,
        QualificationDetails   = entity.QualificationDetails,
        CreatedAt              = entity.CreatedAt,
        UpdatedAt              = entity.UpdatedAt,
    };
}
