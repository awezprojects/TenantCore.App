using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ObstetricPrescriptionDataConfiguration : IEntityTypeConfiguration<ObstetricPrescriptionData>
{
    public void Configure(EntityTypeBuilder<ObstetricPrescriptionData> builder)
    {
        builder.ToTable("ObstetricPrescriptionData", "clinic");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.PrescriptionId).IsRequired();
        builder.HasIndex(o => o.PrescriptionId).IsUnique();

        builder.Property(o => o.Gravida);
        builder.Property(o => o.Para);
        builder.Property(o => o.Live);
        builder.Property(o => o.Abortion);
        builder.Property(o => o.Information).HasMaxLength(2000);
        builder.Property(o => o.MenstrualHistory);
        builder.Property(o => o.PastMedicalHistory);
        builder.Property(o => o.FamilyHistory);
        builder.Property(o => o.PerAbdomen);
        builder.Property(o => o.PerVaginum);
        builder.Property(o => o.SurgicalHistory);
        builder.Property(o => o.PerSpeculum);
        builder.Property(o => o.Lmp);
        builder.Property(o => o.EddByLmp);
        builder.Property(o => o.EddByUsg);

        builder.Property(o => o.CreatedAt).IsRequired();
    }
}
