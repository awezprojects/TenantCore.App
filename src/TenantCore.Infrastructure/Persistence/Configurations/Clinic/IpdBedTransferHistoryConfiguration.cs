using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class IpdBedTransferHistoryConfiguration : IEntityTypeConfiguration<IpdBedTransferHistory>
{
    public void Configure(EntityTypeBuilder<IpdBedTransferHistory> builder)
    {
        builder.ToTable("IpdBedTransferHistories", "clinic");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.ApplicationId).IsRequired();
        builder.Property(h => h.IpdRegistrationId).IsRequired();
        builder.HasIndex(h => new { h.ApplicationId, h.IpdRegistrationId });

        builder.Property(h => h.BedId).IsRequired();
        builder.Property(h => h.RoomId).IsRequired();
        builder.Property(h => h.WardId).IsRequired();
        builder.Property(h => h.RatePerDayAtTime).IsRequired().HasPrecision(18, 2);
        builder.Property(h => h.OccupiedFrom).IsRequired();
        builder.Property(h => h.TransferredByUserId).IsRequired();
        builder.Property(h => h.Notes).HasMaxLength(500);

        // DEF-IPD-16 (13-Nov): all nullable — only set when a shift actually closes this segment
        // with a grace or manual adjustment in play; a plain shift, a discharge/cancel close, or
        // any segment closed before this shipped, leaves them all null.
        builder.Property(h => h.StandardChargeAmount).HasPrecision(18, 2);
        builder.Property(h => h.AppliedChargeAmount).HasPrecision(18, 2);
        builder.Property(h => h.AdjustmentType);
        builder.Property(h => h.ManualChargeAmount).HasPrecision(18, 2);
        builder.Property(h => h.ManualChargeReason).HasMaxLength(500);
        builder.Property(h => h.ManualChargeByUserId);
        builder.Property(h => h.ManualChargeAt);
        // Computed in-memory from the two amounts above — not a column.
        builder.Ignore(h => h.WriteOffAmount);

        builder.Property(h => h.CreatedAt).IsRequired();
        builder.Property(h => h.CreatedBy).HasMaxLength(256);
        builder.Property(h => h.UpdatedBy).HasMaxLength(256);
        builder.Property(h => h.RowVersion).IsRowVersion();

        builder.HasOne(h => h.Bed).WithMany().HasForeignKey(h => h.BedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.Room).WithMany().HasForeignKey(h => h.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.Ward).WithMany().HasForeignKey(h => h.WardId).OnDelete(DeleteBehavior.Restrict);
    }
}
