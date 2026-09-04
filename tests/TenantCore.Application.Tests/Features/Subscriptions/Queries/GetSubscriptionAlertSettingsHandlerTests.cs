using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Queries;

public class GetSubscriptionAlertSettingsHandlerTests
{
    private readonly Mock<ISubscriptionAlertSettingRepository> _repository = new();

    [Fact]
    public async Task Handle_SettingsIncludingDisabled_ReturnedInDisplayOrder()
    {
        var reminder10 = SubscriptionAlertSetting.CreateForSeed(Guid.NewGuid(), SubscriptionAlertType.ExpiryReminder, 10, "s", "h", "b", 1);
        var reminder5 = SubscriptionAlertSetting.CreateForSeed(Guid.NewGuid(), SubscriptionAlertType.ExpiryReminder, 5, "s", "h", "b", 2);
        reminder5.Apply("s", "h", "b", isEnabled: false, displayOrder: 2);

        // GetAllOrderedAsync orders by DisplayOrder on the repository side.
        _repository.Setup(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>())).ReturnsAsync([reminder10, reminder5]);

        var handler = new GetSubscriptionAlertSettingsHandler(_repository.Object);
        var result = (await handler.Handle(new GetSubscriptionAlertSettingsQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].DaysBeforeExpiry.Should().Be(10);
        result[1].DaysBeforeExpiry.Should().Be(5);
        result[1].IsEnabled.Should().BeFalse();
    }
}
