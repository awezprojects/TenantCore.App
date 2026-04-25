using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Translators;

public static class PatientTranslator
{
    public static PatientDto ToDto(Patient entity, bool showFullAadhaar = false) => new()
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
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    private static string? MaskAadhaar(string? aadhaar, bool showFull) =>
        aadhaar is null ? null
        : showFull ? aadhaar
        : "XXXX-XXXX-" + aadhaar[^4..];
}
