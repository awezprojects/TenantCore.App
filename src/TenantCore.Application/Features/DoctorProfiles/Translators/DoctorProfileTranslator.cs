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
        SpecialityId           = entity.SpecialityId,
        SpecialityName         = entity.Speciality?.Name,
        QualificationDetails   = entity.QualificationDetails,
        CreatedAt              = entity.CreatedAt,
        UpdatedAt              = entity.UpdatedAt,
    };

    // Overload used by upsert handler when navigation isn't attached on the entity
    public static DoctorProfileDto ToDto(DoctorProfile entity, DoctorSpeciality speciality) => new()
    {
        Id                     = entity.Id,
        UserId                 = entity.UserId,
        RegistrationNumber     = entity.RegistrationNumber,
        IsRegistrationVerified = entity.IsRegistrationVerified,
        SpecialityId           = entity.SpecialityId,
        SpecialityName         = speciality.Name,
        QualificationDetails   = entity.QualificationDetails,
        CreatedAt              = entity.CreatedAt,
        UpdatedAt              = entity.UpdatedAt,
    };
}
