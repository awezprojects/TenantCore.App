using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class OpdRegistration : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorUserId { get; private set; }
    public string DoctorName { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;
    public DateTime RegistrationDate { get; private set; }
    public decimal Fee { get; private set; }
    public OpdStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public decimal? Weight { get; private set; }
    public string? BloodPressure { get; private set; }
    public int? PulseRate { get; private set; }
    public decimal? OxygenSaturation { get; private set; }

    public Patient Patient { get; private set; } = null!;

    private OpdRegistration() { }

    public static OpdRegistration Create(
        Guid applicationId,
        Guid patientId,
        Guid doctorUserId,
        string doctorName,
        string registrationNumber,
        decimal fee,
        string? notes,
        decimal? weight = null,
        string? bloodPressure = null,
        int? pulseRate = null,
        decimal? oxygenSaturation = null) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        PatientId = patientId,
        DoctorUserId = doctorUserId,
        DoctorName = doctorName,
        RegistrationNumber = registrationNumber,
        RegistrationDate = DateTime.UtcNow,
        Fee = fee,
        Status = OpdStatus.Pending,
        Notes = notes,
        Weight = weight,
        BloodPressure = bloodPressure,
        PulseRate = pulseRate,
        OxygenSaturation = oxygenSaturation,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(Guid doctorUserId, string doctorName, decimal fee, OpdStatus status, string? notes,
        decimal? weight = null, string? bloodPressure = null, int? pulseRate = null, decimal? oxygenSaturation = null)
    {
        if (Status is OpdStatus.Cancelled or OpdStatus.Completed)
            throw new DomainValidationException("Cannot update a completed or cancelled OPD registration.");

        DoctorUserId = doctorUserId;
        DoctorName = doctorName;
        Fee = fee;
        Status = status;
        Notes = notes;
        Weight = weight;
        BloodPressure = bloodPressure;
        PulseRate = pulseRate;
        OxygenSaturation = oxygenSaturation;
        SetUpdatedAt();
    }
}
