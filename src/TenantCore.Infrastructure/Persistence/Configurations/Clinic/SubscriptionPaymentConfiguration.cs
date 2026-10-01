using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("SubscriptionPayments", "clinic", t =>
        {
            t.HasCheckConstraint("CK_SubscriptionPayments_Amount_NonNegative", "[Amount] >= 0");
        });
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Purpose).IsRequired().HasConversion<int>();
        builder.Property(p => p.OnboardingRequestId);
        builder.Property(p => p.ApplicationId);

        builder.Property(p => p.SubscriptionPlanId).IsRequired();
        builder.Property(p => p.PlanCode).IsRequired().HasConversion<int>();
        builder.Property(p => p.PlanName).IsRequired().HasMaxLength(100);

        builder.Property(p => p.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.AmountInMinorUnits).IsRequired();
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.PlanListPrice).IsRequired().HasPrecision(18, 2);

        builder.Property(p => p.ReplacesPaymentId);
        builder.Property(p => p.ReplacedByPaymentId);

        builder.Property(p => p.PayerName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PayerEmail).IsRequired().HasMaxLength(256);
        builder.Property(p => p.PayerPhone).HasMaxLength(20);

        builder.Property(p => p.GatewayPaymentLinkId).HasMaxLength(64);
        builder.Property(p => p.PaymentLinkUrl).HasMaxLength(500);
        builder.Property(p => p.LinkExpiresAt);

        builder.Property(p => p.Status).IsRequired().HasConversion<int>();
        builder.Property(p => p.GatewayPaymentId).HasMaxLength(64);
        builder.Property(p => p.Method).HasMaxLength(20);
        builder.Property(p => p.PaidAt);

        builder.Property(p => p.ClinicSubscriptionId);
        builder.Property(p => p.InitiatedByUserId);
        builder.Property(p => p.InitiatedByAdminEmail).HasMaxLength(256);
        builder.Property(p => p.AmountReason).HasMaxLength(500);
        builder.Property(p => p.ClinicName).HasMaxLength(200);
        builder.Property(p => p.LastCheckAt);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.UpdatedBy).HasMaxLength(256);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasIndex(p => p.OnboardingRequestId);
        builder.HasIndex(p => p.ApplicationId);
        builder.HasIndex(p => p.GatewayPaymentLinkId).IsUnique().HasFilter("[GatewayPaymentLinkId] IS NOT NULL");
        builder.HasIndex(p => p.GatewayPaymentId).IsUnique().HasFilter("[GatewayPaymentId] IS NOT NULL");
        builder.HasIndex(p => p.ClinicSubscriptionId).IsUnique().HasFilter("[ClinicSubscriptionId] IS NOT NULL");
        // Reconciliation sweep's query: LinkCreated payments older than N minutes.
        builder.HasIndex(p => new { p.Status, p.CreatedAt });
    }
}
