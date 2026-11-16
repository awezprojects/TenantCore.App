using FluentAssertions;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.ClinicSettings.Translators;

public class ClinicFeatureFlagsTranslatorTests
{
    [Fact]
    public void ToDto_ValidEntity_MapsAllProperties()
    {
        var appId = Guid.NewGuid();
        var entity = ClinicFeatureFlags.Create(appId, false, false);

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.ApplicationId.Should().Be(entity.ApplicationId);
        dto.PrepaidOpdEnabled.Should().Be(entity.PrepaidOpdEnabled);
        dto.BillingEnabled.Should().Be(entity.BillingEnabled);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDto_IpdBillingSettingsNotConfigured_MapsAsNull()
    {
        var entity = ClinicFeatureFlags.Create(Guid.NewGuid());

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.IpdRoomBillingCycle.Should().BeNull();
        dto.IpdMidStayChargeMode.Should().BeNull();
    }

    [Fact]
    public void ToDto_IpdBillingSettingsConfigured_MapsValues()
    {
        var entity = ClinicFeatureFlags.Create(Guid.NewGuid());
        entity.Update(true, true, RoomBillingCycle.Per24HourBlock, MidStayBedChangeChargeMode.Prorated);

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.IpdRoomBillingCycle.Should().Be(RoomBillingCycle.Per24HourBlock);
        dto.IpdMidStayChargeMode.Should().Be(MidStayBedChangeChargeMode.Prorated);
    }

    [Fact]
    public void ToDto_BillingEnabledTrue_MapsTrue()
    {
        var appId = Guid.NewGuid();
        var entity = ClinicFeatureFlags.Create(appId, true, true);

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.BillingEnabled.Should().BeTrue();
    }

    [Fact]
    public void ToDto_NewClinic_MapsShiftChargingDefaultsThatReproduceOldBehavior()
    {
        // DEF-IPD-16: these four must default to values that change nothing for a clinic that
        // never touches the new settings.
        var entity = ClinicFeatureFlags.Create(Guid.NewGuid());

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.IpdShiftGraceWindowMinutes.Should().Be(0);
        dto.IpdShiftGraceRule.Should().BeNull();
        dto.IpdManualShiftChargeMode.Should().Be(IpdManualShiftChargeMode.Off);
        dto.IpdFullDayShiftRateRule.Should().Be(IpdFullDayShiftRateRule.DestinationRoomRate);
    }

    [Fact]
    public void ToDto_ShiftChargingSettingsConfigured_MapsValues()
    {
        var entity = ClinicFeatureFlags.Create(Guid.NewGuid());
        entity.UpdateShiftChargingSettings(120, IpdShiftGraceRule.LowerRate, IpdManualShiftChargeMode.AdminOnly, IpdFullDayShiftRateRule.HigherRate);

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.IpdShiftGraceWindowMinutes.Should().Be(120);
        dto.IpdShiftGraceRule.Should().Be(IpdShiftGraceRule.LowerRate);
        dto.IpdManualShiftChargeMode.Should().Be(IpdManualShiftChargeMode.AdminOnly);
        dto.IpdFullDayShiftRateRule.Should().Be(IpdFullDayShiftRateRule.HigherRate);
    }

    [Fact]
    public void ToDto_NewClinic_MapsCancelWindowAsUnlimitedByDefault()
    {
        // DEF-IPD-26 (15-Nov).
        var entity = ClinicFeatureFlags.Create(Guid.NewGuid());

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.IpdCancelWindowMinutes.Should().Be(0);
    }

    [Fact]
    public void ToDto_CancelWindowConfigured_MapsValue()
    {
        var entity = ClinicFeatureFlags.Create(Guid.NewGuid());
        entity.UpdateCancelWindow(60);

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.IpdCancelWindowMinutes.Should().Be(60);
    }
}
