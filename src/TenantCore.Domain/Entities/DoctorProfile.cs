using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class DoctorProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string RegistrationNumber { get; private set; } = string.Empty;
    public bool IsRegistrationVerified { get; private set; }
    public Guid? SpecialityId { get; private set; }
    public string? QualificationDetails { get; private set; }

    // Navigation — loaded explicitly; may be null when not included
    public DoctorSpeciality? Speciality { get; private set; }

    // Legacy free-text field kept for display fallback only
    public string? Specialty { get; private set; }

    private DoctorProfile() { }

    public static DoctorProfile Create(
        Guid userId,
        string registrationNumber,
        Guid specialityId,
        string? qualificationDetails) => new()
    {
        Id                     = Guid.NewGuid(),
        UserId                 = userId,
        RegistrationNumber     = registrationNumber,
        IsRegistrationVerified = true,
        SpecialityId           = specialityId,
        QualificationDetails   = qualificationDetails,
        CreatedAt              = DateTime.UtcNow,
    };

    public void Update(
        string registrationNumber,
        Guid specialityId,
        string? qualificationDetails)
    {
        RegistrationNumber   = registrationNumber;
        SpecialityId         = specialityId;
        QualificationDetails = qualificationDetails;
        SetUpdatedAt();
    }

    public void MarkVerified()
    {
        IsRegistrationVerified = true;
        SetUpdatedAt();
    }
}
