using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions", "clinic");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.ApplicationId).IsRequired();
        builder.HasIndex(p => p.ApplicationId);

        builder.Property(p => p.OpdRegistrationId).IsRequired();
        builder.Property(p => p.PatientId).IsRequired();
        builder.Property(p => p.DoctorUserId).IsRequired();
        builder.Property(p => p.DoctorName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PrescriptionNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(p => new { p.ApplicationId, p.PrescriptionNumber }).IsUnique();
        builder.Property(p => p.PrescribedDate).IsRequired();
        builder.Property(p => p.NextVisitDate);
        builder.Property(p => p.Diagnosis).HasMaxLength(1000);
        builder.Property(p => p.Investigations);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.VitalBP).HasMaxLength(20);
        builder.Property(p => p.VitalPulse);
        builder.Property(p => p.VitalTemp).HasPrecision(4, 1);
        builder.Property(p => p.VitalWeight).HasPrecision(5, 1);
        builder.Property(p => p.VitalSpO2).HasPrecision(4, 1);
        builder.Property(p => p.VitalRR);
        builder.Property(p => p.VitalSugar).HasPrecision(6, 1);
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.IsEmailSent).IsRequired().HasDefaultValue(false);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.UpdatedBy).HasMaxLength(256);

        builder.HasOne<OpdRegistration>()
               .WithMany()
               .HasForeignKey(p => p.OpdRegistrationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Patient>()
               .WithMany()
               .HasForeignKey(p => p.PatientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Items)
               .WithOne()
               .HasForeignKey(i => i.PrescriptionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Reports)
               .WithOne()
               .HasForeignKey(r => r.PrescriptionId)
               .OnDelete(DeleteBehavior.Cascade);

        // Specialty-specific 1:0..1 relationships (FK lives on the specialty table)
        builder.HasOne(p => p.ObstetricData)
               .WithOne()
               .HasForeignKey<ObstetricPrescriptionData>(o => o.PrescriptionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Reports).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
