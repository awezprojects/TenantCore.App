using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Queries;

public class GetSubscriptionStatusHandlerTests
{
    private readonly Mock<IClinicSubscriptionRepository> _repository = new();

    private static SubscriptionPlan CreatePlan(int durationDays = 30) =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", durationDays, 999m, "INR", false, false, 1);

    [Fact]
    public async Task Handle_SubscriptionExpiringToday_DaysRemainingIsZero()
    {
        var applicationId = Guid.NewGuid();
        var subscription = ClinicSubscription.Create(applicationId, CreatePlan(durationDays: 1), DateTime.UtcNow.Date.AddDays(-1), "C", "a@b.com", "A");
        // EndDate = start + 1 day = today.

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new GetSubscriptionStatusHandler(_repository.Object);
        var result = await handler.Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: true), CancellationToken.None);

        result.DaysRemaining.Should().Be(0);
        result.HasActiveSubscription.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SubscriptionExpiringTomorrow_DaysRemainingIsOne()
    {
        var applicationId = Guid.NewGuid();
        var subscription = ClinicSubscription.Create(applicationId, CreatePlan(durationDays: 1), DateTime.UtcNow.Date, "C", "a@b.com", "A");
        // EndDate = today + 1 day = tomorrow.

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new GetSubscriptionStatusHandler(_repository.Object);
        var result = await handler.Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: true), CancellationToken.None);

        result.DaysRemaining.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoActiveSubscription_HasActiveSubscriptionIsFalse()
    {
        var applicationId = Guid.NewGuid();
        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new GetSubscriptionStatusHandler(_repository.Object);
        var result = await handler.Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: false), CancellationToken.None);

        result.HasActiveSubscription.Should().BeFalse();
        result.DaysRemaining.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExpiringSoonThreshold_FlipsAtFifteenDays()
    {
        var applicationId = Guid.NewGuid();
        var justInside = ClinicSubscription.Create(applicationId, CreatePlan(durationDays: 15), DateTime.UtcNow.Date, "C", "a@b.com", "A");
        var justOutside = ClinicSubscription.Create(applicationId, CreatePlan(durationDays: 16), DateTime.UtcNow.Date, "C", "a@b.com", "A");

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(justInside);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new GetSubscriptionStatusHandler(_repository.Object);
        (await handler.Handle(new GetSubscriptionStatusQuery(applicationId, true), CancellationToken.None)).IsExpiringSoon.Should().BeTrue();

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(justOutside);
        (await handler.Handle(new GetSubscriptionStatusQuery(applicationId, true), CancellationToken.None)).IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CanSubscribe_ReflectsIsClinicAdminFlagPassedIn()
    {
        var applicationId = Guid.NewGuid();
        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new GetSubscriptionStatusHandler(_repository.Object);

        (await handler.Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: true), CancellationToken.None)).CanSubscribe.Should().BeTrue();
        (await handler.Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: false), CancellationToken.None)).CanSubscribe.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ClinicHasUsedTrial_ReturnsHasUsedTrialTrue()
    {
        var applicationId = Guid.NewGuid();
        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new GetSubscriptionStatusHandler(_repository.Object);
        var result = await handler.Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: true), CancellationToken.None);

        result.HasUsedTrial.Should().BeTrue();
    }
}
