using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems", "clinic");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.PrescriptionId).IsRequired();
        builder.Property(i => i.MedicineId).IsRequired();
        builder.Property(i => i.MedicineName).IsRequired().HasMaxLength(300);
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
        builder.Property(i => i.RemarkEnglish).HasMaxLength(500);
        builder.Property(i => i.RemarkHindi).HasMaxLength(500);
        builder.Property(i => i.RemarkMarathi).HasMaxLength(500);
        builder.Property(i => i.SortOrder).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.HasOne<Medicine>()
               .WithMany()
               .HasForeignKey(i => i.MedicineId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
