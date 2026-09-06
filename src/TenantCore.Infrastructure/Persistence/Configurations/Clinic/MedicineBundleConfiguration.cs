using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class MedicineBundleConfiguration : IEntityTypeConfiguration<MedicineBundle>
{
    public void Configure(EntityTypeBuilder<MedicineBundle> builder)
    {
        builder.ToTable("MedicineBundles", "clinic");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.ApplicationId).IsRequired();
        builder.HasIndex(b => b.ApplicationId);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.DurationDays).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(1000);
        builder.Property(b => b.CreatedByUserId).IsRequired();
        builder.Property(b => b.CreatedByName).IsRequired().HasMaxLength(200);

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.CreatedBy).HasMaxLength(256);
        builder.Property(b => b.UpdatedBy).HasMaxLength(256);

        builder.HasMany(b => b.Items)
               .WithOne()
               .HasForeignKey(i => i.MedicineBundleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
