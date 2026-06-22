using FluentAssertions;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.AmountHandovers.Translators;

public class AmountHandoverTranslatorTests
{
    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var appId = Guid.NewGuid();
        var handedOverBy = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var toUser = Guid.NewGuid();

        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = sessionId,
                HandedOverToUserId = toUser,
                HandedOverToRoleName = "Doctor",
                Amount = 1500,
                Notes = "end of day"
            },
            handedOverBy, appId);

        var entity = AmountHandoverTranslator.ToEntity(command);

        entity.ApplicationId.Should().Be(appId);
        entity.CounterSessionId.Should().Be(sessionId);
        entity.HandedOverByUserId.Should().Be(handedOverBy);
        entity.HandedOverToUserId.Should().Be(toUser);
        entity.HandedOverToRoleName.Should().Be("Doctor");
        entity.Amount.Should().Be(1500);
        entity.Notes.Should().Be("end of day");
        entity.Status.Should().Be(HandoverStatus.Pending);
    }

    [Fact]
    public void ToDto_MapsAllFields()
    {
        var appId = Guid.NewGuid();
        var entity = AmountHandover.Create(appId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Doctor", 2000, "test");

        var dto = AmountHandoverTranslator.ToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.Amount.Should().Be(2000);
        dto.Status.Should().Be(HandoverStatus.Pending);
    }

    [Fact]
    public void ToSummaryDto_MapsKeyFields()
    {
        var appId = Guid.NewGuid();
        var entity = AmountHandover.Create(appId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Doctor", 500, null);

        var dto = AmountHandoverTranslator.ToSummaryDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.Amount.Should().Be(500);
        dto.Status.Should().Be(HandoverStatus.Pending);
    }
}
