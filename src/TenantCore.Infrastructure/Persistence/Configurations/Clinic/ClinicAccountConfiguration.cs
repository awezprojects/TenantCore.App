using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicAccountConfiguration : IEntityTypeConfiguration<ClinicAccount>
{
    public void Configure(EntityTypeBuilder<ClinicAccount> builder)
    {
        builder.ToTable("ClinicAccounts", "clinic");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        // Unique: at most one account row per clinic. The guard reads this by ApplicationId on
        // every clinic-scoped request, so the index is the lookup path too.
        builder.Property(a => a.ApplicationId).IsRequired();
        builder.HasIndex(a => a.ApplicationId).IsUnique();

        builder.Property(a => a.AccessStatus).IsRequired().HasConversion<int>();
        builder.Property(a => a.SuspensionMessage).HasMaxLength(500);
        builder.Property(a => a.SuspendedByAdminEmail).HasMaxLength(256);
        builder.Property(a => a.ReactivatedByAdminEmail).HasMaxLength(256);
        builder.Property(a => a.RestrictToOfferedPlans).IsRequired().HasDefaultValue(false);

        // Phase 2 — billing mode. The column-level default is 1 (Subscription), so every pre-existing
        // row is already correct and the migration needs no backfill. A queued change stores the mode
        // the clinic will switch to, and the instant it becomes live (the coverage end).
        builder.Property(a => a.BillingMode).IsRequired().HasConversion<int>()
            .HasDefaultValue(BillingMode.Subscription);
        builder.Property(a => a.PendingBillingMode).HasConversion<int>();

        // Phase 2 — the one-time registration fee: a per-clinic override of the plan's catalogue
        // default, its one payment, and the waive that is the only alternative to paying it.
        builder.Property(a => a.RegistrationFee).HasPrecision(18, 2);
        builder.Property(a => a.RegistrationFeeChargedBy).HasMaxLength(256);
        builder.Property(a => a.RegistrationFeeWaived).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.RegistrationFeeWaiveReason).HasMaxLength(500);
        builder.Property(a => a.RegistrationFeeWaivedByAdminEmail).HasMaxLength(256);

        // Phase 2 — package-granted free test days (30–90 when set). Bounds are enforced by the
        // entity and the validator, not by a check constraint: the Admin portal mirrors the rule.
        builder.Property(a => a.FreeTestDays);

        // Phase 2 — deferred wallet credit: stored at activation for PAYG clinics with a registration
        // fee, cleared once the fee is paid and the credit is applied to the wallet.
        builder.Property(a => a.PendingWalletCreditOnFeePayment).HasPrecision(18, 2);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(256);
        builder.Property(a => a.UpdatedBy).HasMaxLength(256);
        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}
