using FluentAssertions;
using Moq;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Commands;

public class ClinicPlanOfferHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IClinicPlanOfferRepository> _offerRepository = new();
    private readonly Mock<IClinicAccountRepository> _accountRepository = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public ClinicPlanOfferHandlerTests()
    {
        _offerRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _accountRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CreateClinicPlanOfferHandler CreateHandler()
        => new(_planRepository.Object, _offerRepository.Object);

    private WithdrawClinicPlanOfferHandler WithdrawHandler()
        => new(_offerRepository.Object);

    private SetClinicPlanVisibilityHandler VisibilityHandler()
        => new(_accountRepository.Object);

    private SubscriptionPlan SetUpPlan(bool isTrial = false, bool isActive = true)
    {
        var plan = isTrial
            ? SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Trial", "d", 14, 0m, "INR", true, false, 1)
            : SubscriptionPlan.CreateCustom("Monthly", "d", 30, 999m, false, 1, true);

        if (!isActive) plan.Deactivate();
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        return plan;
    }

    private CreateClinicPlanOfferCommand CreateCommand(Guid planId, decimal? price = 499m, DateTime? validUntil = null)
        => new(_applicationId, planId, price, validUntil, "Launch offer", Guid.NewGuid(), "admin@example.test");

    [Fact]
    public async Task Handle_Create_ValidOffer_AddsIt()
    {
        var plan = SetUpPlan();
        _offerRepository.Setup(r => r.GetActiveForClinicAndPlanAsync(_applicationId, plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicPlanOffer?)null);

        ClinicPlanOffer? added = null;
        _offerRepository.Setup(r => r.AddAsync(It.IsAny<ClinicPlanOffer>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicPlanOffer, CancellationToken>((o, _) => added = o);

        await CreateHandler().Handle(CreateCommand(plan.Id), CancellationToken.None);

        added.Should().NotBeNull();
        added!.ApplicationId.Should().Be(_applicationId);
        added.OfferPrice.Should().Be(499m);
        added.CreatedByAdminEmail.Should().Be("admin@example.test");
    }

    [Fact]
    public async Task Handle_Create_DuplicateLiveOffer_Throws()
    {
        var plan = SetUpPlan();
        var existing = ClinicPlanOffer.Create(_applicationId, plan.Id, 499m, null, null, "admin@example.test");
        _offerRepository.Setup(r => r.GetActiveForClinicAndPlanAsync(_applicationId, plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = async () => await CreateHandler().Handle(CreateCommand(plan.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already has a live offer*");
    }

    [Fact]
    public async Task Handle_Create_TrialPlan_Throws()
    {
        var plan = SetUpPlan(isTrial: true);

        var act = async () => await CreateHandler().Handle(CreateCommand(plan.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Trial plan cannot be offered*");
    }

    [Fact]
    public async Task Handle_Create_InactivePlan_ThrowsNotFoundException()
    {
        var plan = SetUpPlan(isActive: false);

        var act = async () => await CreateHandler().Handle(CreateCommand(plan.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Withdraw_LiveOffer_WithdrawsIt()
    {
        var offer = ClinicPlanOffer.Create(_applicationId, Guid.NewGuid(), 499m, null, null, "admin@example.test");
        _offerRepository.Setup(r => r.GetByIdForClinicAsync(offer.Id, _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offer);

        await WithdrawHandler().Handle(
            new WithdrawClinicPlanOfferCommand(_applicationId, offer.Id, Guid.NewGuid(), "admin@example.test"),
            CancellationToken.None);

        offer.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Withdraw_OfferOfAnotherClinic_ThrowsNotFoundException()
    {
        _offerRepository.Setup(r => r.GetByIdForClinicAsync(It.IsAny<Guid>(), _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicPlanOffer?)null);

        var act = async () => await WithdrawHandler().Handle(
            new WithdrawClinicPlanOfferCommand(_applicationId, Guid.NewGuid(), Guid.NewGuid(), "admin@example.test"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Withdraw_AlreadyWithdrawn_Throws()
    {
        var offer = ClinicPlanOffer.Create(_applicationId, Guid.NewGuid(), 499m, null, null, "admin@example.test");
        offer.Withdraw("admin@example.test");
        _offerRepository.Setup(r => r.GetByIdForClinicAsync(offer.Id, _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offer);

        var act = async () => await WithdrawHandler().Handle(
            new WithdrawClinicPlanOfferCommand(_applicationId, offer.Id, Guid.NewGuid(), "admin@example.test"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_SetVisibility_NoAccountRow_CreatesOneAndSetsTheFlag()
    {
        _accountRepository.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicAccount?)null);

        ClinicAccount? added = null;
        _accountRepository.Setup(r => r.AddAsync(It.IsAny<ClinicAccount>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicAccount, CancellationToken>((a, _) => added = a);

        await VisibilityHandler().Handle(
            new SetClinicPlanVisibilityCommand(_applicationId, true, Guid.NewGuid(), "admin@example.test"),
            CancellationToken.None);

        added.Should().NotBeNull();
        added!.RestrictToOfferedPlans.Should().BeTrue();
    }
}
