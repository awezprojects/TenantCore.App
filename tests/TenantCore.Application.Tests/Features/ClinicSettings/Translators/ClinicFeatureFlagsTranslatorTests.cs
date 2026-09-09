using FluentAssertions;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Entities;

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
    public void ToDto_BillingEnabledTrue_MapsTrue()
    {
        var appId = Guid.NewGuid();
        var entity = ClinicFeatureFlags.Create(appId, true, true);

        var dto = ClinicFeatureFlagsTranslator.ToDto(entity);

        dto.BillingEnabled.Should().BeTrue();
    }
}
