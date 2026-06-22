using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class DoctorFeeConfigConfiguration : IEntityTypeConfiguration<DoctorFeeConfig>
{
    public void Configure(EntityTypeBuilder<DoctorFeeConfig> builder)
    {
        builder.ToTable("DoctorFeeConfigs", "clinic");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.ApplicationId).IsRequired();
        builder.HasIndex(d => d.ApplicationId);

        builder.Property(d => d.DoctorProfileId).IsRequired();
        builder.HasIndex(d => new { d.DoctorProfileId, d.ApplicationId }).IsUnique();

        builder.Property(d => d.VisitFee).IsRequired().HasPrecision(18, 2);
        builder.Property(d => d.IsActive).IsRequired();

        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(256);
        builder.Property(d => d.UpdatedBy).HasMaxLength(256);
        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}
