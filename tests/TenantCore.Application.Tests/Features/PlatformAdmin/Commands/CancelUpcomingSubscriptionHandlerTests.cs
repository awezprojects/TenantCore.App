using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Commands;

public class CancelUpcomingSubscriptionHandlerTests
{
    private readonly Mock<IClinicSubscriptionRepository> _subscriptionRepository = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public CancelUpcomingSubscriptionHandlerTests()
    {
        _subscriptionRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _subscriptionRepository.Setup(r => r.GetUpcomingForClinicAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _subscriptionRepository.Setup(r => r.GetActiveForClinicAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicSubscription?)null);
    }

    private CancelUpcomingSubscriptionHandler CreateHandler()
        => new(_subscriptionRepository.Object, Mock.Of<ILogger<CancelUpcomingSubscriptionHandler>>());

    private ClinicSubscription Subscription(Guid applicationId, DateTime startDate, int durationDays = 30)
    {
        var plan = SubscriptionPlan.CreateCustom("Monthly", "d", durationDays, 999m, false, 1, true);
        return ClinicSubscription.Create(applicationId, plan, startDate, "Clinic", "billing@example.test", "Billing");
    }

    private CancelUpcomingSubscriptionCommand Command(Guid subscriptionId)
        => new(_applicationId, subscriptionId, "Doctor changed their mind", Guid.NewGuid(), "admin@example.test");

    [Fact]
    public async Task Handle_UpcomingTerm_CancelsIt()
    {
        var subscription = Subscription(_applicationId, DateTime.UtcNow.AddDays(10));
        _subscriptionRepository.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await CreateHandler().Handle(Command(subscription.Id), CancellationToken.None);

        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancellationReason.Should().Be("Doctor changed their mind");
        _subscriptionRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyStartedTerm_ThrowsInvalidOperationException()
    {
        var subscription = Subscription(_applicationId, DateTime.UtcNow.AddDays(-2));
        _subscriptionRepository.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var act = async () => await CreateHandler().Handle(Command(subscription.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_SubscriptionOfAnotherClinic_ThrowsNotFoundException()
    {
        var subscription = Subscription(Guid.NewGuid(), DateTime.UtcNow.AddDays(10));
        _subscriptionRepository.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var act = async () => await CreateHandler().Handle(Command(subscription.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_MissingSubscription_ThrowsNotFoundException()
    {
        _subscriptionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicSubscription?)null);

        var act = async () => await CreateHandler().Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_LaterTerms_AreRechainedToCloseTheGap()
    {
        var utcNow = DateTime.UtcNow;
        var currentEnd = utcNow.AddDays(10);

        var current = Subscription(_applicationId, utcNow.AddDays(-20), durationDays: 30);
        var cancelled = Subscription(_applicationId, currentEnd, durationDays: 30);      // starts at coverage end
        var later = Subscription(_applicationId, currentEnd.AddDays(30), durationDays: 30); // queued behind it

        _subscriptionRepository.Setup(r => r.GetByIdAsync(cancelled.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelled);
        _subscriptionRepository.Setup(r => r.GetActiveForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        _subscriptionRepository.Setup(r => r.GetUpcomingForClinicAsync(_applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([cancelled, later]);

        await CreateHandler().Handle(Command(cancelled.Id), CancellationToken.None);

        cancelled.Status.Should().Be(SubscriptionStatus.Cancelled);
        later.StartDate.Should().Be(current.EndDate, "the later term moves up to where the current one ends");
        later.EndDate.Should().Be(current.EndDate.AddDays(30), "it keeps its full 30-day duration");
    }

    [Fact]
    public async Task Handle_NoCurrentTerm_RechainsFromNow()
    {
        var utcNow = DateTime.UtcNow;
        var cancelled = Subscription(_applicationId, utcNow.AddDays(5), durationDays: 30);
        var later = Subscription(_applicationId, utcNow.AddDays(35), durationDays: 30);

        _subscriptionRepository.Setup(r => r.GetByIdAsync(cancelled.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelled);
        _subscriptionRepository.Setup(r => r.GetUpcomingForClinicAsync(_applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([cancelled, later]);

        await CreateHandler().Handle(Command(cancelled.Id), CancellationToken.None);

        later.StartDate.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(5));
    }
}
