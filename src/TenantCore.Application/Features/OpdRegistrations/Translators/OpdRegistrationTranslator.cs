using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Translators;

public static class OpdRegistrationTranslator
{
    public static OpdRegistrationDto ToDto(OpdRegistration entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        PatientId = entity.PatientId,
        PatientName = entity.Patient is not null
            ? $"{entity.Patient.FirstName} {entity.Patient.LastName}"
            : string.Empty,
        DoctorUserId = entity.DoctorUserId,
        DoctorName = entity.DoctorName,
        RegistrationNumber = entity.RegistrationNumber,
        RegistrationDate = entity.RegistrationDate,
        Fee = entity.Fee,
        Status = entity.Status,
        Notes = entity.Notes,
        Weight = entity.Weight,
        BloodPressure = entity.BloodPressure,
        PulseRate = entity.PulseRate,
        OxygenSaturation = entity.OxygenSaturation,
        CreatedAt = entity.CreatedAt
    };
}
