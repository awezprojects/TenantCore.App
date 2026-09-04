using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Queries;

public class GetSubscriptionPlansHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IClinicSubscriptionRepository> _subscriptionRepository = new();

    [Fact]
    public async Task Handle_FourSeededPlans_ReturnedInDisplayOrder()
    {
        var applicationId = Guid.NewGuid();
        var trial = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Trial", "d", 14, 0, "INR", true, false, 1);
        var yearly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Yearly, "Yearly", "d", 365, 8999, "INR", false, false, 4);
        var quarterly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Quarterly, "Quarterly", "d", 90, 2499, "INR", false, true, 3);
        var monthly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999, "INR", false, false, 2);

        // GetActivePlansAsync is the real repository's ordering point (ORDER BY DisplayOrder);
        // the mock returns them already ordered, matching what that query produces.
        _planRepository.Setup(r => r.GetActivePlansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([trial, monthly, quarterly, yearly]);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new GetSubscriptionPlansHandler(_planRepository.Object, _subscriptionRepository.Object);
        var result = (await handler.Handle(new GetSubscriptionPlansQuery(applicationId), CancellationToken.None)).ToList();

        result.Should().HaveCount(4);
        result.Select(p => p.Code).Should().ContainInOrder(
            SubscriptionPlanCode.Trial, SubscriptionPlanCode.Monthly, SubscriptionPlanCode.Quarterly, SubscriptionPlanCode.Yearly);
    }

    [Fact]
    public async Task Handle_InactivePlansExcludedByRepository_HandlerReturnsOnlyWhatRepositoryGives()
    {
        // GetActivePlansAsync already filters IsActive on the repository side — the handler
        // trusts that and does no additional filtering, so an empty result passes straight through.
        var applicationId = Guid.NewGuid();
        _planRepository.Setup(r => r.GetActivePlansAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new GetSubscriptionPlansHandler(_planRepository.Object, _subscriptionRepository.Object);
        var result = await handler.Handle(new GetSubscriptionPlansQuery(applicationId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ClinicHasUsedTrial_TrialPlanMarkedAlreadyUsed()
    {
        var applicationId = Guid.NewGuid();
        var trial = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Trial", "d", 14, 0, "INR", true, false, 1);
        var monthly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999, "INR", false, false, 2);

        _planRepository.Setup(r => r.GetActivePlansAsync(It.IsAny<CancellationToken>())).ReturnsAsync([trial, monthly]);
        _subscriptionRepository.Setup(r => r.HasUsedTrialAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new GetSubscriptionPlansHandler(_planRepository.Object, _subscriptionRepository.Object);
        var result = (await handler.Handle(new GetSubscriptionPlansQuery(applicationId), CancellationToken.None)).ToList();

        result.Single(p => p.Code == SubscriptionPlanCode.Trial).AlreadyUsed.Should().BeTrue();
        result.Single(p => p.Code == SubscriptionPlanCode.Monthly).AlreadyUsed.Should().BeFalse();
    }
}
