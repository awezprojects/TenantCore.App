using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class MedicineDosageFormConfiguration : IEntityTypeConfiguration<MedicineDosageForm>
{
    public void Configure(EntityTypeBuilder<MedicineDosageForm> builder)
    {
        builder.ToTable("MedicineDosageForms");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
        builder.Property(f => f.Description).HasMaxLength(500);
        builder.Property(f => f.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(f => f.Name).IsUnique();

        builder.Property(f => f.CreatedAt).IsRequired().HasColumnName("CreatedDate");
        builder.Property(f => f.UpdatedAt).HasColumnName("ModifiedDate");
        builder.Property(f => f.CreatedBy).HasMaxLength(256).HasColumnName("CreatedBy");
        builder.Property(f => f.UpdatedBy).HasMaxLength(256).HasColumnName("ModifiedBy");

        builder.HasData(
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0001-0000-0000-000000000000"), "Tablet", "Solid oral dosage form"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0002-0000-0000-000000000000"), "Tablet SR", "Sustained-release tablet"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0003-0000-0000-000000000000"), "Tablet XR", "Extended-release tablet"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0004-0000-0000-000000000000"), "Capsule", "Hard or soft shell capsule"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0005-0000-0000-000000000000"), "Syrup", "Liquid oral dosage form"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0006-0000-0000-000000000000"), "Dry Syrup", "Powder reconstituted as syrup"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0007-0000-0000-000000000000"), "Cream", "Topical semi-solid emulsion"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0008-0000-0000-000000000000"), "Eye Drop", "Ophthalmic liquid drops"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0009-0000-0000-000000000000"), "Drop", "Oral or nasal liquid drops"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0010-0000-0000-000000000000"), "Injection", "Parenteral dosage form"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0011-0000-0000-000000000000"), "Infusion", "Intravenous infusion solution"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0012-0000-0000-000000000000"), "Inhaler", "Inhalation device"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0013-0000-0000-000000000000"), "Patch", "Transdermal patch"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0014-0000-0000-000000000000"), "Powder", "Dry powder formulation"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0015-0000-0000-000000000000"), "Gel", "Topical gel"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0016-0000-0000-000000000000"), "Ointment", "Topical ointment"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0017-0000-0000-000000000000"), "Lotion", "Topical lotion"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0018-0000-0000-000000000000"), "Ophthalmic Suspension", "Eye suspension"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0019-0000-0000-000000000000"), "Oral Suspension", "Oral liquid suspension"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0020-0000-0000-000000000000"), "Ophthalmic Solution", "Eye solution"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0021-0000-0000-000000000000"), "Solution", "Liquid solution"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0022-0000-0000-000000000000"), "Spray", "Nasal or topical spray"),
            MedicineDosageForm.CreateForSeed(Guid.Parse("a1b2c3d4-0023-0000-0000-000000000000"), "Oral Solution", "Oral liquid solution")
        );
    }
}
