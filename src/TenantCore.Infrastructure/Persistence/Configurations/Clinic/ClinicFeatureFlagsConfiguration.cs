using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

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

        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.RowVersion).IsRowVersion();
    }
}
