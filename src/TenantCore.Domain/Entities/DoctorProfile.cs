using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class DoctorProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string RegistrationNumber { get; private set; } = string.Empty;
    public bool IsRegistrationVerified { get; private set; }
    public string? Specialty { get; private set; }
    public string? QualificationDetails { get; private set; }

    private DoctorProfile() { }

    public static DoctorProfile Create(
        Guid userId,
        string registrationNumber,
        string? specialty,
        string? qualificationDetails) => new()
    {
        Id                    = Guid.NewGuid(),
        UserId                = userId,
        RegistrationNumber    = registrationNumber,
        IsRegistrationVerified = true,
        Specialty             = specialty,
        QualificationDetails  = qualificationDetails,
        CreatedAt             = DateTime.UtcNow,
    };

    public void Update(
        string registrationNumber,
        string? specialty,
        string? qualificationDetails)
    {
        RegistrationNumber   = registrationNumber;
        Specialty            = specialty;
        QualificationDetails = qualificationDetails;
        SetUpdatedAt();
    }

    public void MarkVerified()
    {
        IsRegistrationVerified = true;
        SetUpdatedAt();
    }
}
