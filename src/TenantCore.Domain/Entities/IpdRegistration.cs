using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class IpdRegistration : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorUserId { get; private set; }
    public string DoctorName { get; private set; } = string.Empty;
    public string AdmissionNumber { get; private set; } = string.Empty;
    public DateTime AdmissionDate { get; private set; }
    public DateTime? DischargeDate { get; private set; }
    public string? WardName { get; private set; }
    public string? RoomNumber { get; private set; }
    public string? BedNumber { get; private set; }
    public decimal InitialFee { get; private set; }
    public IpdStatus Status { get; private set; }
    public string? AdmissionNotes { get; private set; }
    public string? DischargeNotes { get; private set; }

    public Patient Patient { get; private set; } = null!;

    private IpdRegistration() { }

    public static IpdRegistration Create(
        Guid applicationId,
        Guid patientId,
        Guid doctorUserId,
        string doctorName,
        string admissionNumber,
        string? wardName,
        string? roomNumber,
        string? bedNumber,
        decimal initialFee,
        string? admissionNotes) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        PatientId = patientId,
        DoctorUserId = doctorUserId,
        DoctorName = doctorName,
        AdmissionNumber = admissionNumber,
        AdmissionDate = DateTime.UtcNow,
        WardName = wardName,
        RoomNumber = roomNumber,
        BedNumber = bedNumber,
        InitialFee = initialFee,
        Status = IpdStatus.Admitted,
        AdmissionNotes = admissionNotes,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(
        Guid doctorUserId,
        string doctorName,
        string? wardName,
        string? roomNumber,
        string? bedNumber,
        IpdStatus status,
        string? admissionNotes)
    {
        if (Status is IpdStatus.Discharged or IpdStatus.Cancelled)
            throw new DomainValidationException("Cannot update a discharged or cancelled IPD registration.");

        DoctorUserId = doctorUserId;
        DoctorName = doctorName;
        WardName = wardName;
        RoomNumber = roomNumber;
        BedNumber = bedNumber;
        Status = status;
        AdmissionNotes = admissionNotes;
        SetUpdatedAt();
    }

    public void Discharge(string? dischargeNotes)
    {
        if (Status != IpdStatus.Admitted)
            throw new DomainValidationException("Patient must be in Admitted status to discharge.");

        Status = IpdStatus.Discharged;
        DischargeDate = DateTime.UtcNow;
        DischargeNotes = dischargeNotes;
        SetUpdatedAt();
    }
}
