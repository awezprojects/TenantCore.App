using FluentAssertions;
using TenantCore.Application.Features.CounterSessions.Commands;
using TenantCore.Application.Features.CounterSessions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.CounterSessions.Translators;

public class CounterSessionTranslatorTests
{
    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var appId = Guid.NewGuid();
        var openedBy = Guid.NewGuid();
        var command = new OpenCounterSessionCommand(
            new OpenCounterSessionRequest { SessionDate = DateTime.Today }, openedBy, appId);

        var entity = CounterSessionTranslator.ToEntity(command);

        entity.ApplicationId.Should().Be(appId);
        entity.OpenedByUserId.Should().Be(openedBy);
        entity.SessionDate.Should().Be(DateTime.Today);
        entity.Status.Should().Be(CounterSessionStatus.Open);
    }

    [Fact]
    public void ToDto_MapsAllFields()
    {
        var appId = Guid.NewGuid();
        var entity = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);

        var dto = CounterSessionTranslator.ToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.ApplicationId.Should().Be(appId);
        dto.Status.Should().Be(CounterSessionStatus.Open);
    }

    [Fact]
    public void ToSummaryDto_MapsAllFields()
    {
        var appId = Guid.NewGuid();
        var entity = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);

        var dto = CounterSessionTranslator.ToSummaryDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.Status.Should().Be(CounterSessionStatus.Open);
    }
}
