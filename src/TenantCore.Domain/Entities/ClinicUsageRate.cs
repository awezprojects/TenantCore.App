using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// One admin-set price per billable operation per clinic. Tenant-scoped.
///
/// <b>No row means rate 0 means free.</b> An admin who has not priced an operation cannot
/// accidentally block a clinic: the operation goes through, is recorded with UnitRate = 0 and
/// IsFree = true, and surfaces as unpriced on the Admin billing dashboard so the omission is
/// visible rather than silent.
///
/// Setting a rate is an upsert — the handler deactivates the current live row and inserts the new
/// one in the same SaveChanges, preserving the rate history the Admin audit shows.
/// </summary>
public class ClinicUsageRate : AuditableEntity
{
    public Guid ApplicationId { get; private set; }

    public BillingItem BillingItem { get; private set; }

    /// <summary>decimal(18,4) — per-unit price.</summary>
    public decimal Rate { get; private set; }

    /// <summary>At most one active row per (clinic, item), enforced by a filtered unique index.</summary>
    public bool IsActive { get; private set; }

    /// <summary>UTC. Informational — the rate in force is the newest active row.</summary>
    public DateTime? EffectiveFrom { get; private set; }

    public string? UpdatedByAdminEmail { get; private set; }

    private ClinicUsageRate() { }

    public static ClinicUsageRate Create(
        Guid applicationId, BillingItem billingItem, decimal rate, DateTime utcNow, string adminEmail)
    {
        if (rate < 0m)
            throw new DomainException("A usage rate cannot be negative — zero means free.");

        return new ClinicUsageRate
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            BillingItem = billingItem,
            Rate = rate,
            IsActive = true,
            EffectiveFrom = utcNow,
            UpdatedByAdminEmail = adminEmail,
            CreatedAt = utcNow
        };
    }

    /// <summary>Retires this row before its replacement is inserted, so the pair is never both live.</summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        SetUpdatedAt();
    }

    /// <summary>Corrects the price in place. Prefer deactivate-and-insert for a real price change —
    /// this exists for fixing a typo on a rate that has never been used.</summary>
    public void UpdateRate(decimal rate, string adminEmail)
    {
        if (rate < 0m)
            throw new DomainException("A usage rate cannot be negative — zero means free.");

        Rate = rate;
        UpdatedByAdminEmail = adminEmail;
        SetUpdatedAt();
    }
}
