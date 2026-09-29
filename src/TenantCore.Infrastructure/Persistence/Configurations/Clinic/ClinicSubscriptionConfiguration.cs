using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicSubscriptionConfiguration : IEntityTypeConfiguration<ClinicSubscription>
{
    public void Configure(EntityTypeBuilder<ClinicSubscription> builder)
    {
        builder.ToTable("ClinicSubscriptions", "clinic", t =>
        {
            t.HasCheckConstraint("CK_ClinicSubscriptions_PricePaid_NonNegative", "[PricePaid] >= 0");
            t.HasCheckConstraint("CK_ClinicSubscriptions_DurationDays_Positive", "[DurationDays] > 0");
        });
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.ApplicationId).IsRequired();
        builder.Property(s => s.SubscriptionPlanId).IsRequired();
        builder.Property(s => s.PlanCode).IsRequired().HasConversion<int>();
        builder.Property(s => s.PlanName).IsRequired().HasMaxLength(60);
        builder.Property(s => s.PricePaid).IsRequired().HasPrecision(18, 2);
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(3);
        builder.Property(s => s.DurationDays).IsRequired();
        builder.Property(s => s.StartDate).IsRequired();
        builder.Property(s => s.EndDate).IsRequired();
        builder.Property(s => s.Status).IsRequired().HasConversion<int>();
        builder.Property(s => s.CancelledBy).HasMaxLength(256);

        builder.Property(s => s.ClinicName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BillingContactEmail).IsRequired().HasMaxLength(256);
        builder.Property(s => s.BillingContactName).IsRequired().HasMaxLength(200);

        builder.Property(s => s.SubscriptionPaymentId);
        builder.Property(s => s.OnboardingRequestId);
        builder.Property(s => s.PurchasedByUserId);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.UpdatedBy).HasMaxLength(256);
        builder.Property(s => s.RowVersion).IsRowVersion();

        // Serves the guard's per-request lookup (ApplicationId + Status) and the
        // EndDate-ordered reads used to find the latest/active subscription.
        builder.HasIndex(s => new { s.ApplicationId, s.Status, s.EndDate });

        // Idempotency guards for the durable workflow's activation step — at most one
        // subscription per payment, and at most one per onboarding request (trial grants).
        builder.HasIndex(s => s.SubscriptionPaymentId).IsUnique().HasFilter("[SubscriptionPaymentId] IS NOT NULL");
        builder.HasIndex(s => s.OnboardingRequestId).IsUnique().HasFilter("[OnboardingRequestId] IS NOT NULL");

        builder.HasOne<SubscriptionPlan>()
               .WithMany()
               .HasForeignKey(s => s.SubscriptionPlanId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
