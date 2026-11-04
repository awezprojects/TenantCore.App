using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

/// <summary>
/// DEF-OPD-13: the OPD token number used to be derived from the highest <i>existing</i> row for the
/// day, so deleting the day's latest visit made the next registration reuse its number. This counter
/// is never decremented, including when a registration is later deleted, so a token number is never
/// reused for a given clinic and date.
/// </summary>
public class OpdRegistrationNumberCounter : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public DateTime CounterDate { get; private set; }
    public int LastSequence { get; private set; }

    private OpdRegistrationNumberCounter() { }

    public static OpdRegistrationNumberCounter Create(Guid applicationId, DateTime counterDate) => new()
    {
        ApplicationId = applicationId,
        CounterDate = counterDate.Date,
        LastSequence = 0
    };

    /// <summary>
    /// DEF-OPD-23: a counter created on a day that already has registrations (e.g. this migration's
    /// first day in production, or any day where registrations existed before the counter row did)
    /// must not restart from 0 — it would reissue numbers already given out. Seed it from the
    /// highest number already on record for that clinic+day before the first call to <see cref="Next"/>.
    /// </summary>
    public static OpdRegistrationNumberCounter CreateSeeded(Guid applicationId, DateTime counterDate, int seedSequence)
    {
        var counter = Create(applicationId, counterDate);
        counter.LastSequence = Math.Max(0, seedSequence);
        return counter;
    }

    public int Next()
    {
        LastSequence++;
        SetUpdatedAt();
        return LastSequence;
    }
}
