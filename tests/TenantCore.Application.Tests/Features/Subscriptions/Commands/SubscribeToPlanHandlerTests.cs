using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace TenantCore.Application.Tests.Features.Subscriptions.Commands;

public class SubscribeToPlanHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IClinicSubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IAuthApplicationService> _authApplicationService = new();
    private readonly Mock<ILogger<SubscribeToPlanHandler>> _logger = new();

    private SubscribeToPlanHandler CreateHandler() =>
        new(_planRepository.Object, _subscriptionRepository.Object, _authApplicationService.Object, _logger.Object);

    private static SubscriptionPlan CreatePlan(bool isTrial = false, int durationDays = 30, decimal price = 999m) =>
        SubscriptionPlan.CreateForSeed(
            Guid.NewGuid(), isTrial ? SubscriptionPlanCode.Trial : SubscriptionPlanCode.Monthly,
            isTrial ? "Free Trial" : "Monthly", "desc", durationDays, price, "INR",
            isTrial: isTrial, isPopular: false, displayOrder: 1);

    private void SetupAuthServices(Guid applicationId, Guid userId)
    {
        _authApplicationService
            .Setup(s => s.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationResponseDto { ApplicationId = applicationId, ApplicationName = "Sunrise Clinic" });

        _authApplicationService
            .Setup(s => s.GetApplicationUsersAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ApplicationUserResponseDto { UserId = userId, FullName = "Dr. Admin", EmailId = "admin@sunrise.test" }
            ]);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesActiveSubscriptionWithDatesFromPlanDuration()
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = CreatePlan(durationDays: 90, price: 2499m);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _subscriptionRepository.Setup(r => r.GetLatestForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _subscriptionRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupAuthServices(applicationId, userId);

        var handler = CreateHandler();
        var command = new SubscribeToPlanCommand(applicationId, plan.Id, userId);

        var before = DateTime.UtcNow;
        var result = await handler.Handle(command, CancellationToken.None);
        var after = DateTime.UtcNow;

        result.Status.Should().Be(SubscriptionStatus.Active);
        result.DurationDays.Should().Be(90);
        result.PricePaid.Should().Be(2499m);
        result.StartDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        (result.EndDate - result.StartDate).Days.Should().Be(90);
        result.ClinicName.Should().Be("Sunrise Clinic");
        result.BillingContactEmail.Should().Be("admin@sunrise.test");
        result.BillingContactName.Should().Be("Dr. Admin");

        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
        _subscriptionRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PlanNotFound_ThrowsNotFoundException()
    {
        _planRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPlan?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new SubscribeToPlanCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InactivePlan_ThrowsNotFoundException()
    {
        var plan = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999m, "INR", false, false, 1);
        // CreateForSeed always sets IsActive = true — simulate a deactivated plan the way the repository would return one.
        typeof(SubscriptionPlan).GetProperty(nameof(SubscriptionPlan.IsActive))!.SetValue(plan, false);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var handler = CreateHandler();
        var action = () => handler.Handle(new SubscribeToPlanCommand(Guid.NewGuid(), plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TrialAlreadyUsed_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan(isTrial: true);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var action = () => handler.Handle(new SubscribeToPlanCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TrialPreviouslyCancelled_StillThrowsInvalidOperationException()
    {
        // HasUsedTrialAsync is defined to return true for a prior trial of ANY status,
        // including Cancelled — this test locks in that the handler trusts the repository's
        // answer rather than re-checking status itself.
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan(isTrial: true);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var action = () => handler.Handle(new SubscribeToPlanCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_RenewalBeforeExpiry_StartsDayAfterCurrentSubscriptionEnds()
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var newPlan = CreatePlan(durationDays: 30);
        var existingPlan = CreatePlan(durationDays: 90);
        var existing = ClinicSubscription.Create(applicationId, existingPlan, DateTime.UtcNow.AddDays(-10), "Clinic", "a@b.com", "Admin");
        var expectedStart = existing.EndDate.AddDays(1);

        _planRepository.Setup(r => r.GetByIdAsync(newPlan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newPlan);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _subscriptionRepository.Setup(r => r.GetLatestForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _subscriptionRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupAuthServices(applicationId, userId);

        var handler = CreateHandler();
        var result = await handler.Handle(new SubscribeToPlanCommand(applicationId, newPlan.Id, userId), CancellationToken.None);

        result.StartDate.Should().Be(expectedStart);
        result.EndDate.Should().Be(expectedStart.AddDays(30));
    }

    [Fact]
    public async Task Handle_NoExistingSubscription_StartsImmediately()
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = CreatePlan();

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _subscriptionRepository.Setup(r => r.GetLatestForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _subscriptionRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupAuthServices(applicationId, userId);

        var handler = CreateHandler();

        var before = DateTime.UtcNow;
        var result = await handler.Handle(new SubscribeToPlanCommand(applicationId, plan.Id, userId), CancellationToken.None);
        var after = DateTime.UtcNow;

        result.StartDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task Handle_PriceAndDurationAlwaysTakenFromPlan_NeverFromCaller()
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = CreatePlan(durationDays: 365, price: 8999m);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _subscriptionRepository.Setup(r => r.GetLatestForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _subscriptionRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupAuthServices(applicationId, userId);

        var handler = CreateHandler();
        // SubscribeToPlanCommand carries no price/duration fields at all — this test
        // documents that the DTO shape itself makes client-supplied pricing impossible.
        var result = await handler.Handle(new SubscribeToPlanCommand(applicationId, plan.Id, userId), CancellationToken.None);

        result.PricePaid.Should().Be(8999m);
        result.DurationDays.Should().Be(365);
    }
}
