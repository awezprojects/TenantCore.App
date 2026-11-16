using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public sealed record IpdDepositDto(
    Guid Id,
    Guid IpdRegistrationId,
    decimal Amount,
    Guid CollectedByUserId,
    DateTime CollectedAt,
    Guid? CounterSessionId,
    string? Notes);

public sealed record CollectIpdDepositRequest(decimal Amount, string? Notes);

public sealed record IpdParticularDto(
    Guid Id,
    Guid IpdRegistrationId,
    Guid ParticularId,
    string ParticularName,
    decimal Amount,
    Guid AddedByUserId,
    DateTime AddedAt);

public sealed record AddIpdParticularRequest(Guid ParticularId, decimal Amount);

public sealed record ApplyIpdDiscountRequest(decimal Discount);

// DEF-IPD-16 (13-Nov): StandardAmount/AppliedAmount/AdjustmentType/Reason let the bill show a
// grace- or manual-adjusted day differently from a plain one — Amount always equals AppliedAmount
// (what was actually billed); the other three are null-equivalent (AdjustmentType.None, no reason)
// on a plain, unadjusted line item, so existing callers that only read Amount see no change.
public sealed record IpdBillLineItemDto(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string WardName,
    string RoomNumber,
    string BedNumber,
    decimal Amount,
    decimal StandardAmount,
    decimal AppliedAmount,
    IpdBillAdjustmentType AdjustmentType,
    string? Reason);

// IsProvisional is always true — this DTO only ever represents a live, never-persisted
// calculation ("amount payable if discharged today"). See IpdFinalBillDto for the locked bill.
public sealed record IpdProvisionalBillDto(
    bool IsProvisional,
    Guid IpdRegistrationId,
    decimal InitialFee,
    decimal RoomChargesTotal,
    IReadOnlyList<IpdBillLineItemDto> RoomChargeLineItems,
    decimal ParticularsTotal,
    decimal TotalAmount,
    decimal Discount,
    decimal FinalAmount,
    decimal TotalDeposited,
    decimal BalanceDue,
    DateTime AsOfUtc,
    decimal WriteOffTotal = 0m);

// DEF-IPD-22 (14-Nov): WasCancelled lets the UI/print page tell a real discharge's locked bill
// apart from a cancelled admission's forced-₹0 bill — without it, a cancelled stay's bill showed
// "FINAL BILL — Locked at discharge" next to a non-zero InitialFee/RoomChargesTotal/
// RoomChargeLineItems (the raw, un-waived figures the calculator computed from real occupancy
// history) above a ₹0.00 Total, which reads as the numbers not adding up. InitialFee/
// RoomChargesTotal/RoomChargeLineItems are deliberately left as the real, un-waived figures here
// (not zeroed) — they are what a human needs to see to understand *what* was waived, not just
// that something was.
public sealed record IpdFinalBillDto(
    Guid IpdRegistrationId,
    decimal InitialFee,
    decimal RoomChargesTotal,
    IReadOnlyList<IpdBillLineItemDto> RoomChargeLineItems,
    decimal ParticularsTotal,
    decimal TotalAmount,
    decimal Discount,
    decimal FinalAmount,
    decimal TotalDeposited,
    decimal BalanceDue,
    DateTime FinalizedAt,
    PaymentStatus PaymentStatus,
    decimal RefundDue,
    RefundStatus RefundStatus,
    DateTime? SettledAt,
    DateTime? RefundedAt,
    decimal WriteOffTotal = 0m,
    bool WasCancelled = false);

public sealed record IpdCensusWardRowDto(Guid WardId, string WardName, int TotalBeds, int OccupiedBeds, int AvailableBeds);

public sealed record IpdCensusReportDto(int TotalBeds, int OccupiedBeds, int AvailableBeds, IReadOnlyList<IpdCensusWardRowDto> Wards);

public sealed record IpdRevenueReportDto(decimal TotalDeposits, decimal TotalBalanceCollected, decimal TotalRefunded, decimal NetRevenue);

public sealed record IpdDischargeSummaryRowDto(
    Guid IpdRegistrationId,
    string AdmissionNumber,
    string PatientName,
    string PatientPhoneNumber,
    DateTime AdmissionDate,
    DateTime DischargeDate,
    decimal? FinalAmount,
    decimal? BalanceDue,
    bool Settled,
    // OBS-8: a discharge from before this feature shipped has no IpdPayment row at all — that
    // is not the same thing as "Outstanding" (a real bill with money still owed), so the UI
    // needs to tell the two apart instead of showing a misleading status for pre-feature data.
    bool HasBill);

public sealed record IpdDischargeSummaryReportDto(IReadOnlyList<IpdDischargeSummaryRowDto> Discharges);
