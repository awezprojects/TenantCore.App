using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Subscriptions.Services;

public class ClinicPlanCatalogTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IClinicPlanOfferRepository> _offerRepository = new();
    private readonly Mock<IClinicAccountRepository> _accountRepository = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    private ClinicPlanCatalog CreateCatalog()
        => new(_planRepository.Object, _offerRepository.Object, _accountRepository.Object);

    private static SubscriptionPlan Plan(string name, decimal price, bool isPublic = true)
        => SubscriptionPlan.CreateCustom(name, "d", 30, price, false, 1, isPublic);

    private void SetUp(
        IReadOnlyList<SubscriptionPlan> plans,
        IReadOnlyList<ClinicPlanOffer>? offers = null,
        bool restricted = false)
    {
        _planRepository.Setup(r => r.GetActivePlansAsync(It.IsAny<CancellationToken>())).ReturnsAsync(plans);
        _offerRepository.Setup(r => r.GetLiveForClinicAsync(_applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(offers ?? []);

        var account = ClinicAccount.CreateDefault(_applicationId);
        if (restricted) account.SetRestrictToOfferedPlans(true);
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restricted ? account : null);
    }

    [Fact]
    public async Task GetPlansForClinic_PublicPlans_AreVisibleAtListPrice()
    {
        var plan = Plan("Monthly", 999m);
        SetUp([plan]);

        var options = await CreateCatalog().GetPlansForClinicAsync(_applicationId);

        options.Should().HaveCount(1);
        options[0].EffectivePrice.Should().Be(999m);
        options[0].IsSpecialOffer.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlansForClinic_PrivatePlanWithoutOffer_IsHidden()
    {
        SetUp([Plan("Private Package", 4999m, isPublic: false)]);

        var options = await CreateCatalog().GetPlansForClinicAsync(_applicationId);

        options.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlansForClinic_PrivatePlanWithOffer_IsVisibleAtOfferPrice()
    {
        var plan = Plan("Private Package", 4999m, isPublic: false);
        var offer = ClinicPlanOffer.Create(_applicationId, plan.Id, 3999m, null, null, "admin@example.test");
        SetUp([plan], [offer]);

        var options = await CreateCatalog().GetPlansForClinicAsync(_applicationId);

        options.Should().HaveCount(1);
        options[0].EffectivePrice.Should().Be(3999m);
        options[0].IsSpecialOffer.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlansForClinic_OfferWithoutPrice_GrantsVisibilityAtListPrice()
    {
        var plan = Plan("Private Package", 4999m, isPublic: false);
        var offer = ClinicPlanOffer.Create(_applicationId, plan.Id, null, null, null, "admin@example.test");
        SetUp([plan], [offer]);

        var options = await CreateCatalog().GetPlansForClinicAsync(_applicationId);

        options[0].EffectivePrice.Should().Be(4999m);
        options[0].IsSpecialOffer.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlansForClinic_RestrictedClinic_SeesOnlyItsOwnOffers()
    {
        var publicPlan = Plan("Monthly", 999m);
        var offeredPlan = Plan("Negotiated", 2499m);
        var offer = ClinicPlanOffer.Create(_applicationId, offeredPlan.Id, 1999m, null, null, "admin@example.test");
        SetUp([publicPlan, offeredPlan], [offer], restricted: true);

        var options = await CreateCatalog().GetPlansForClinicAsync(_applicationId);

        options.Should().HaveCount(1);
        options[0].Plan.Name.Should().Be("Negotiated");
    }

    [Fact]
    public async Task IsVisible_PlanNotInTheClinicsCatalogue_ReturnsFalse()
    {
        var hidden = Plan("Private Package", 4999m, isPublic: false);
        SetUp([hidden]);

        var visible = await CreateCatalog().IsVisibleAsync(_applicationId, hidden.Id);

        visible.Should().BeFalse();
    }

    [Fact]
    public async Task GetEffectivePrice_LiveOffer_ReturnsOfferPrice()
    {
        var plan = Plan("Monthly", 999m);
        var offer = ClinicPlanOffer.Create(_applicationId, plan.Id, 499m, DateTime.UtcNow.AddDays(30), null, "admin@example.test");
        _offerRepository.Setup(r => r.GetActiveForClinicAndPlanAsync(_applicationId, plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offer);

        var price = await CreateCatalog().GetEffectivePriceAsync(_applicationId, plan);

        price.Should().Be(499m);
    }

    [Fact]
    public async Task GetEffectivePrice_ExpiredOffer_FallsBackToListPrice()
    {
        var plan = Plan("Monthly", 999m);
        var expired = ClinicPlanOffer.Create(_applicationId, plan.Id, 499m, DateTime.UtcNow.AddDays(-1), null, "admin@example.test");
        _offerRepository.Setup(r => r.GetActiveForClinicAndPlanAsync(_applicationId, plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expired);

        var price = await CreateCatalog().GetEffectivePriceAsync(_applicationId, plan);

        price.Should().Be(999m);
    }

    [Fact]
    public async Task GetEffectivePrice_NoOffer_ReturnsListPrice()
    {
        var plan = Plan("Monthly", 999m);
        _offerRepository.Setup(r => r.GetActiveForClinicAndPlanAsync(_applicationId, plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicPlanOffer?)null);

        var price = await CreateCatalog().GetEffectivePriceAsync(_applicationId, plan);

        price.Should().Be(999m);
    }
}
