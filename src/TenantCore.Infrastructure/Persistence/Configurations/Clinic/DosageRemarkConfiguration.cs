using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class DosageRemarkConfiguration : IEntityTypeConfiguration<DosageRemark>
{
    public void Configure(EntityTypeBuilder<DosageRemark> builder)
    {
        builder.ToTable("DosageRemarks", "clinic");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.ApplicationId).IsRequired();
        builder.HasIndex(d => new { d.ApplicationId, d.MedicineForm });

        builder.Property(d => d.MedicineForm).IsRequired();
        builder.Property(d => d.RemarkEnglish).IsRequired().HasMaxLength(500);
        builder.Property(d => d.RemarkHindi).HasMaxLength(500);
        builder.Property(d => d.RemarkMarathi).HasMaxLength(500);
        builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(256);
        builder.Property(d => d.UpdatedBy).HasMaxLength(256);
    }
}
