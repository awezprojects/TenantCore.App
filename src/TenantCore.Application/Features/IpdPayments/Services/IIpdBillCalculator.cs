using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.IpdPayments.Services;

public sealed record IpdBillCalculationResult(decimal RoomChargesTotal, IReadOnlyList<IpdBillLineItemDto> LineItems, decimal WriteOffTotal);

// Shared by GetIpdProvisionalBillQuery (as-of now, never persisted) and FinalizeIpdBillHandler
// (as-of DischargeDate, persisted) — one calculation, so the two can only ever differ by the
// stay continuing, never by a math discrepancy.
public interface IIpdBillCalculator
{
    Task<IpdBillCalculationResult> CalculateRoomChargesAsync(IpdRegistration registration, DateTime asOfUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// DEF-IPD-16: what one segment's own day would cost, standalone, under the clinic's mid-stay
    /// charge mode — the full rate under FullDayCharge regardless of elapsed time, or a share of
    /// it under Prorated, capped at a full day. Used both to validate a manual override's 0..cap
    /// range at shift time and as one side of the grace/full-day-rate-rule comparison. Pure and
    /// synchronous — it only needs the segment and the setting, no repository access.
    /// </summary>
    decimal CalculateStandardSegmentCharge(IpdBedTransferHistory segment, DateTime asOfUtc, MidStayBedChangeChargeMode midStayMode);

    /// <summary>Same computation as above, for a room that doesn't have a segment yet (e.g. the
    /// destination room being compared against at shift time, before the new segment is opened).</summary>
    decimal CalculateStandardCharge(decimal ratePerDay, DateTime occupiedFrom, DateTime asOfUtc, MidStayBedChangeChargeMode midStayMode);

    /// <summary>
    /// Resolves which of two same-day room rates a FullDayCharge block bills when neither grace
    /// nor a manual override applies — the clinic's IpdFullDayShiftRateRule setting.
    /// </summary>
    decimal ResolveFullDayRate(decimal originStandardCharge, decimal destinationStandardCharge, IpdFullDayShiftRateRule rule);
}
