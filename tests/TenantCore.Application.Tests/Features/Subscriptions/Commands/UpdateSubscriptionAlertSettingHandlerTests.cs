using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Commands;

public class UpdateSubscriptionAlertSettingHandlerTests
{
    private readonly Mock<ISubscriptionAlertSettingRepository> _repository = new();

    [Fact]
    public async Task Handle_ExistingSetting_UpdatesFieldsAndSaves()
    {
        var setting = SubscriptionAlertSetting.CreateForSeed(
            Guid.NewGuid(), SubscriptionAlertType.ExpiryReminder, 10, "old subject", "old headline", "old body", 1);

        var request = new UpdateSubscriptionAlertSettingRequest
        {
            Subject = "new subject",
            Headline = "new headline",
            BodyMessage = "new body",
            IsEnabled = false,
            DisplayOrder = 5
        };

        _repository.Setup(r => r.GetByIdAsync(setting.Id, It.IsAny<CancellationToken>())).ReturnsAsync(setting);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateSubscriptionAlertSettingHandler(_repository.Object);
        var result = await handler.Handle(new UpdateSubscriptionAlertSettingCommand(setting.Id, request), CancellationToken.None);

        result.Subject.Should().Be("new subject");
        result.Headline.Should().Be("new headline");
        result.BodyMessage.Should().Be("new body");
        result.IsEnabled.Should().BeFalse();
        result.DisplayOrder.Should().Be(5);

        _repository.Verify(r => r.Update(setting), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionAlertSetting?)null);

        var handler = new UpdateSubscriptionAlertSettingHandler(_repository.Object);
        var action = () => handler.Handle(
            new UpdateSubscriptionAlertSettingCommand(Guid.NewGuid(), new UpdateSubscriptionAlertSettingRequest()),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
