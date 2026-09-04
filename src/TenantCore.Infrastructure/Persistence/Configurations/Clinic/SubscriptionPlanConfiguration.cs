using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    // Fixed seed GUIDs — never change these once deployed, ClinicSubscription rows reference them by Id.
    private static readonly Guid TrialId = Guid.Parse("b2c3d4e5-0001-0000-0000-000000000000");
    private static readonly Guid MonthlyId = Guid.Parse("b2c3d4e5-0002-0000-0000-000000000000");
    private static readonly Guid QuarterlyId = Guid.Parse("b2c3d4e5-0003-0000-0000-000000000000");
    private static readonly Guid YearlyId = Guid.Parse("b2c3d4e5-0004-0000-0000-000000000000");

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans", "clinic");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code).IsRequired().HasConversion<int>();
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(60);
        builder.Property(p => p.Description).HasMaxLength(250);
        builder.Property(p => p.DurationDays).IsRequired();
        builder.Property(p => p.Price).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("INR");
        builder.Property(p => p.IsTrial).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.IsPopular).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DisplayOrder).IsRequired();
        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.UpdatedBy).HasMaxLength(256);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasData(
            SubscriptionPlan.CreateForSeed(TrialId, SubscriptionPlanCode.Trial, "Free Trial",
                "Try every feature free for 14 days.", 14, 0m, "INR", isTrial: true, isPopular: false, displayOrder: 1),
            SubscriptionPlan.CreateForSeed(MonthlyId, SubscriptionPlanCode.Monthly, "Monthly",
                "Billed every 30 days. Cancel anytime.", 30, 999m, "INR", isTrial: false, isPopular: false, displayOrder: 2),
            SubscriptionPlan.CreateForSeed(QuarterlyId, SubscriptionPlanCode.Quarterly, "Quarterly",
                "Our most popular plan — save versus monthly billing.", 90, 2499m, "INR", isTrial: false, isPopular: true, displayOrder: 3),
            SubscriptionPlan.CreateForSeed(YearlyId, SubscriptionPlanCode.Yearly, "Yearly",
                "The best value — a full year of every feature.", 365, 8999m, "INR", isTrial: false, isPopular: false, displayOrder: 4)
        );
    }
}
