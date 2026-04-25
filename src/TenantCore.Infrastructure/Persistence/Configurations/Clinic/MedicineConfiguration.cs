using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("Medicines");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.GenericName).HasMaxLength(200);
        builder.Property(m => m.BrandName).HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(1000);
        builder.Property(m => m.Composition).HasMaxLength(500);
        builder.Property(m => m.Composition2).HasMaxLength(500);
        builder.Property(m => m.Dosage).HasMaxLength(200);
        builder.Property(m => m.Form).HasMaxLength(50);
        builder.Property(m => m.Manufacturer).HasMaxLength(200);
        builder.Property(m => m.IsGeneric).IsRequired().HasDefaultValue(false);
        builder.Property(m => m.PackSize).HasMaxLength(100);
        builder.Property(m => m.Uses).HasMaxLength(1000);
        builder.Property(m => m.SideEffects).HasMaxLength(1000);
        builder.Property(m => m.Contraindications).HasMaxLength(1000);
        builder.Property(m => m.Storage).HasMaxLength(200);
        builder.Property(m => m.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(m => m.Name);
        builder.HasIndex(m => m.GenericName);
        builder.HasIndex(m => m.BrandName);

        builder.HasOne(m => m.MedicineType)
            .WithMany()
            .HasForeignKey(m => m.MedicineTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(m => m.CreatedAt).IsRequired().HasColumnName("CreatedDate");
        builder.Property(m => m.UpdatedAt).HasColumnName("ModifiedDate");
        builder.Property(m => m.CreatedBy).HasMaxLength(256).HasColumnName("CreatedBy");
        builder.Property(m => m.UpdatedBy).HasMaxLength(256).HasColumnName("ModifiedBy");
    }
}
