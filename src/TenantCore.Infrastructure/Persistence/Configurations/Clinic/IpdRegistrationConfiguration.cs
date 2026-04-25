using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class IpdRegistrationConfiguration : IEntityTypeConfiguration<IpdRegistration>
{
    public void Configure(EntityTypeBuilder<IpdRegistration> builder)
    {
        builder.ToTable("IpdRegistrations", "clinic");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.ApplicationId).IsRequired();
        builder.HasIndex(i => i.ApplicationId);

        builder.Property(i => i.PatientId).IsRequired();
        builder.Property(i => i.DoctorUserId).IsRequired();
        builder.Property(i => i.DoctorName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.AdmissionNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(i => new { i.ApplicationId, i.AdmissionNumber }).IsUnique();
        builder.Property(i => i.AdmissionDate).IsRequired();
        builder.Property(i => i.WardName).HasMaxLength(100);
        builder.Property(i => i.RoomNumber).HasMaxLength(20);
        builder.Property(i => i.BedNumber).HasMaxLength(20);
        builder.Property(i => i.InitialFee).IsRequired().HasPrecision(18, 2);
        builder.Property(i => i.Status).IsRequired();
        builder.Property(i => i.AdmissionNotes).HasMaxLength(1000);
        builder.Property(i => i.DischargeNotes).HasMaxLength(1000);

        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.CreatedBy).HasMaxLength(256);
        builder.Property(i => i.UpdatedBy).HasMaxLength(256);
        builder.Property(i => i.RowVersion).IsRowVersion();

        builder.HasOne(i => i.Patient)
               .WithMany()
               .HasForeignKey(i => i.PatientId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
