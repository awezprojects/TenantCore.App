using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.IpdPayments.Services;

public sealed class IpdBillCalculator(
    IIpdBedTransferHistoryRepository historyRepository,
    IClinicFeatureFlagsRepository featureFlagsRepository) : IIpdBillCalculator
{
    public async Task<IpdBillCalculationResult> CalculateRoomChargesAsync(
        IpdRegistration registration, DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        var segments = (await historyRepository.GetByRegistrationAsync(registration.Id, registration.ApplicationId, cancellationToken))
            .OrderBy(s => s.OccupiedFrom)
            .ToList();

        // No bed was ever allocated (ward-only admission) — no room charge, and no need to
        // require the clinic to have configured a billing policy it never uses.
        if (segments.Count == 0)
            return new IpdBillCalculationResult(0, [], 0);

        var flags = await featureFlagsRepository.GetByApplicationAsync(registration.ApplicationId, cancellationToken);
        if (flags?.IpdRoomBillingCycle is null || flags.IpdMidStayChargeMode is null)
            throw new InvalidOperationException(UserMessages.IpdBillingSettingsNotConfigured);

        var periodStart = segments[0].OccupiedFrom;
        if (asOfUtc <= periodStart)
            return new IpdBillCalculationResult(0, [], 0);

        // DEF-IPD-16 (13-Nov, "Period shows 05:30 IST"): PerCalendarDay blocks must align to the
        // clinic's LOCAL midnight, not UTC midnight. periodStart is a UTC instant; ToLocalTime()
        // treats an Unspecified-kind value as UTC (the same convention the bill print pages
        // already rely on for display), so periodStart.ToLocalTime().Date.ToUniversalTime() is
        // "local midnight, expressed back as the UTC instant it actually occurs at" — e.g. IST
        // midnight becomes 18:30 UTC the previous day, not 00:00 UTC. Per24HourBlock is unaffected
        // — it was always anchored to the admission instant itself, not a calendar boundary.
        var blockOrigin = flags.IpdRoomBillingCycle == RoomBillingCycle.PerCalendarDay
            ? periodStart.ToLocalTime().Date.ToUniversalTime()
            : periodStart;
        var blockLength = TimeSpan.FromDays(1);

        var lineItems = new List<IpdBillLineItemDto>();
        decimal total = 0;
        decimal writeOffTotal = 0;

        for (var blockStart = blockOrigin; blockStart < asOfUtc; blockStart += blockLength)
        {
            var blockEnd = blockStart + blockLength;
            var periodEnd = Min(blockEnd, asOfUtc);

            var overlapping = segments
                .Select(s => (Segment: s, OverlapStart: Max(s.OccupiedFrom, blockStart), OverlapEnd: Min(s.OccupiedTo ?? asOfUtc, blockEnd)))
                .Where(x => x.OverlapEnd > x.OverlapStart)
                .ToList();

            if (overlapping.Count == 0)
                continue;

            // DEF-IPD-16: a segment that closed (was shifted away from) inside this exact block,
            // carrying a frozen grace/manual override (see TransferBedHandler), settles the WHOLE
            // block by itself — the override already accounts for both rooms' time within it. The
            // segment that opened mid-block from that same shift is deliberately not charged again
            // for this block; its own billing resumes from the next one onward.
            var overridden = overlapping.FirstOrDefault(x =>
                x.Segment.OccupiedTo is { } closedAt && closedAt >= blockStart && closedAt < blockEnd &&
                x.Segment.AdjustmentType is not null and not IpdBillAdjustmentType.None);

            if (overridden.Segment is not null)
            {
                var seg = overridden.Segment;
                var applied = seg.AppliedChargeAmount ?? 0m;
                var standard = seg.StandardChargeAmount ?? applied;
                total += applied;
                writeOffTotal += standard - applied;
                lineItems.Add(new IpdBillLineItemDto(
                    blockStart, periodEnd, seg.Ward.Name, seg.Room.RoomNumber, seg.Bed.BedNumber, applied,
                    standard, applied, seg.AdjustmentType!.Value, seg.ManualChargeReason));
                continue;
            }

            if (overlapping.Count == 1 || flags.IpdMidStayChargeMode == MidStayBedChangeChargeMode.FullDayCharge)
            {
                // One room all block, or policy says charge one full block regardless of a
                // mid-block transfer. With no override in play, a true single-room day always
                // bills in full (unchanged); a multi-room day resolves via the clinic's rate rule
                // — DestinationRoomRate (the default) reproduces the old unconditional "last
                // segment wins" behavior exactly, for either shift direction.
                var chosen = overlapping[^1].Segment;
                var amount = overlapping.Count == 1
                    ? chosen.RatePerDayAtTime
                    : ResolveFullDayRate(overlapping[0].Segment.RatePerDayAtTime, chosen.RatePerDayAtTime, flags.IpdFullDayShiftRateRule);

                total += amount;
                lineItems.Add(new IpdBillLineItemDto(
                    blockStart, periodEnd, chosen.Ward.Name, chosen.Room.RoomNumber, chosen.Bed.BedNumber, amount,
                    amount, amount, IpdBillAdjustmentType.None, null));
            }
            else
            {
                // Prorated: split the block's charge by time share across every room occupied in
                // it. DEF-IPD-16 (prorated-same-day bug): accumulate full decimal precision and
                // only round the grand total once, at the very end of the whole calculation — the
                // old code rounded each segment's tiny share to 2 decimals as it went, which could
                // round EVERY segment in a same-day admit+shift+discharge down to ₹0.00
                // individually, silently billing nothing at all for a day the patient really did
                // occupy a room.
                foreach (var (segment, overlapStart, overlapEnd) in overlapping)
                {
                    var share = (decimal)(overlapEnd - overlapStart).Ticks / blockLength.Ticks;
                    var amount = segment.RatePerDayAtTime * share;
                    total += amount; // unrounded — see the comment above on why
                    // OBS-27 (14-Nov QA): the line item itself is a final, user-facing figure
                    // (shown on the Provisional Bill and in the API response) — round it for
                    // display here; this is independent of `total`, which keeps accumulating the
                    // unrounded share so the grand total still can't round down to ₹0.
                    var displayAmount = Math.Round(amount, 2);
                    lineItems.Add(new IpdBillLineItemDto(
                        overlapStart, overlapEnd, segment.Ward.Name, segment.Room.RoomNumber, segment.Bed.BedNumber, displayAmount,
                        displayAmount, displayAmount, IpdBillAdjustmentType.None, null));
                }
            }
        }

        return new IpdBillCalculationResult(Math.Round(total, 2), lineItems, Math.Round(writeOffTotal, 2));
    }

    public decimal CalculateStandardSegmentCharge(IpdBedTransferHistory segment, DateTime asOfUtc, MidStayBedChangeChargeMode midStayMode)
        => CalculateStandardCharge(segment.RatePerDayAtTime, segment.OccupiedFrom, asOfUtc, midStayMode);

    public decimal CalculateStandardCharge(decimal ratePerDay, DateTime occupiedFrom, DateTime asOfUtc, MidStayBedChangeChargeMode midStayMode)
    {
        if (midStayMode == MidStayBedChangeChargeMode.FullDayCharge)
            return ratePerDay;

        var elapsed = asOfUtc - occupiedFrom;
        if (elapsed <= TimeSpan.Zero)
            return 0m;

        var fraction = Math.Min(1m, (decimal)elapsed.Ticks / TimeSpan.FromDays(1).Ticks);
        return ratePerDay * fraction;
    }

    public decimal ResolveFullDayRate(decimal originStandardCharge, decimal destinationStandardCharge, IpdFullDayShiftRateRule rule) => rule switch
    {
        IpdFullDayShiftRateRule.HigherRate => Math.Max(originStandardCharge, destinationStandardCharge),
        IpdFullDayShiftRateRule.OriginRoomRate => originStandardCharge,
        _ => destinationStandardCharge // DestinationRoomRate — also the default for any future value.
    };

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
