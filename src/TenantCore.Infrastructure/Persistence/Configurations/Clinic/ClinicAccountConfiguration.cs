using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

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

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(256);
        builder.Property(a => a.UpdatedBy).HasMaxLength(256);
        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}
