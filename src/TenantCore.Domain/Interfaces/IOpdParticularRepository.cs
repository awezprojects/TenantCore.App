using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IOpdParticularRepository : IClinicRepository<OpdParticular>
{
    Task<IEnumerable<OpdParticular>> GetByOpdRegistrationIdAsync(Guid opdRegistrationId, Guid applicationId, CancellationToken ct = default);
    Task<decimal> GetTotalByOpdRegistrationIdAsync(Guid opdRegistrationId, Guid applicationId, CancellationToken ct = default);

    // Returns all service items collected in a given counter session (CounterSessionId set, Received).
    // Used by counter handlers to compute the per-item portion of the session total.
    Task<IEnumerable<OpdParticular>> GetCollectedBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default);

    // Same as GetCollectedBySessionIdAsync but for several sessions at once (e.g. every session a
    // given receptionist opened in a date range) — one query instead of one per session.
    Task<IEnumerable<OpdParticular>> GetCollectedBySessionIdsAsync(IEnumerable<Guid> sessionIds, Guid applicationId, CancellationToken ct = default);

    // Returns all service items individually collected within [fromUtc, toExclusiveUtc) by their
    // OWN CollectedAt — not by when the registration's visit fee was first received. Used by
    // date-bucketed reports (DEF-E2E-12) to credit an item's money to the day it was actually
    // collected, the same way GetCollectedBySessionIdAsync credits it to the session it was
    // actually collected in.
    Task<IEnumerable<OpdParticular>> GetCollectedByDateRangeAsync(DateTime fromUtc, DateTime toExclusiveUtc, Guid applicationId, CancellationToken ct = default);

    // DEF-E2E-08/09/11/12: OpdPayment.CollectedAmount is a running total across every event for a
    // registration — the visit fee/full-bill, plus every individually collected item, however many
    // sessions or days apart. Callers that attribute money to a specific session or day (not "this
    // registration, ever") must back the item portion out first: payment.CollectedAmount minus the
    // value this returns for that registration is the fee/full-bill-only "base" contribution, with
    // each item's own amount added back wherever (and whenever) it was actually collected via
    // GetCollectedBySessionIdAsync / GetCollectedByDateRangeAsync instead.
    Task<Dictionary<Guid, decimal>> GetIndividuallyCollectedTotalsByRegistrationIdsAsync(IEnumerable<Guid> opdRegistrationIds, Guid applicationId, CancellationToken ct = default);
}
