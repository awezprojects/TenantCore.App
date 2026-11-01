using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

public class SubscriptionPlanTests
{
    private static SubscriptionPlan Trial() =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Free Trial", "d",
            14, 0m, "INR", isTrial: true, isPopular: false, displayOrder: 1);

    [Fact]
    public void CreateCustom_UsesCustomCodeAndIsActive()
    {
        var plan = SubscriptionPlan.CreateCustom("Clinic Pro", "desc", 90, 4999m, isPopular: true, displayOrder: 5, isPublic: false);

        plan.Code.Should().Be(SubscriptionPlanCode.Custom);
        plan.IsTrial.Should().BeFalse();
        plan.IsActive.Should().BeTrue();
        plan.IsPublic.Should().BeFalse();
        plan.Currency.Should().Be("INR");
    }

    [Fact]
    public void CreateForSeed_IsPublicByDefault()
        => Trial().IsPublic.Should().BeTrue();

    [Fact]
    public void UpdateDetails_ChangesEditableFieldsButNotCode()
    {
        var plan = SubscriptionPlan.CreateCustom("Old", "old", 30, 999m, false, 1, true);
        var originalCode = plan.Code;

        plan.UpdateDetails("New", "new", 60, 1499m, isPopular: true, displayOrder: 2, isPublic: false);

        plan.Name.Should().Be("New");
        plan.DurationDays.Should().Be(60);
        plan.Price.Should().Be(1499m);
        plan.IsPopular.Should().BeTrue();
        plan.IsPublic.Should().BeFalse();
        plan.Code.Should().Be(originalCode);
    }

    [Fact]
    public void UpdateDetails_TrialWithNonZeroPrice_Throws()
    {
        var plan = Trial();

        var act = () => plan.UpdateDetails("Free Trial", "d", 14, 1m, false, 1, true);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must stay free*");
    }

    [Fact]
    public void UpdateDetails_TrialAtZero_Succeeds()
    {
        var plan = Trial();

        plan.UpdateDetails("Free Trial", "longer trial", 21, 0m, false, 1, true);

        plan.DurationDays.Should().Be(21);
    }

    [Fact]
    public void DeactivateAndActivate_ToggleIsActive()
    {
        var plan = SubscriptionPlan.CreateCustom("Plan", "d", 30, 999m, false, 1, true);

        plan.Deactivate();
        plan.IsActive.Should().BeFalse();

        plan.Activate();
        plan.IsActive.Should().BeTrue();
    }
}
