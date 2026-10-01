using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// The platform's own state for one clinic — access (suspended or not) and which plans it may see.
/// Tenant-scoped, at most one row per clinic.
///
/// Absence of a row means the defaults: Active, public catalogue. A row is created lazily by the
/// first admin action that needs one, so existing clinics need no backfill.
///
/// Suspension does NOT pause the subscription clock: the term keeps running while the clinic is
/// locked out (an admin compensates with a free grant if needed).
/// </summary>
public class ClinicAccount : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public ClinicAccessStatus AccessStatus { get; private set; }

    /// <summary>Shown to every user of the clinic while it is suspended.</summary>
    public string? SuspensionMessage { get; private set; }

    public DateTime? SuspendedAt { get; private set; }
    public string? SuspendedByAdminEmail { get; private set; }
    public DateTime? ReactivatedAt { get; private set; }
    public string? ReactivatedByAdminEmail { get; private set; }

    /// <summary>True hides the public catalogue — the clinic then sees only plans it has a live offer for.</summary>
    public bool RestrictToOfferedPlans { get; private set; }

    private ClinicAccount() { }

    public static ClinicAccount CreateDefault(Guid applicationId) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        AccessStatus = ClinicAccessStatus.Active,
        RestrictToOfferedPlans = false,
        CreatedAt = DateTime.UtcNow
    };

    public bool IsSuspended => AccessStatus == ClinicAccessStatus.Suspended;

    public void Suspend(string message, string adminEmail)
    {
        if (IsSuspended)
            throw new InvalidOperationException("This clinic is already suspended.");

        AccessStatus = ClinicAccessStatus.Suspended;
        SuspensionMessage = message;
        SuspendedAt = DateTime.UtcNow;
        SuspendedByAdminEmail = adminEmail;
        SetUpdatedAt();
    }

    public void Reactivate(string adminEmail)
    {
        if (!IsSuspended)
            throw new InvalidOperationException("This clinic is not suspended.");

        AccessStatus = ClinicAccessStatus.Active;
        // SuspensionMessage is deliberately kept: the Admin portal shows what the last suspension said.
        ReactivatedAt = DateTime.UtcNow;
        ReactivatedByAdminEmail = adminEmail;
        SetUpdatedAt();
    }

    public void SetRestrictToOfferedPlans(bool restrict)
    {
        RestrictToOfferedPlans = restrict;
        SetUpdatedAt();
    }
}
