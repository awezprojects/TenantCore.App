using FluentAssertions;
using TenantCore.Application.Features.UsageBilling.Services;

namespace TenantCore.Application.Tests.Features.UsageBilling.Services;

/// <summary>
/// How long a clinic's free test window lasts. A granted value always wins — that is the whole point
/// of the field — and a plan with no usable duration falls back to the domain's thirty-day floor
/// rather than to zero.
/// </summary>
public class TrialTermRulesTests
{
    private static TrialTermRules Rules() => new();

    [Theory]
    [InlineData(7, 30, 7)]
    [InlineData(14, 30, 14)]
    [InlineData(60, 30, 60)]
    public void ResolveTrialDurationDays_GrantedFreeTestDays_WinOverThePlanDuration(
        int granted, int planDays, int expected)
    {
        Rules().ResolveTrialDurationDays(granted, planDays).Should().Be(expected);
    }

    [Fact]
    public void ResolveTrialDurationDays_NoGrantedValue_UsesThePlanDuration()
    {
        Rules().ResolveTrialDurationDays(null, 14).Should().Be(14);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void ResolveTrialDurationDays_PlanDurationUnusable_FallsBackToThirtyDays(int planDays)
    {
        Rules().ResolveTrialDurationDays(null, planDays).Should().Be(30,
            "thirty days is the floor the domain allows, and the safe answer");
    }

    [Fact]
    public void ResolveTrialDurationDays_GrantedZero_IsHonouredAsNoFreeDays()
    {
        Rules().ResolveTrialDurationDays(0, 30).Should().Be(0,
            "a granted value is never second-guessed, even when it means no free window at all");
    }

    [Fact]
    public void ResolveTrialDurationDays_IsPureAndRepeatable()
    {
        var rules = Rules();

        rules.ResolveTrialDurationDays(10, 30).Should().Be(10);
        rules.ResolveTrialDurationDays(10, 30).Should().Be(10);
    }
}
