using FluentAssertions;
using TenantCore.Application.Features.VitalPresets.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.VitalPresets.Translators;

public class VitalPresetLookupItemTranslatorTests
{
    [Fact]
    public void ToDto_GlobalItem_MapsIsGlobalTrue()
    {
        var entity = VitalPresetLookupItem.CreateGlobal(VitalFieldType.Bp, "120/80");

        var dto = VitalPresetLookupItemTranslator.ToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.VitalField.Should().Be(VitalFieldType.Bp);
        dto.Value.Should().Be("120/80");
        dto.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ClinicItem_MapsIsGlobalFalse()
    {
        var appId = Guid.NewGuid();
        var entity = VitalPresetLookupItem.CreateForClinic(appId, VitalFieldType.Pulse, "75");

        var dto = VitalPresetLookupItemTranslator.ToDto(entity);

        dto.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void ToDtoList_MapsEveryEntity()
    {
        var entities = new[]
        {
            VitalPresetLookupItem.CreateGlobal(VitalFieldType.Bp, "120/80"),
            VitalPresetLookupItem.CreateGlobal(VitalFieldType.Pulse, "72")
        };

        var dtos = VitalPresetLookupItemTranslator.ToDtoList(entities).ToList();

        dtos.Should().HaveCount(2);
    }
}
