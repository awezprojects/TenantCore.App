using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class MedicineBundleItemConfiguration : IEntityTypeConfiguration<MedicineBundleItem>
{
    public void Configure(EntityTypeBuilder<MedicineBundleItem> builder)
    {
        builder.ToTable("MedicineBundleItems", "clinic");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.MedicineBundleId).IsRequired();
        builder.Property(i => i.MedicineId).IsRequired();
        builder.Property(i => i.MedicineName).IsRequired().HasMaxLength(300);
        builder.Property(i => i.GenericName).HasMaxLength(300);
        builder.Property(i => i.MedicineForm).IsRequired();
        builder.Property(i => i.Strength).HasMaxLength(100);
        builder.Property(i => i.DosageUnit).IsRequired().HasMaxLength(20);
        builder.Property(i => i.DosageMorning).HasPrecision(5, 2);
        builder.Property(i => i.DosageAfternoon).HasPrecision(5, 2);
        builder.Property(i => i.DosageEvening).HasPrecision(5, 2);
        builder.Property(i => i.DosageNight).HasPrecision(5, 2);
        builder.Property(i => i.DurationDays).IsRequired();
        builder.Property(i => i.Quantity).IsRequired().HasPrecision(10, 2);
        builder.Property(i => i.Frequency).HasMaxLength(20);
        builder.Property(i => i.Timing).HasMaxLength(100);
        builder.Property(i => i.Instructions).HasMaxLength(500);
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.HasOne<Medicine>()
               .WithMany()
               .HasForeignKey(i => i.MedicineId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
