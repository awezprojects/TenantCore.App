using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Application.Features.PlatformAdmin.Models;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Commands;

public class SubscriptionPlanHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly IOptions<OnboardingOptions> _options =
        Options.Create(new OnboardingOptions { MinPaymentAmount = 1m, MaxPaymentAmount = 500000m });

    public SubscriptionPlanHandlerTests()
        => _planRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private CreateSubscriptionPlanHandler CreateHandler()
        => new(_planRepository.Object, _options, Mock.Of<ILogger<CreateSubscriptionPlanHandler>>());

    private UpdateSubscriptionPlanHandler UpdateHandler()
        => new(_planRepository.Object, _options);

    private SetSubscriptionPlanActiveHandler ActiveHandler()
        => new(_planRepository.Object);

    private static PlanDetails Details(string name = "Clinic Pro", decimal price = 4999m)
        => new(name, "desc", 90, price, IsPopular: false, IsPublic: true, DisplayOrder: 5);

    [Fact]
    public async Task Handle_Create_ValidPlan_AddsCustomPlan()
    {
        _planRepository.Setup(r => r.NameExistsAsync("Clinic Pro", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        SubscriptionPlan? added = null;
        _planRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPlan, CancellationToken>((p, _) => added = p);

        await CreateHandler().Handle(
            new CreateSubscriptionPlanCommand(Details(), Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Code.Should().Be(SubscriptionPlanCode.Custom);
        added.Name.Should().Be("Clinic Pro");
        added.Price.Should().Be(4999m);
    }

    [Fact]
    public async Task Handle_Create_DuplicateName_Throws()
    {
        _planRepository.Setup(r => r.NameExistsAsync("Clinic Pro", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = async () => await CreateHandler().Handle(
            new CreateSubscriptionPlanCommand(Details(), Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_Create_PriceBelowGatewayMinimum_Throws()
    {
        _planRepository.Setup(r => r.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = async () => await CreateHandler().Handle(
            new CreateSubscriptionPlanCommand(Details(price: 0m), Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*at least*");
    }

    [Fact]
    public async Task Handle_Update_KeepsCodeAndAppliesChanges()
    {
        var plan = SubscriptionPlan.CreateCustom("Old", "old", 30, 999m, false, 1, true);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _planRepository.Setup(r => r.NameExistsAsync("Clinic Pro", plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await UpdateHandler().Handle(
            new UpdateSubscriptionPlanCommand(plan.Id, Details(), Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        plan.Name.Should().Be("Clinic Pro");
        plan.Code.Should().Be(SubscriptionPlanCode.Custom);
        _planRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Update_TrialPriceAboveZero_Throws()
    {
        var trial = SubscriptionPlan.CreateForSeed(
            Guid.NewGuid(), SubscriptionPlanCode.Trial, "Free Trial", "d", 14, 0m, "INR", true, false, 1);
        _planRepository.Setup(r => r.GetByIdAsync(trial.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trial);
        _planRepository.Setup(r => r.NameExistsAsync(It.IsAny<string>(), trial.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = async () => await UpdateHandler().Handle(
            new UpdateSubscriptionPlanCommand(trial.Id, Details(name: "Free Trial", price: 99m), Guid.NewGuid(), "admin@example.test"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*must stay free*");
    }

    [Fact]
    public async Task Handle_Update_TrialAtZero_IsAllowedDespiteGatewayMinimum()
    {
        var trial = SubscriptionPlan.CreateForSeed(
            Guid.NewGuid(), SubscriptionPlanCode.Trial, "Free Trial", "d", 14, 0m, "INR", true, false, 1);
        _planRepository.Setup(r => r.GetByIdAsync(trial.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trial);
        _planRepository.Setup(r => r.NameExistsAsync(It.IsAny<string>(), trial.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await UpdateHandler().Handle(
            new UpdateSubscriptionPlanCommand(trial.Id,
                new PlanDetails("Free Trial", "d", 21, 0m, false, true, 1), Guid.NewGuid(), "admin@example.test"),
            CancellationToken.None);

        trial.DurationDays.Should().Be(21);
    }

    [Fact]
    public async Task Handle_Update_MissingPlan_ThrowsNotFoundException()
    {
        _planRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var act = async () => await UpdateHandler().Handle(
            new UpdateSubscriptionPlanCommand(Guid.NewGuid(), Details(), Guid.NewGuid(), "admin@example.test"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Deactivate_SetsIsActiveFalse()
    {
        var plan = SubscriptionPlan.CreateCustom("Plan", "d", 30, 999m, false, 1, true);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        await ActiveHandler().Handle(
            new SetSubscriptionPlanActiveCommand(plan.Id, false, Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetSubscriptionPlanActiveCommand_ActionName_ReflectsTheDirection()
    {
        var activate = new SetSubscriptionPlanActiveCommand(Guid.NewGuid(), true, Guid.NewGuid(), "a@b.test");
        var deactivate = new SetSubscriptionPlanActiveCommand(Guid.NewGuid(), false, Guid.NewGuid(), "a@b.test");

        activate.ActionName.Should().Be("Subscription Plan Activated");
        deactivate.ActionName.Should().Be("Subscription Plan Deactivated");
    }
}
