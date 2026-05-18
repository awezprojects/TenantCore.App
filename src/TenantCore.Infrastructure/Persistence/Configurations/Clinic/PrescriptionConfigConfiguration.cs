using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class PrescriptionConfigConfiguration : IEntityTypeConfiguration<PrescriptionConfig>
{
    public void Configure(EntityTypeBuilder<PrescriptionConfig> builder)
    {
        builder.ToTable("PrescriptionConfigs", "clinic");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ApplicationId).IsRequired();
        builder.HasIndex(c => c.ApplicationId).IsUnique();

        builder.Property(c => c.DefaultLanguage).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
    }
}
