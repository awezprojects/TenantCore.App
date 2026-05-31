using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations;

internal sealed class DoctorProfileConfiguration : IEntityTypeConfiguration<DoctorProfile>
{
    public void Configure(EntityTypeBuilder<DoctorProfile> builder)
    {
        builder.HasKey(dp => dp.Id);
        builder.HasIndex(dp => dp.UserId).IsUnique();
        builder.Property(dp => dp.RegistrationNumber).IsRequired().HasMaxLength(100);
        builder.Property(dp => dp.Specialty).HasMaxLength(200);
        builder.Property(dp => dp.QualificationDetails).HasMaxLength(500);
        builder.Property(dp => dp.RowVersion).IsRowVersion();
    }
}
