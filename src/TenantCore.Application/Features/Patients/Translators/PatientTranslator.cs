using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Translators;

public static class PatientTranslator
{
    public static PatientDto ToDto(Patient entity, bool showFullAadhaar = false,
        bool hasLmpRecord = false, bool hasActiveTenure = false) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        DateOfBirth = entity.DateOfBirth,
        Gender = entity.Gender,
        PhoneNumber = entity.PhoneNumber,
        Email = entity.Email,
        AadhaarNumber = MaskAadhaar(entity.AadhaarNumber, showFullAadhaar),
        PhotoUrl = entity.PhotoUrl,
        Address = entity.Address,
        BloodGroup = entity.BloodGroup,
        EmergencyContactName = entity.EmergencyContactName,
        EmergencyContactPhone = entity.EmergencyContactPhone,
        KnownAllergies = entity.KnownAllergies,
        MedicalHistory = entity.MedicalHistory,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        HasLmpRecord = hasLmpRecord,
        HasActiveTenure = hasActiveTenure
    };

    private static string? MaskAadhaar(string? aadhaar, bool showFull) =>
        aadhaar is null ? null
        : showFull ? aadhaar
        : "XXXX-XXXX-" + aadhaar[^4..];
}
