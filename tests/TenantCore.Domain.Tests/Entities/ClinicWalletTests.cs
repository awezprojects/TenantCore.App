using System.Reflection;
using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;

namespace TenantCore.Domain.Tests.Entities;

/// <summary>
/// The prepaid wallet's own arithmetic. The balance only ever moves through Credit/Debit, each paired
/// with exactly one ledger row staged by the caller — see plan/registration-fee-and-usage-billing
/// "Decisions" 9 and 10.
/// </summary>
public class ClinicWalletTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ClinicWallet NewWallet(decimal threshold = 100m)
        => ClinicWallet.Create(Guid.NewGuid(), threshold, "INR");

    [Fact]
    public void Create_StartsWithAZeroBalanceAtTheConfiguredThreshold()
    {
        var wallet = NewWallet(250m);

        wallet.Balance.Should().Be(0m);
        wallet.LowBalanceThreshold.Should().Be(250m);
        wallet.Currency.Should().Be("INR");
        wallet.LastTopUpAt.Should().BeNull();
        wallet.LastDebitAt.Should().BeNull();
    }

    [Fact]
    public void Credit_AddsFundsAndStampsTheTopUpInstant()
    {
        var wallet = NewWallet();

        wallet.Credit(500m, Now);

        wallet.Balance.Should().Be(500m);
        wallet.LastTopUpAt.Should().Be(Now);
    }

    [Fact]
    public void Credit_Accumulates()
    {
        var wallet = NewWallet();

        wallet.Credit(500m, Now);
        wallet.Credit(250.50m, Now);

        wallet.Balance.Should().Be(750.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Credit_NonPositiveAmount_Throws(decimal amount)
    {
        var wallet = NewWallet();

        var act = () => wallet.Credit(amount, Now);

        act.Should().Throw<DomainException>().WithMessage("*Invalid wallet amount*");
        wallet.Balance.Should().Be(0m, "a refused credit must not move the balance");
    }

    [Fact]
    public void Debit_RemovesFundsAndStampsTheDebitInstant()
    {
        var wallet = NewWallet();
        wallet.Credit(500m, Now);

        wallet.Debit(125.25m, Now);

        wallet.Balance.Should().Be(374.75m);
        wallet.LastDebitAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Debit_NonPositiveAmount_Throws(decimal amount)
    {
        var wallet = NewWallet();
        wallet.Credit(500m, Now);

        var act = () => wallet.Debit(amount, Now);

        act.Should().Throw<DomainException>().WithMessage("*Invalid wallet amount*");
        wallet.Balance.Should().Be(500m);
    }

    [Fact]
    public void Debit_MoreThanTheBalance_ThrowsAndLeavesTheBalanceUntouched()
    {
        var wallet = NewWallet();
        wallet.Credit(100m, Now);

        var act = () => wallet.Debit(100.01m, Now);

        act.Should().Throw<DomainException>().WithMessage("*Insufficient wallet balance*");
        wallet.Balance.Should().Be(100m, "there is no overdraft");
    }

    [Fact]
    public void Debit_ExactlyTheBalance_IsAllowed()
    {
        var wallet = NewWallet();
        wallet.Credit(100m, Now);

        wallet.Debit(100m, Now);

        wallet.Balance.Should().Be(0m);
    }

    [Fact]
    public void HasFunds_IsInclusiveOfTheExactAmount()
    {
        var wallet = NewWallet();
        wallet.Credit(100m, Now);

        wallet.HasFunds(100m).Should().BeTrue();
        wallet.HasFunds(100.01m).Should().BeFalse();
    }

    [Fact]
    public void IsLow_IsTrueAtTheThresholdAndFalseAboveIt()
    {
        var wallet = NewWallet(100m);
        wallet.Credit(200m, Now);

        wallet.IsLow.Should().BeFalse();

        wallet.Debit(100m, Now);
        wallet.Balance.Should().Be(100m);
        wallet.IsLow.Should().BeTrue("at or below the threshold is low");

        wallet.Debit(1m, Now);
        wallet.IsLow.Should().BeTrue();
    }

    [Fact]
    public void IsLow_OnANeverCreditedWallet_IsTrue_AndStillBlocksNothing()
    {
        var wallet = NewWallet();

        wallet.IsLow.Should().BeTrue("a zero balance is at or below any threshold");
        // The flag is purely informational. It lives next to the balance but is never consulted by
        // HasFunds, so a low balance can never block an operation on its own.
        wallet.HasFunds(0m).Should().BeTrue();
        wallet.HasFunds(1m).Should().BeFalse();
    }

    [Fact]
    public void Balance_HasNoPublicSetter_NorASetBalanceMethod()
    {
        // The ledger is the only record of how the balance got here — a SetBalance would silently
        // break the "credit is always paired with a ledger row" rule (plan decision 10).
        typeof(ClinicWallet).GetProperty(nameof(ClinicWallet.Balance))!
            .SetMethod!.IsPublic.Should().BeFalse();

        typeof(ClinicWallet).GetMethod("SetBalance").Should().BeNull();
        typeof(ClinicWallet).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name).Should().NotContain(name => name.Contains("SetBalance"));
    }
}
