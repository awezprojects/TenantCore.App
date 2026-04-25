using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Translators;

public static class IpdRegistrationTranslator
{
    public static IpdRegistrationDto ToDto(IpdRegistration entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        PatientId = entity.PatientId,
        PatientName = entity.Patient is not null
            ? $"{entity.Patient.FirstName} {entity.Patient.LastName}"
            : string.Empty,
        DoctorUserId = entity.DoctorUserId,
        DoctorName = entity.DoctorName,
        AdmissionNumber = entity.AdmissionNumber,
        AdmissionDate = entity.AdmissionDate,
        DischargeDate = entity.DischargeDate,
        WardName = entity.WardName,
        RoomNumber = entity.RoomNumber,
        BedNumber = entity.BedNumber,
        InitialFee = entity.InitialFee,
        Status = entity.Status,
        AdmissionNotes = entity.AdmissionNotes,
        DischargeNotes = entity.DischargeNotes,
        CreatedAt = entity.CreatedAt
    };
}
