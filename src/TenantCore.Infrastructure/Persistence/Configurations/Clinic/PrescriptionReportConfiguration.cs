using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class PrescriptionReportConfiguration : IEntityTypeConfiguration<PrescriptionReport>
{
    public void Configure(EntityTypeBuilder<PrescriptionReport> builder)
    {
        builder.ToTable("PrescriptionReports", "clinic");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.PrescriptionId).IsRequired();
        builder.Property(r => r.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(r => r.StoredFileName).IsRequired().HasMaxLength(255);
        builder.Property(r => r.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(r => r.FileSizeBytes);
        builder.Property(r => r.UploadedAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
    }
}
