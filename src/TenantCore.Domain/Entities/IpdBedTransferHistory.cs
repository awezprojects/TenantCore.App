using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

// One row per bed-occupancy segment for an IPD admission. Opened on admit, closed + a new
// row opened on every TransferBed, closed (no new row) on discharge/cancel. Backbone for the
// room-charge calculator (Phase 2) and for the DEF-IPD-01/02/03 bed-shift fix: occupancy
// moves through this history, not through free-text strings on IpdRegistration.
public class IpdBedTransferHistory : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid IpdRegistrationId { get; private set; }
    public Guid BedId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid WardId { get; private set; }

    // Snapshot of Room.PricePerDay at the moment this segment opened, so a later room-price
    // change never rewrites a past stay's bill.
    public decimal RatePerDayAtTime { get; private set; }

    public DateTime OccupiedFrom { get; private set; }
    public DateTime? OccupiedTo { get; private set; }
    public Guid TransferredByUserId { get; private set; }
    public string? Notes { get; private set; }

    // DEF-IPD-16 (13-Nov): what this segment's room charge actually came out to, frozen at the
    // moment a shift closes it — set by FreezeCharge, called only from TransferBedHandler
    // alongside Close(), since a write-off (standard vs. applied) only exists when a shift is
    // what ended the segment. A segment closed by discharge/cancel instead, or one closed before
    // this shipped, has no frozen charge — IpdBillCalculator falls back to its own live
    // per-block computation for those, same as it always has.
    public decimal? StandardChargeAmount { get; private set; }
    public decimal? AppliedChargeAmount { get; private set; }
    public IpdBillAdjustmentType? AdjustmentType { get; private set; }
    public decimal? ManualChargeAmount { get; private set; }
    public string? ManualChargeReason { get; private set; }
    public Guid? ManualChargeByUserId { get; private set; }
    public DateTime? ManualChargeAt { get; private set; }

    public decimal? WriteOffAmount => StandardChargeAmount is null || AppliedChargeAmount is null
        ? null
        : StandardChargeAmount - AppliedChargeAmount;

    public Bed Bed { get; private set; } = null!;
    public Room Room { get; private set; } = null!;
    public Ward Ward { get; private set; } = null!;

    private IpdBedTransferHistory() { }

    public static IpdBedTransferHistory Open(
        Guid applicationId,
        Guid ipdRegistrationId,
        Guid bedId,
        Guid roomId,
        Guid wardId,
        decimal ratePerDayAtTime,
        Guid transferredByUserId,
        string? notes) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        IpdRegistrationId = ipdRegistrationId,
        BedId = bedId,
        RoomId = roomId,
        WardId = wardId,
        RatePerDayAtTime = ratePerDayAtTime,
        OccupiedFrom = DateTime.UtcNow,
        TransferredByUserId = transferredByUserId,
        Notes = notes,
        CreatedAt = DateTime.UtcNow
    };

    public void Close()
    {
        OccupiedTo = DateTime.UtcNow;
        SetUpdatedAt();
    }

    /// <summary>
    /// Records a manual override amount for the room being left, entered at shift time. The
    /// caller (TransferBedHandler) has already validated the 0 ≤ amount ≤ standardChargeAmount
    /// cap and that a reason is present — this just stores who/when/what was overridden.
    /// </summary>
    public void SetManualCharge(decimal amount, string reason, Guid byUserId)
    {
        ManualChargeAmount = amount;
        ManualChargeReason = reason;
        ManualChargeByUserId = byUserId;
        ManualChargeAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    /// <summary>
    /// Freezes what this segment's room charge actually came out to, at the moment it closes —
    /// the standard (policy-only) amount, what was actually applied (after grace/manual), and why
    /// they differ. Called alongside Close(); kept separate so a caller that only needs occupancy
    /// freed (e.g. a path that doesn't yet know the charge) is never forced to supply one.
    /// </summary>
    public void FreezeCharge(decimal standardChargeAmount, decimal appliedChargeAmount, IpdBillAdjustmentType adjustmentType)
    {
        StandardChargeAmount = standardChargeAmount;
        AppliedChargeAmount = appliedChargeAmount;
        AdjustmentType = adjustmentType;
        SetUpdatedAt();
    }
}
