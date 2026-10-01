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
    private readonly Mock<IClinicAccountRepository> _accountRepository = new();

    public GetSubscriptionStatusHandlerTests()
    {
        // Defaults: nothing queued, never suspended. Individual tests override as needed.
        _repository.Setup(r => r.GetUpcomingForClinicAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repository.Setup(r => r.GetCoverageEndAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicAccount?)null);
    }

    private GetSubscriptionStatusHandler CreateHandler()
        => new(_repository.Object, _accountRepository.Object);

    private static SubscriptionPlan CreatePlan(int durationDays = 30) =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", durationDays, 999m, "INR", false, false, 1);

    /// <summary>Keeps the coverage end consistent with the current term for the single-term tests.</summary>
    private void SetCoverage(Guid applicationId, DateTime? coverageEnd)
        => _repository.Setup(r => r.GetCoverageEndAsync(applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(coverageEnd);

    [Fact]
    public async Task Handle_SubscriptionExpiringToday_DaysRemainingIsZero()
    {
        var applicationId = Guid.NewGuid();
        var subscription = ClinicSubscription.Create(applicationId, CreatePlan(durationDays: 1), DateTime.UtcNow.Date.AddDays(-1), "C", "a@b.com", "A");
        // EndDate = start + 1 day = today.

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        SetCoverage(applicationId, subscription.EndDate);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: true), CancellationToken.None);

        result.DaysRemaining.Should().Be(0);
        result.HasActiveSubscription.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SubscriptionExpiringTomorrow_DaysRemainingIsOne()
    {
        var applicationId = Guid.NewGuid();
        var subscription = ClinicSubscription.Create(applicationId, CreatePlan(durationDays: 1), DateTime.UtcNow.Date, "C", "a@b.com", "A");

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        SetCoverage(applicationId, subscription.EndDate);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: true), CancellationToken.None);

        result.DaysRemaining.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoActiveSubscription_HasActiveSubscriptionIsFalse()
    {
        var applicationId = Guid.NewGuid();
        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: false), CancellationToken.None);

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
        SetCoverage(applicationId, justInside.EndDate);
        var handler = CreateHandler();
        (await handler.Handle(new GetSubscriptionStatusQuery(applicationId, true), CancellationToken.None)).IsExpiringSoon.Should().BeTrue();

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(justOutside);
        SetCoverage(applicationId, justOutside.EndDate);
        (await handler.Handle(new GetSubscriptionStatusQuery(applicationId, true), CancellationToken.None)).IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CanSubscribe_ReflectsIsClinicAdminFlagPassedIn()
    {
        var applicationId = Guid.NewGuid();
        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();

        (await handler.Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: true), CancellationToken.None)).CanSubscribe.Should().BeTrue();
        (await handler.Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: false), CancellationToken.None)).CanSubscribe.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ClinicHasUsedTrial_ReturnsHasUsedTrialTrue()
    {
        var applicationId = Guid.NewGuid();
        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(applicationId, IsClinicAdmin: true), CancellationToken.None);

        result.HasUsedTrial.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpcomingTermOnly_ClinicIsStillLocked()
    {
        // A term bought for later must not unlock the clinic before it starts.
        var applicationId = Guid.NewGuid();
        var upcoming = ClinicSubscription.Create(applicationId, CreatePlan(), DateTime.UtcNow.AddDays(10), "C", "a@b.com", "A");

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.GetUpcomingForClinicAsync(applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([upcoming]);
        SetCoverage(applicationId, upcoming.EndDate);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(applicationId, true), CancellationToken.None);

        result.HasActiveSubscription.Should().BeFalse();
        result.Upcoming.Should().HaveCount(1);
        result.Upcoming[0].StartDate.Should().Be(upcoming.StartDate);
    }

    [Fact]
    public async Task Handle_CurrentTermEndingSoonButNextAlreadyBought_IsNotExpiringSoon()
    {
        // The whole point of queuing an upgrade: the clinic is covered continuously, so it must
        // not be nagged to renew even though THIS term ends in days.
        var applicationId = Guid.NewGuid();
        var current = ClinicSubscription.Create(applicationId, CreatePlan(durationDays: 3), DateTime.UtcNow.Date, "C", "a@b.com", "A");
        var next = ClinicSubscription.Create(applicationId, CreatePlan(durationDays: 90), current.EndDate, "C", "a@b.com", "A");

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(current);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.GetUpcomingForClinicAsync(applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([next]);
        SetCoverage(applicationId, next.EndDate);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(applicationId, true), CancellationToken.None);

        result.DaysRemaining.Should().Be(3, "this term still ends in 3 days");
        result.IsExpiringSoon.Should().BeFalse("coverage runs well past the warning threshold");
        result.CoverageEndDate.Should().Be(next.EndDate);
        result.CoverageDaysRemaining.Should().BeGreaterThan(15);
    }

    [Fact]
    public async Task Handle_SuspendedClinic_ReturnsSuspensionFlagAndMessage()
    {
        var applicationId = Guid.NewGuid();
        var account = ClinicAccount.CreateDefault(applicationId);
        account.Suspend("Payment overdue", "admin@example.test");

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(applicationId, true), CancellationToken.None);

        result.IsSuspended.Should().BeTrue();
        result.SuspensionMessage.Should().Be("Payment overdue");
    }

    [Fact]
    public async Task Handle_ReactivatedClinic_DoesNotLeakTheOldSuspensionMessage()
    {
        var applicationId = Guid.NewGuid();
        var account = ClinicAccount.CreateDefault(applicationId);
        account.Suspend("Payment overdue", "admin@example.test");
        account.Reactivate("admin@example.test");

        _repository.Setup(r => r.GetActiveForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _repository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(applicationId, true), CancellationToken.None);

        result.IsSuspended.Should().BeFalse();
        result.SuspensionMessage.Should().BeNull();
    }
}
