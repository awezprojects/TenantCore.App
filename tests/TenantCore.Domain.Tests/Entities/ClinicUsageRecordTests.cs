using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

/// <summary>
/// The metered-usage ledger row. Every billable operation is recorded for every clinic whatever its
/// mode — a free row is still a row (plan/registration-fee-and-usage-billing, decisions 7–9).
/// </summary>
public class ClinicUsageRecordTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ClinicUsageRecord Create(
        BillingMode mode, decimal unitRate, int quantity = 1, bool forceFree = false)
        => ClinicUsageRecord.Create(
            Guid.NewGuid(), BillingItem.OpdRegistration, Guid.NewGuid(), unitRate, mode, Now, quantity,
            createdByUserId: null, forceFree: forceFree);

    [Fact]
    public void SubscriptionMode_IsAlwaysFree_EvenWhenTheItemIsPriced()
    {
        var record = Create(BillingMode.Subscription, unitRate: 25m, quantity: 4);

        record.IsFree.Should().BeTrue("a subscription clinic is metered, never charged");
        record.Amount.Should().Be(0m);
        record.UnitRate.Should().Be(25m, "the rate is snapshotted so the usage stays convertible later");
        record.PayerMode.Should().Be(BillingMode.Subscription);
        record.WalletTransactionId.Should().BeNull();
    }

    [Fact]
    public void PayAsYouGo_WithAPrice_ChargesTheRoundedProduct()
    {
        var record = Create(BillingMode.PayAsYouGo, unitRate: 25m, quantity: 4);

        record.IsFree.Should().BeFalse();
        record.Amount.Should().Be(100m);
        record.PayerMode.Should().Be(BillingMode.PayAsYouGo);
    }

    [Fact]
    public void PayAsYouGo_WithAFractionalPrice_RoundsAwayFromZeroToTwoDecimals()
    {
        // 12.345 * 3 = 37.035 — money is stored at 2 decimals, rounded away from zero.
        var record = Create(BillingMode.PayAsYouGo, unitRate: 12.345m, quantity: 3);

        record.Amount.Should().Be(37.04m);
        record.UnitRate.Should().Be(12.345m, "the rate snapshot keeps the 4-decimal precision it was recorded at");
    }

    [Fact]
    public void PayAsYouGo_WithoutAPrice_IsFreeNotBlocked()
    {
        // No ClinicUsageRate row means rate 0 means free — an unpriced item never fails a registration.
        var record = Create(BillingMode.PayAsYouGo, unitRate: 0m);

        record.IsFree.Should().BeTrue();
        record.Amount.Should().Be(0m);
        record.UnitRate.Should().Be(0m, "the row is how the Admin dashboard sees the item is unpriced");
        record.PayerMode.Should().Be(BillingMode.PayAsYouGo);
    }

    [Fact]
    public void ForceFree_RecordsTheRateButChargesNothing()
    {
        // The UsageBilling:Enabled kill switch still meters, so switching metering back on needs no
        // backfill and analytics keep working while a rollout is paused.
        var record = Create(BillingMode.PayAsYouGo, unitRate: 40m, quantity: 2, forceFree: true);

        record.IsFree.Should().BeTrue();
        record.Amount.Should().Be(0m);
        record.UnitRate.Should().Be(40m);
        record.PayerMode.Should().Be(BillingMode.PayAsYouGo);
    }

    [Fact]
    public void Create_SnapshotsTheModeItWasChargedUnder()
    {
        // The clinic's mode can change later; the row keeps saying what applied when it was written.
        var charged = Create(BillingMode.PayAsYouGo, unitRate: 10m);
        var later = Create(BillingMode.Subscription, unitRate: 10m);

        charged.PayerMode.Should().Be(BillingMode.PayAsYouGo);
        charged.IsFree.Should().BeFalse();
        later.PayerMode.Should().Be(BillingMode.Subscription);
        later.IsFree.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithAQuantityBelowOne_Throws(int quantity)
    {
        var act = () => Create(BillingMode.PayAsYouGo, unitRate: 10m, quantity: quantity);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quantity");
    }

    [Fact]
    public void Create_StampsTheOperationsInstantAndTheActor()
    {
        var userId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        var record = ClinicUsageRecord.Create(
            Guid.NewGuid(), BillingItem.IpdDischarge, sourceId, 30m, BillingMode.PayAsYouGo, Now,
            quantity: 1, createdByUserId: userId);

        record.ChargedAt.Should().Be(Now);
        record.CreatedByUserId.Should().Be(userId);
        record.SourceEntityId.Should().Be(sourceId);
        record.BillingItem.Should().Be(BillingItem.IpdDischarge);
        record.Quantity.Should().Be(1);
    }

    [Fact]
    public void AttachWalletTransaction_LinksTheDebitThatPaidForIt()
    {
        var record = Create(BillingMode.PayAsYouGo, unitRate: 10m);
        var ledgerId = Guid.NewGuid();

        record.AttachWalletTransaction(ledgerId);

        record.WalletTransactionId.Should().Be(ledgerId);
    }

    [Fact]
    public void AttachWalletTransaction_OnAFreeRow_Throws()
    {
        // A free row must never point at a ledger movement that does not exist.
        var record = Create(BillingMode.Subscription, unitRate: 10m);

        var act = () => record.AttachWalletTransaction(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>().WithMessage("*free usage record*");
        record.WalletTransactionId.Should().BeNull();
    }
}
