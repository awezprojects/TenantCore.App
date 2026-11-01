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

/// <summary>
/// This free-activation endpoint now only ever grants the Trial plan, and only to a clinic with
/// no subscription history at all (clinic-trial-razorpay-subscriptions). Paid plans are rejected
/// outright — they're activated exclusively through a confirmed Razorpay payment link.
/// </summary>
public class SubscribeToPlanHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IClinicSubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IClinicAccountRepository> _accountRepository = new();
    private readonly Mock<IAuthApplicationService> _authApplicationService = new();
    private readonly Mock<ILogger<SubscribeToPlanHandler>> _logger = new();

    public SubscribeToPlanHandlerTests()
        => _accountRepository.Setup(r => r.IsSuspendedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

    private SubscribeToPlanHandler CreateHandler() =>
        new(_planRepository.Object, _subscriptionRepository.Object, _accountRepository.Object,
            _authApplicationService.Object, _logger.Object);

    [Fact]
    public async Task Handle_SuspendedClinic_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        _accountRepository.Setup(r => r.IsSuspendedAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => CreateHandler().Handle(
            new SubscribeToPlanCommand(applicationId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*suspended*");
        _planRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

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
    public async Task Handle_TrialPlanForClinicWithNoHistory_CreatesActiveTrialSubscription()
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = CreatePlan(isTrial: true, durationDays: 14, price: 0m);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _subscriptionRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupAuthServices(applicationId, userId);

        var handler = CreateHandler();
        var command = new SubscribeToPlanCommand(applicationId, plan.Id, userId);

        var before = DateTime.UtcNow;
        var result = await handler.Handle(command, CancellationToken.None);
        var after = DateTime.UtcNow;

        result.Status.Should().Be(SubscriptionStatus.Active);
        result.DurationDays.Should().Be(14);
        result.PricePaid.Should().Be(0m);
        result.StartDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.ClinicName.Should().Be("Sunrise Clinic");
        result.BillingContactEmail.Should().Be("admin@sunrise.test");
        result.BillingContactName.Should().Be("Dr. Admin");

        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
        _subscriptionRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PaidPlan_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan(isTrial: false, price: 999m);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var handler = CreateHandler();
        var action = () => handler.Handle(new SubscribeToPlanCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        // A paid plan is rejected purely from the plan's own IsTrial flag — no history lookup needed.
        _subscriptionRepository.Verify(r => r.HasAnySubscriptionHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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
        var plan = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Trial", "d", 14, 0m, "INR", true, false, 1);
        // CreateForSeed always sets IsActive = true — simulate a deactivated plan the way the repository would return one.
        typeof(SubscriptionPlan).GetProperty(nameof(SubscriptionPlan.IsActive))!.SetValue(plan, false);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var handler = CreateHandler();
        var action = () => handler.Handle(new SubscribeToPlanCommand(Guid.NewGuid(), plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ClinicAlreadyHasSubscriptionHistory_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan(isTrial: true, price: 0m);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var action = () => handler.Handle(new SubscribeToPlanCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HistoryFromAnyPastStatusStillBlocksTheTrial()
    {
        // HasAnySubscriptionHistoryAsync is defined to return true for ANY prior subscription row
        // regardless of status (even a cancelled one) — the handler trusts that answer as-is.
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan(isTrial: true, price: 0m);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var action = () => handler.Handle(new SubscribeToPlanCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_PriceAndDurationAlwaysTakenFromPlan_NeverFromCaller()
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = CreatePlan(isTrial: true, durationDays: 21, price: 0m);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _subscriptionRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupAuthServices(applicationId, userId);

        var handler = CreateHandler();
        // SubscribeToPlanCommand carries no price/duration fields at all — this test documents
        // that the DTO shape itself makes client-supplied pricing impossible.
        var result = await handler.Handle(new SubscribeToPlanCommand(applicationId, plan.Id, userId), CancellationToken.None);

        result.PricePaid.Should().Be(0m);
        result.DurationDays.Should().Be(21);
    }
}
