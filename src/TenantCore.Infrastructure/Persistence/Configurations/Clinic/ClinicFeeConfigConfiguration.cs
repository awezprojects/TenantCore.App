using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicFeeConfigConfiguration : IEntityTypeConfiguration<ClinicFeeConfig>
{
    public void Configure(EntityTypeBuilder<ClinicFeeConfig> builder)
    {
        builder.ToTable("ClinicFeeConfigs", "clinic");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ApplicationId).IsRequired();
        builder.HasIndex(c => c.ApplicationId).IsUnique();

        builder.Property(c => c.OpdFee).IsRequired().HasPrecision(18, 2);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
