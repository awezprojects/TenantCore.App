using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class SubscriptionAlertSettingConfiguration : IEntityTypeConfiguration<SubscriptionAlertSetting>
{
    // Fixed seed GUIDs — the future notification Function and admin portal will reference these rows.
    private static readonly Guid Reminder10DayId = Guid.Parse("c3d4e5f6-0001-0000-0000-000000000000");
    private static readonly Guid Reminder5DayId = Guid.Parse("c3d4e5f6-0002-0000-0000-000000000000");
    private static readonly Guid Reminder2DayId = Guid.Parse("c3d4e5f6-0003-0000-0000-000000000000");
    private static readonly Guid ExpiredId = Guid.Parse("c3d4e5f6-0004-0000-0000-000000000000");

    public void Configure(EntityTypeBuilder<SubscriptionAlertSetting> builder)
    {
        builder.ToTable("SubscriptionAlertSettings", "clinic");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.AlertType).IsRequired().HasConversion<int>();
        builder.Property(s => s.DaysBeforeExpiry).IsRequired();
        builder.HasIndex(s => new { s.AlertType, s.DaysBeforeExpiry }).IsUnique();

        builder.Property(s => s.Subject).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Headline).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BodyMessage).IsRequired().HasMaxLength(1000);
        builder.Property(s => s.IsEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.DisplayOrder).IsRequired();

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.UpdatedBy).HasMaxLength(256);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasData(
            SubscriptionAlertSetting.CreateForSeed(Reminder10DayId, SubscriptionAlertType.ExpiryReminder, 10,
                "Your {ClinicName} subscription expires in {DaysRemaining} days",
                "Time to renew soon",
                "Your subscription is set to expire on {ExpiryDate}. Renew now to avoid any interruption to your clinic's access.",
                displayOrder: 1),
            SubscriptionAlertSetting.CreateForSeed(Reminder5DayId, SubscriptionAlertType.ExpiryReminder, 5,
                "Reminder: {ClinicName} subscription expires in {DaysRemaining} days",
                "Your subscription expires soon",
                "Only a few days left. Renew before {ExpiryDate} to keep your clinic running without interruption.",
                displayOrder: 2),
            SubscriptionAlertSetting.CreateForSeed(Reminder2DayId, SubscriptionAlertType.ExpiryReminder, 2,
                "Final notice: {ClinicName} subscription expires in {DaysRemaining} days",
                "Final reminder — act now",
                "Your subscription expires on {ExpiryDate}. Renew today to avoid losing access to your clinic.",
                displayOrder: 3),
            SubscriptionAlertSetting.CreateForSeed(ExpiredId, SubscriptionAlertType.Expired, 0,
                "Your {ClinicName} subscription has expired",
                "Subscription expired",
                "Your subscription expired on {ExpiryDate}. Choose a plan to restore access to your clinic.",
                displayOrder: 4)
        );
    }
}
