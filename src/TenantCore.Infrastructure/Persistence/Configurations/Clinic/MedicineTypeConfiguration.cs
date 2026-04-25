using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class MedicineTypeConfiguration : IEntityTypeConfiguration<MedicineType>
{
    public void Configure(EntityTypeBuilder<MedicineType> builder)
    {
        builder.ToTable("MedicineTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(t => t.Name).IsUnique();

        builder.Property(t => t.CreatedAt).IsRequired().HasColumnName("CreatedDate");
        builder.Property(t => t.UpdatedAt).HasColumnName("ModifiedDate");
        builder.Property(t => t.CreatedBy).HasMaxLength(256).HasColumnName("CreatedBy");
        builder.Property(t => t.UpdatedBy).HasMaxLength(256).HasColumnName("ModifiedBy");
    }
}
