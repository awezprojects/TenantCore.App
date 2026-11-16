using FluentAssertions;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Translators;

public class IpdBedTransferHistoryTranslatorTests
{
    private static IpdBedTransferHistory BuildSegment(out Bed bed, out Room room, out Ward ward)
    {
        var applicationId = Guid.NewGuid();
        ward = Ward.Create(applicationId, "Ward A", null);
        room = Room.Create(applicationId, ward.Id, "101", "General", 500m);
        bed = Bed.Create(applicationId, ward.Id, room.Id, "B1");
        var segment = IpdBedTransferHistory.Open(applicationId, Guid.NewGuid(), bed.Id, room.Id, ward.Id, 500m, Guid.NewGuid(), "Initial allocation");
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Bed))!.SetValue(segment, bed);
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Room))!.SetValue(segment, room);
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Ward))!.SetValue(segment, ward);
        return segment;
    }

    [Fact]
    public void ToDto_ValidEntity_MapsAllProperties()
    {
        var segment = BuildSegment(out var bed, out var room, out var ward);

        var dto = IpdBedTransferHistoryTranslator.ToDto(segment);

        dto.Id.Should().Be(segment.Id);
        dto.BedId.Should().Be(bed.Id);
        dto.WardName.Should().Be(ward.Name);
        dto.RoomNumber.Should().Be(room.RoomNumber);
        dto.BedNumber.Should().Be(bed.BedNumber);
        dto.RatePerDayAtTime.Should().Be(500m);
        dto.OccupiedTo.Should().BeNull();
        dto.Notes.Should().Be("Initial allocation");
        dto.StandardChargeAmount.Should().BeNull();
        dto.AppliedChargeAmount.Should().BeNull();
        dto.AdjustmentType.Should().BeNull();
        dto.WriteOffAmount.Should().BeNull();
    }

    [Fact]
    public void ToDto_SegmentWithFrozenOverride_MapsShiftChargingFields()
    {
        // OBS-25 (14-Nov QA): bed-history now carries the same shift-charging fields the bill
        // endpoints already exposed.
        var segment = BuildSegment(out _, out _, out _);
        segment.FreezeCharge(2500m, 300m, IpdBillAdjustmentType.Grace);

        var dto = IpdBedTransferHistoryTranslator.ToDto(segment);

        dto.StandardChargeAmount.Should().Be(2500m);
        dto.AppliedChargeAmount.Should().Be(300m);
        dto.AdjustmentType.Should().Be(IpdBillAdjustmentType.Grace);
        dto.WriteOffAmount.Should().Be(2200m);
    }

    [Fact]
    public void ToDtoList_MultipleEntities_MapsAll()
    {
        var first = BuildSegment(out _, out _, out _);
        var second = BuildSegment(out _, out _, out _);

        var result = IpdBedTransferHistoryTranslator.ToDtoList([first, second]);

        result.Should().HaveCount(2);
    }
}
