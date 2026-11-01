using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

/// <summary>
/// The Phase 2 billing state on <see cref="ClinicAccount"/>: which mode is live, the one-time
/// registration fee and the package-granted free test days.
/// See plan/registration-fee-and-usage-billing "Decisions" 12-14.
/// </summary>
public class ClinicAccountBillingTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ClinicAccount NewAccount() => ClinicAccount.CreateDefault(Guid.NewGuid());

    // -- billing mode ---------------------------------------------------------------------------

    [Fact]
    public void CreateDefault_IsSubscriptionWithNothingQueued()
    {
        var account = NewAccount();

        account.BillingMode.Should().Be(BillingMode.Subscription);
        account.PendingBillingMode.Should().BeNull();
        account.PendingBillingModeEffectiveFrom.Should().BeNull();
        account.EffectiveBillingMode(Now).Should().Be(BillingMode.Subscription);
    }

    [Fact]
    public void SetBillingMode_ApplyNow_SwitchesImmediatelyAndLeavesNothingQueued()
    {
        var account = NewAccount();

        account.SetBillingMode(BillingMode.PayAsYouGo, Now, applyNow: true);

        account.BillingMode.Should().Be(BillingMode.PayAsYouGo);
        account.PendingBillingMode.Should().BeNull();
        account.PendingBillingModeEffectiveFrom.Should().BeNull();
        account.EffectiveBillingMode(Now).Should().Be(BillingMode.PayAsYouGo);
    }

    [Fact]
    public void SetBillingMode_ApplyNow_ClearsAnAlreadyQueuedChange()
    {
        var account = NewAccount();
        account.SetBillingMode(BillingMode.PayAsYouGo, Now, applyNow: false, Now.AddDays(30));

        account.SetBillingMode(BillingMode.Subscription, Now, applyNow: true);

        account.BillingMode.Should().Be(BillingMode.Subscription);
        account.PendingBillingMode.Should().BeNull();
    }

    [Fact]
    public void SetBillingMode_Queued_KeepsTheCurrentModeUntilTheEffectiveInstant()
    {
        var account = NewAccount();
        var coverageEnd = Now.AddDays(30);

        account.SetBillingMode(BillingMode.PayAsYouGo, Now, applyNow: false, coverageEnd);

        account.BillingMode.Should().Be(BillingMode.Subscription, "the paid-for days are not lost");
        account.PendingBillingMode.Should().Be(BillingMode.PayAsYouGo);
        account.PendingBillingModeEffectiveFrom.Should().Be(coverageEnd);
        account.EffectiveBillingMode(Now).Should().Be(BillingMode.Subscription);
        account.EffectiveBillingMode(coverageEnd.AddSeconds(-1)).Should().Be(BillingMode.Subscription);
        account.EffectiveBillingMode(coverageEnd).Should().Be(BillingMode.PayAsYouGo, "the boundary is inclusive");
        account.EffectiveBillingMode(coverageEnd.AddDays(1)).Should().Be(BillingMode.PayAsYouGo);
    }

    [Fact]
    public void SetBillingMode_QueuedWithoutAnEffectiveDate_IsImmediate()
    {
        // An unsubscribed clinic has no coverage to wait for, so the queue is immediate.
        var account = NewAccount();

        account.SetBillingMode(BillingMode.PayAsYouGo, Now, applyNow: false);

        account.PendingBillingModeEffectiveFrom.Should().Be(Now);
        account.EffectiveBillingMode(Now).Should().Be(BillingMode.PayAsYouGo);
    }

    [Fact]
    public void SetBillingMode_QueuedTwice_ALaterDateSupersedesTheEarlierOne()
    {
        var account = NewAccount();
        var firstEnd = Now.AddDays(30);
        var laterEnd = Now.AddDays(60);

        account.SetBillingMode(BillingMode.PayAsYouGo, Now, applyNow: false, firstEnd);
        account.SetBillingMode(BillingMode.PayAsYouGo, Now, applyNow: false, laterEnd);

        account.PendingBillingModeEffectiveFrom.Should().Be(laterEnd);
        account.EffectiveBillingMode(firstEnd.AddDays(1)).Should().Be(BillingMode.Subscription);
        account.EffectiveBillingMode(laterEnd).Should().Be(BillingMode.PayAsYouGo);
    }

    [Fact]
    public void ApplyPendingBillingMode_MakesTheQueuedModeLiveAndClearsTheQueue()
    {
        var account = NewAccount();
        var coverageEnd = Now.AddDays(30);
        account.SetBillingMode(BillingMode.PayAsYouGo, Now, applyNow: false, coverageEnd);

        account.ApplyPendingBillingMode(coverageEnd);

        account.BillingMode.Should().Be(BillingMode.PayAsYouGo);
        account.PendingBillingMode.Should().BeNull();
        account.PendingBillingModeEffectiveFrom.Should().BeNull();
    }

    [Fact]
    public void ApplyPendingBillingMode_WithNothingQueued_IsANoOp()
    {
        var account = NewAccount();

        account.ApplyPendingBillingMode(Now);

        account.BillingMode.Should().Be(BillingMode.Subscription);
        account.PendingBillingMode.Should().BeNull();
    }

    [Theory]
    [InlineData(BillingMode.Subscription, BillingMode.PayAsYouGo)]
    [InlineData(BillingMode.PayAsYouGo, BillingMode.Subscription)]
    public void ResolveEffectiveBillingMode_AppliesTheSameRuleWithoutLoadingTheRow(
        BillingMode current, BillingMode queued)
    {
        var effectiveFrom = Now.AddDays(10);

        ClinicAccount.ResolveEffectiveBillingMode(current, queued, effectiveFrom, Now)
            .Should().Be(current);
        ClinicAccount.ResolveEffectiveBillingMode(current, queued, effectiveFrom, effectiveFrom.AddDays(1))
            .Should().Be(queued);
        ClinicAccount.ResolveEffectiveBillingMode(current, null, null, Now)
            .Should().Be(current, "no queue means the stored mode is live");
        ClinicAccount.ResolveEffectiveBillingMode(current, queued, null, Now)
            .Should().Be(current, "a queued mode with no instant can never activate");
    }

    // -- registration fee -----------------------------------------------------------------------

    [Fact]
    public void ANewAccountOwesItsRegistrationFee()
    {
        var account = NewAccount();

        account.RegistrationFeeChargedAt.Should().BeNull();
        account.RegistrationFeeWaived.Should().BeFalse();
        account.HasRegistrationFeeDue.Should().BeTrue();
    }

    [Fact]
    public void HasRegistrationFeeDue_IsFalseOnceWaived()
    {
        var account = NewAccount();

        account.WaiveRegistrationFee("Opening offer", "admin@example.test");

        account.HasRegistrationFeeDue.Should().BeFalse();
    }

    [Fact]
    public void HasRegistrationFeeDue_IsFalseOnceCharged()
    {
        var account = NewAccount();

        account.MarkRegistrationFeeCharged("payment:abc", Now);

        account.HasRegistrationFeeDue.Should().BeFalse();
    }

    [Fact]
    public void SetRegistrationFee_StoresThePerClinicOverride()
    {
        var account = NewAccount();

        account.SetRegistrationFee(2500m, "admin@example.test");

        account.RegistrationFee.Should().Be(2500m);
    }

    [Fact]
    public void WaiveRegistrationFee_RecordsTheReasonAndTheAdmin()
    {
        var account = NewAccount();

        account.WaiveRegistrationFee("Founding clinic", "admin@example.test");

        account.RegistrationFeeWaived.Should().BeTrue();
        account.RegistrationFeeWaiveReason.Should().Be("Founding clinic");
        account.RegistrationFeeWaivedByAdminEmail.Should().Be("admin@example.test");
    }

    [Fact]
    public void WaiveRegistrationFee_Twice_Throws()
    {
        var account = NewAccount();
        account.WaiveRegistrationFee("first", "admin@example.test");

        var act = () => account.WaiveRegistrationFee("second", "admin@example.test");

        act.Should().Throw<InvalidOperationException>().WithMessage("*already been waived*");
    }

    [Fact]
    public void WaiveRegistrationFee_OncePaid_Throws_SoARefundIsNeededInstead()
    {
        var account = NewAccount();
        account.MarkRegistrationFeeCharged("payment:abc", Now);

        var act = () => account.WaiveRegistrationFee("changed our mind", "admin@example.test");

        act.Should().Throw<InvalidOperationException>().WithMessage("*refund it instead*");
    }

    [Fact]
    public void MarkRegistrationFeeCharged_RecordsWhoChargedIt()
    {
        var account = NewAccount();

        account.MarkRegistrationFeeCharged("payment:abc", Now);

        account.RegistrationFeeChargedAt.Should().Be(Now);
        account.RegistrationFeeChargedBy.Should().Be("payment:abc");
    }

    [Fact]
    public void MarkRegistrationFeeCharged_IsOnceOnly()
    {
        var account = NewAccount();
        account.MarkRegistrationFeeCharged("payment:abc", Now);

        var act = () => account.MarkRegistrationFeeCharged("payment:def", Now);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already been charged*");
        account.RegistrationFeeChargedBy.Should().Be("payment:abc");
    }

    [Fact]
    public void MarkRegistrationFeeCharged_WhenWaived_Throws()
    {
        var account = NewAccount();
        account.WaiveRegistrationFee("Opening offer", "admin@example.test");

        var act = () => account.MarkRegistrationFeeCharged("payment:abc", Now);

        act.Should().Throw<InvalidOperationException>().WithMessage("*waived*");
    }

    // -- free test days -------------------------------------------------------------------------

    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(90)]
    public void SetFreeTestDays_WithinTheAllowedBounds_IsAccepted(int days)
    {
        var account = NewAccount();

        account.SetFreeTestDays(days);

        account.FreeTestDays.Should().Be(days);
    }

    [Theory]
    [InlineData(29)]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(91)]
    public void SetFreeTestDays_OutsideTheAllowedBounds_Throws(int days)
    {
        var account = NewAccount();

        var act = () => account.SetFreeTestDays(days);

        act.Should().Throw<DomainException>().WithMessage("*between 30 and 90*");
        account.FreeTestDays.Should().BeNull();
    }

    [Fact]
    public void StartTrial_RecordsTheFirstTrialOnly()
    {
        var account = NewAccount();
        account.HasUsedTrial.Should().BeFalse();

        account.StartTrial(Now);

        account.HasUsedTrial.Should().BeTrue();
        account.TrialStartedAt.Should().Be(Now);

        var act = () => account.StartTrial(Now.AddDays(1));
        act.Should().Throw<DomainException>().WithMessage("*already used its free trial*");
        account.TrialStartedAt.Should().Be(Now, "the first trial's instant is never overwritten");
    }

}
