using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicUsageRateConfiguration : IEntityTypeConfiguration<ClinicUsageRate>
{
    public void Configure(EntityTypeBuilder<ClinicUsageRate> builder)
    {
        builder.ToTable("ClinicUsageRates", "clinic");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ApplicationId).IsRequired();
        builder.Property(r => r.BillingItem).IsRequired().HasConversion<int>();
        builder.Property(r => r.Rate).HasPrecision(18, 4).IsRequired();
        builder.Property(r => r.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(r => r.UpdatedByAdminEmail).HasMaxLength(256);

        // At most one live rate per item per clinic. Deactivating the old row and inserting the new
        // one is allowed — the pair can never both be live, which is what keeps the rate in force
        // unambiguous without a version column.
        builder.HasIndex(r => new { r.ApplicationId, r.BillingItem })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.UpdatedBy).HasMaxLength(256);
        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}
