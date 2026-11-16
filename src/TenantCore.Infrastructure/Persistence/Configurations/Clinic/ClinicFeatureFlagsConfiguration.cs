using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicFeatureFlagsConfiguration : IEntityTypeConfiguration<ClinicFeatureFlags>
{
    public void Configure(EntityTypeBuilder<ClinicFeatureFlags> builder)
    {
        builder.ToTable("ClinicFeatureFlags", "clinic");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.ApplicationId).IsRequired();
        builder.HasIndex(f => f.ApplicationId).IsUnique();

        builder.Property(f => f.PrepaidOpdEnabled).IsRequired();
        builder.Property(f => f.BillingEnabled).IsRequired();
        // Nullable, no default (confirmed) — unset until a Clinic Admin explicitly picks one.
        builder.Property(f => f.IpdRoomBillingCycle);
        builder.Property(f => f.IpdMidStayChargeMode);

        // DEF-IPD-16 (13-Nov). Unlike the two above, these four DO have defaults — the ones that
        // reproduce today's behavior exactly, so no existing clinic's bills change until a Clinic
        // Admin deliberately opts into one of these.
        builder.Property(f => f.IpdShiftGraceWindowMinutes).IsRequired().HasDefaultValue(0);
        builder.Property(f => f.IpdShiftGraceRule);
        builder.Property(f => f.IpdManualShiftChargeMode).IsRequired().HasDefaultValue(IpdManualShiftChargeMode.Off);
        builder.Property(f => f.IpdFullDayShiftRateRule).IsRequired().HasDefaultValue(IpdFullDayShiftRateRule.DestinationRoomRate);

        // DEF-IPD-26 (15-Nov). 0 = unlimited (today's behavior — unchanged until a Clinic Admin opts in).
        builder.Property(f => f.IpdCancelWindowMinutes).IsRequired().HasDefaultValue(0);

        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.RowVersion).IsRowVersion();
    }
}
