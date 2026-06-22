using FluentAssertions;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Application.Features.DoctorFeeConfigs.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.DoctorFeeConfigs.Translators;

public class DoctorFeeConfigTranslatorTests
{
    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var appId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();
        var command = new CreateDoctorFeeConfigCommand(
            new CreateDoctorFeeConfigRequest { DoctorProfileId = doctorProfileId, VisitFee = 500 }, appId);

        var entity = DoctorFeeConfigTranslator.ToEntity(command);

        entity.ApplicationId.Should().Be(appId);
        entity.DoctorProfileId.Should().Be(doctorProfileId);
        entity.VisitFee.Should().Be(500);
        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ToDto_MapsAllFields()
    {
        var appId = Guid.NewGuid();
        var entity = DoctorFeeConfig.Create(appId, Guid.NewGuid(), 750);

        var dto = DoctorFeeConfigTranslator.ToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.VisitFee.Should().Be(750);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ToSummaryDto_MapsAllFields()
    {
        var appId = Guid.NewGuid();
        var entity = DoctorFeeConfig.Create(appId, Guid.NewGuid(), 600);

        var dto = DoctorFeeConfigTranslator.ToSummaryDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.VisitFee.Should().Be(600);
    }
}
