using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Queries;

/// <summary>
/// The handler now delegates visibility and per-clinic pricing to IClinicPlanCatalog and only
/// applies the Trial "already used" rule on top. The catalogue's own rules are covered by
/// ClinicPlanCatalogTests.
/// </summary>
public class GetSubscriptionPlansHandlerTests
{
    private readonly Mock<IClinicPlanCatalog> _planCatalog = new();
    private readonly Mock<IClinicSubscriptionRepository> _subscriptionRepository = new();

    private GetSubscriptionPlansHandler CreateHandler()
        => new(_planCatalog.Object, _subscriptionRepository.Object);

    private void SetUpCatalogue(Guid applicationId, params ClinicPlanOption[] options)
        => _planCatalog.Setup(c => c.GetPlansForClinicAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(options);

    private static ClinicPlanOption Option(SubscriptionPlan plan, decimal? effectivePrice = null, bool isSpecialOffer = false, DateTime? validUntil = null)
        => new(plan, effectivePrice ?? plan.Price, isSpecialOffer, validUntil);

    [Fact]
    public async Task Handle_FourSeededPlans_ReturnedInCatalogueOrder()
    {
        var applicationId = Guid.NewGuid();
        var trial = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Trial", "d", 14, 0, "INR", true, false, 1);
        var monthly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999, "INR", false, false, 2);
        var quarterly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Quarterly, "Quarterly", "d", 90, 2499, "INR", false, true, 3);
        var yearly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Yearly, "Yearly", "d", 365, 8999, "INR", false, false, 4);

        SetUpCatalogue(applicationId, Option(trial), Option(monthly), Option(quarterly), Option(yearly));
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = (await CreateHandler().Handle(new GetSubscriptionPlansQuery(applicationId), CancellationToken.None)).ToList();

        result.Should().HaveCount(4);
        result.Select(p => p.Code).Should().ContainInOrder(
            SubscriptionPlanCode.Trial, SubscriptionPlanCode.Monthly, SubscriptionPlanCode.Quarterly, SubscriptionPlanCode.Yearly);
    }

    [Fact]
    public async Task Handle_NoVisiblePlans_ReturnsEmptyList()
    {
        var applicationId = Guid.NewGuid();
        SetUpCatalogue(applicationId);
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateHandler().Handle(new GetSubscriptionPlansQuery(applicationId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ClinicHasAnySubscriptionHistory_TrialPlanMarkedAlreadyUsed()
    {
        // AlreadyUsed on the Trial card reflects ANY subscription history, not just a prior trial —
        // a clinic that came through onboarding always has history (its trial or its paid plan)
        // by the time it can reach this page.
        var applicationId = Guid.NewGuid();
        var trial = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Trial", "d", 14, 0, "INR", true, false, 1);
        var monthly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999, "INR", false, false, 2);

        SetUpCatalogue(applicationId, Option(trial), Option(monthly));
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = (await CreateHandler().Handle(new GetSubscriptionPlansQuery(applicationId), CancellationToken.None)).ToList();

        result.Single(p => p.Code == SubscriptionPlanCode.Trial).AlreadyUsed.Should().BeTrue();
        // AlreadyUsed only ever applies to the Trial card — paid plans are never flagged by this rule.
        result.Single(p => p.Code == SubscriptionPlanCode.Monthly).AlreadyUsed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ClinicHasNoSubscriptionHistory_TrialPlanNotMarkedAlreadyUsed()
    {
        var applicationId = Guid.NewGuid();
        var trial = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Trial", "d", 14, 0, "INR", true, false, 1);

        SetUpCatalogue(applicationId, Option(trial));
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = (await CreateHandler().Handle(new GetSubscriptionPlansQuery(applicationId), CancellationToken.None)).ToList();

        result.Single(p => p.Code == SubscriptionPlanCode.Trial).AlreadyUsed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SpecialOffer_SurfacesOfferPriceAndKeepsListPrice()
    {
        var applicationId = Guid.NewGuid();
        var monthly = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999, "INR", false, false, 2);
        var validUntil = DateTime.UtcNow.AddDays(30);

        SetUpCatalogue(applicationId, Option(monthly, effectivePrice: 499m, isSpecialOffer: true, validUntil: validUntil));
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionHistoryAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = (await CreateHandler().Handle(new GetSubscriptionPlansQuery(applicationId), CancellationToken.None)).ToList();

        var dto = result.Single();
        dto.Price.Should().Be(499m, "the clinic pays its offer price");
        dto.ListPrice.Should().Be(999m, "the catalogue price is kept so the UI can show the saving");
        dto.IsSpecialOffer.Should().BeTrue();
        dto.OfferValidUntil.Should().Be(validUntil);
    }
}
