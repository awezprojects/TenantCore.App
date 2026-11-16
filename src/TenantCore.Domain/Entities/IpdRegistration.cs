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
    // Source of truth for occupancy going forward (see DEF-IPD-01/02/03 in the IPD E2E test
    // report). WardName/RoomNumber/BedNumber below are now a denormalized DISPLAY SNAPSHOT
    // only, written by the server from the resolved Bed/Room/Ward — never free text again.
    public Guid? CurrentBedId { get; private set; }
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
        Guid? currentBedId,
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
        CurrentBedId = currentBedId,
        WardName = wardName,
        RoomNumber = roomNumber,
        BedNumber = bedNumber,
        InitialFee = initialFee,
        Status = IpdStatus.Admitted,
        AdmissionNotes = admissionNotes,
        CreatedAt = DateTime.UtcNow
    };

    // Status and bed no longer move through here (DEF-IPD-04) — see TransferBedHandler for bed
    // changes and Discharge/Cancel below for the only two terminal status transitions.
    public void Update(Guid doctorUserId, string doctorName, string? admissionNotes)
    {
        if (Status is IpdStatus.Discharged or IpdStatus.Cancelled)
            throw new DomainValidationException("Cannot update a discharged or cancelled IPD registration.");

        DoctorUserId = doctorUserId;
        DoctorName = doctorName;
        AdmissionNotes = admissionNotes;
        SetUpdatedAt();
    }

    // Called only by TransferBedHandler, after it has already validated the target bed and
    // moved occupancy on the Bed entities themselves. This just updates the registration's
    // pointer + display snapshot — it never touches Status (bed transfers are not a status).
    public void RecordBedTransfer(Guid newBedId, string wardName, string roomNumber, string bedNumber)
    {
        if (Status != IpdStatus.Admitted)
            throw new DomainValidationException("Patient must be in Admitted status to transfer beds.");

        CurrentBedId = newBedId;
        WardName = wardName;
        RoomNumber = roomNumber;
        BedNumber = bedNumber;
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

    // DEF-IPD-19/25 (15-Nov): a cancelled admission bills ₹0 and refunds everything in full — see
    // CancelIpdRegistrationHandler/IpdPayment.FinalizeAtCancellation. cancelWindowMinutes (0 =
    // unlimited, the clinic's default — preserves pre-existing behavior for any clinic that never
    // touches the new setting) caps how long after admission that free cancellation stays
    // available, so a stay that has genuinely consumed room time/fee can't be waved away as a
    // "mistake" indefinitely — past the window, Cancel is refused outright and Discharge (which
    // bills for real) is the only path.
    public void Cancel(string? reason, int cancelWindowMinutes = 0, DateTime? asOfUtc = null)
    {
        if (Status != IpdStatus.Admitted)
            throw new DomainValidationException("Only an Admitted registration can be cancelled.");

        if (cancelWindowMinutes > 0)
        {
            var now = asOfUtc ?? DateTime.UtcNow;
            if (now - AdmissionDate > TimeSpan.FromMinutes(cancelWindowMinutes))
            {
                throw new DomainValidationException(
                    $"This admission can no longer be cancelled — it was admitted more than {cancelWindowMinutes} minute(s) ago. Discharge the patient instead to settle the actual bill.");
            }
        }

        Status = IpdStatus.Cancelled;
        DischargeNotes = reason;
        SetUpdatedAt();
    }
}
