using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class IpdDepositConfiguration : IEntityTypeConfiguration<IpdDeposit>
{
    public void Configure(EntityTypeBuilder<IpdDeposit> builder)
    {
        builder.ToTable("IpdDeposits", "clinic", t => t.HasCheckConstraint("CK_IpdDeposits_Amount_Positive", "[Amount] > 0"));
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.ApplicationId).IsRequired();
        builder.Property(d => d.IpdRegistrationId).IsRequired();
        builder.HasIndex(d => new { d.ApplicationId, d.IpdRegistrationId });

        builder.Property(d => d.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(d => d.CollectedByUserId).IsRequired();
        builder.Property(d => d.CollectedAt).IsRequired();
        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(256);
        builder.Property(d => d.UpdatedBy).HasMaxLength(256);
        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}
