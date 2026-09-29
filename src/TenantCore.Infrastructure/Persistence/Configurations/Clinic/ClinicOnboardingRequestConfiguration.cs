using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicOnboardingRequestConfiguration : IEntityTypeConfiguration<ClinicOnboardingRequest>
{
    public void Configure(EntityTypeBuilder<ClinicOnboardingRequest> builder)
    {
        builder.ToTable("ClinicOnboardingRequests", "clinic");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.RequestedByUserId).IsRequired();
        builder.Property(r => r.RequesterName).IsRequired().HasMaxLength(150);
        builder.Property(r => r.RequesterEmail).IsRequired().HasMaxLength(256);
        builder.Property(r => r.RequesterPhone).IsRequired().HasMaxLength(20);

        builder.Property(r => r.ClinicName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.PreferredClinicCode).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Address).IsRequired().HasMaxLength(500);
        builder.Property(r => r.City).IsRequired().HasMaxLength(100);
        builder.Property(r => r.State).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Pincode).IsRequired().HasMaxLength(10);
        builder.Property(r => r.ClinicContactNumber).HasMaxLength(20);
        builder.Property(r => r.OfficialEmail).HasMaxLength(256);
        builder.Property(r => r.Website).HasMaxLength(256);

        builder.Property(r => r.DoctorName).IsRequired().HasMaxLength(150);
        builder.Property(r => r.MedicalRegistrationNumber).IsRequired().HasMaxLength(50);
        builder.Property(r => r.MedicalCouncil).IsRequired().HasMaxLength(150);
        builder.Property(r => r.ExpectedStaffCount).IsRequired();
        builder.Property(r => r.ReferralSource).HasMaxLength(100);
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.Property(r => r.Status).IsRequired().HasConversion<int>();

        builder.Property(r => r.ApprovedPlanId);
        builder.Property(r => r.ApprovedPlanCode).HasConversion<int?>();
        builder.Property(r => r.PlanListPrice).HasPrecision(18, 2);
        builder.Property(r => r.ApprovedAmount).HasPrecision(18, 2);
        builder.Property(r => r.AmountReason).HasMaxLength(500);
        builder.Property(r => r.IsTrialGrant).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.ApprovedClinicCode).HasMaxLength(20);

        builder.Property(r => r.ReviewedByAdminId);
        builder.Property(r => r.ReviewedByAdminEmail).HasMaxLength(256);
        builder.Property(r => r.ReviewedAt);
        builder.Property(r => r.ReviewNote).HasMaxLength(1000);
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);

        builder.Property(r => r.CurrentPaymentId);
        builder.Property(r => r.ProvisionedApplicationId);
        builder.Property(r => r.ClinicSubscriptionId);
        builder.Property(r => r.ActivatedAt);
        builder.Property(r => r.LastPaymentCheckAt);

        builder.Property(r => r.NeedsAttention).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.AttentionReason).HasMaxLength(2000);

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.UpdatedBy).HasMaxLength(256);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasIndex(r => r.RequestedByUserId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.NeedsAttention);
    }
}
