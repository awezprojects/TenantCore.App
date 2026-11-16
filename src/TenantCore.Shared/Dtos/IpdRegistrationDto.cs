using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class IpdRegistrationDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid DoctorUserId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string AdmissionNumber { get; init; } = string.Empty;
    public DateTime AdmissionDate { get; init; }
    public DateTime? DischargeDate { get; init; }
    public Guid? CurrentBedId { get; init; }
    public string? WardName { get; init; }
    public string? RoomNumber { get; init; }
    public string? BedNumber { get; init; }
    public decimal InitialFee { get; init; }
    public IpdStatus Status { get; init; }
    public string? AdmissionNotes { get; init; }
    public string? DischargeNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateIpdRegistrationDto(
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    Guid? BedId,
    decimal InitialFee,
    string? AdmissionNotes);

// Status and bed are no longer settable through Update — they move through their own
// dedicated actions (Discharge, TransferBed, Cancel) so no open PUT can leave an admission
// in a state the API never otherwise allows (see DEF-IPD-04 in the IPD E2E test report).
public sealed record UpdateIpdRegistrationDto(
    Guid DoctorUserId,
    string DoctorName,
    string? AdmissionNotes);

public sealed record DischargePatientDto(string? DischargeNotes);

public sealed record TransferBedRequest(Guid NewBedId, string? Notes, decimal? ManualChargeAmount = null, string? ManualChargeReason = null);

// DEF-IPD-16: lets the Shift dialog show real numbers (and the manual amount's cap) before the
// user commits, instead of discovering the validation error only after submitting.
public sealed record TransferBedPreviewDto(
    decimal StandardAmount,
    decimal SuggestedAmount,
    decimal MaxManualAmount,
    bool WithinGraceWindow,
    bool ManualChargeAllowed);

public sealed record CancelIpdRegistrationRequest(string? Reason);

// OBS-25 (14-Nov QA): the bill endpoints already exposed StandardAmount/AppliedAmount/
// AdjustmentType/write-off for a segment's frozen shift charge — bed-history did not, even though
// it is the natural place to audit "what happened on this shift" without pulling a whole bill.
// All five are null/None on a plain shift, a still-open segment, or one closed before this shipped.
public sealed record IpdBedTransferHistoryDto(
    Guid Id,
    Guid BedId,
    string WardName,
    string RoomNumber,
    string BedNumber,
    decimal RatePerDayAtTime,
    DateTime OccupiedFrom,
    DateTime? OccupiedTo,
    Guid TransferredByUserId,
    string? Notes,
    decimal? StandardChargeAmount = null,
    decimal? AppliedChargeAmount = null,
    IpdBillAdjustmentType? AdjustmentType = null,
    decimal? WriteOffAmount = null,
    string? ManualChargeReason = null);
