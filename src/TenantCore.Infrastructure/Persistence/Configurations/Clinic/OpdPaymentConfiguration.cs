using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class OpdPaymentConfiguration : IEntityTypeConfiguration<OpdPayment>
{
    public void Configure(EntityTypeBuilder<OpdPayment> builder)
    {
        builder.ToTable("OpdPayments", "clinic");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.ApplicationId).IsRequired();
        builder.HasIndex(o => o.ApplicationId);

        builder.Property(o => o.OpdRegistrationId).IsRequired();
        builder.HasIndex(o => o.OpdRegistrationId).IsUnique();

        builder.Property(o => o.VisitFee).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.ParticularsTotal).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.TotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.Discount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.FinalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.CollectedAmount).IsRequired().HasPrecision(18, 2);

        builder.Property(o => o.PaymentStatus)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(o => o.ReceivedByUserId);
        builder.Property(o => o.CounterSessionId);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.CreatedBy).HasMaxLength(256);
        builder.Property(o => o.UpdatedBy).HasMaxLength(256);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne<OpdRegistration>()
               .WithMany()
               .HasForeignKey(o => o.OpdRegistrationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
