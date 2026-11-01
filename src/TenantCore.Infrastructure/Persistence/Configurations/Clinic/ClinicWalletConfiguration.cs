using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicWalletConfiguration : IEntityTypeConfiguration<ClinicWallet>
{
    public void Configure(EntityTypeBuilder<ClinicWallet> builder)
    {
        builder.ToTable("ClinicWallets", "clinic");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        // At most one wallet per clinic. This is also the debit path's lookup.
        builder.Property(w => w.ApplicationId).IsRequired();
        builder.HasIndex(w => w.ApplicationId).IsUnique();

        builder.Property(w => w.Balance).HasPrecision(18, 2).IsRequired();
        builder.Property(w => w.LowBalanceThreshold).HasPrecision(18, 2).IsRequired();
        builder.Property(w => w.Currency).IsRequired().HasMaxLength(3);

        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.CreatedBy).HasMaxLength(256);
        builder.Property(w => w.UpdatedBy).HasMaxLength(256);

        // The concurrency token the debit path depends on: two concurrent charges cannot both pass
        // the balance check, so the wallet can never go negative.
        builder.Property(w => w.RowVersion).IsRowVersion();
    }
}
