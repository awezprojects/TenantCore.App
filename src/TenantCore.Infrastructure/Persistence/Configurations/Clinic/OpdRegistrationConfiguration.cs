using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class OpdRegistrationConfiguration : IEntityTypeConfiguration<OpdRegistration>
{
    public void Configure(EntityTypeBuilder<OpdRegistration> builder)
    {
        builder.ToTable("OpdRegistrations", "clinic");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.ApplicationId).IsRequired();
        builder.HasIndex(o => o.ApplicationId);

        builder.Property(o => o.PatientId).IsRequired();
        builder.Property(o => o.DoctorUserId).IsRequired();
        builder.Property(o => o.DoctorName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.RegistrationNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(o => new { o.ApplicationId, o.RegistrationNumber }).IsUnique();
        builder.Property(o => o.RegistrationDate).IsRequired();
        builder.Property(o => o.Fee).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.Weight).HasPrecision(5, 1);
        builder.Property(o => o.BloodPressure).HasMaxLength(20);
        builder.Property(o => o.OxygenSaturation).HasPrecision(4, 1);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.CreatedBy).HasMaxLength(256);
        builder.Property(o => o.UpdatedBy).HasMaxLength(256);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne(o => o.Patient)
               .WithMany()
               .HasForeignKey(o => o.PatientId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
