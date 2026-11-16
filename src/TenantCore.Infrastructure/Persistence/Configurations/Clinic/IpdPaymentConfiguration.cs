using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class IpdPaymentConfiguration : IEntityTypeConfiguration<IpdPayment>
{
    public void Configure(EntityTypeBuilder<IpdPayment> builder)
    {
        builder.ToTable("IpdPayments", "clinic");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.ApplicationId).IsRequired();
        builder.Property(p => p.IpdRegistrationId).IsRequired();
        builder.HasIndex(p => p.IpdRegistrationId).IsUnique();

        builder.Property(p => p.InitialFee).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.RoomChargesTotal).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.ParticularsTotal).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.TotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.Discount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.FinalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.TotalDeposited).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.BalanceDue).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.RefundDue).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.PaymentStatus).IsRequired();
        builder.Property(p => p.RefundStatus).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.UpdatedBy).HasMaxLength(256);
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
