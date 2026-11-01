using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Handlers;
using TenantCore.Application.Features.UsageBilling.Models;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Tests.Features.UsageBilling.Handlers;

/// <summary>
/// The metering engine. Each test pins down one of the four outcomes: a replay returns the original
/// charge, a free row is written for every non-charged case, a funded prepaid clinic is debited, and
/// an unfunded one is refused with 403 <c>insufficient_wallet_balance</c> having committed nothing.
/// </summary>
public class ChargeUsageCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IClinicUsageRecordRepository> _usageRecords = new();
    private readonly Mock<IClinicUsageRateRepository> _usageRates = new();
    private readonly Mock<IClinicWalletRepository> _wallets = new();
    private readonly Mock<IClinicWalletTransactionRepository> _ledger = new();
    private readonly Mock<IClinicAccountRepository> _accounts = new();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _sourceEntityId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ChargeUsageCommandHandlerTests()
    {
        // Defaults describing "a clinic nobody has configured": no prior row, no rate, no wallet, no
        // account row — which is exactly today's Subscription behaviour.
        _usageRecords
            .Setup(r => r.GetBySourceAsync(
                It.IsAny<Guid>(), It.IsAny<BillingItem>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicUsageRecord?)null);
        _usageRates
            .Setup(r => r.GetActiveAsync(It.IsAny<Guid>(), It.IsAny<BillingItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicUsageRate?)null);
        _accounts
            .Setup(r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicAccount?)null);
        _wallets
            .Setup(r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicWallet?)null);

        // Captured at call time — the handler stages these rows but never saves them.
        _usageRecords
            .Setup(r => r.AddAsync(It.IsAny<ClinicUsageRecord>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicUsageRecord, CancellationToken>((record, _) => StagedRecord = record)
            .Returns(Task.CompletedTask);
        _ledger
            .Setup(r => r.AddAsync(It.IsAny<ClinicWalletTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicWalletTransaction, CancellationToken>((row, _) => StagedLedgerRow = row)
            .Returns(Task.CompletedTask);
    }

    private ClinicUsageRecord? StagedRecord { get; set; }

    private ClinicWalletTransaction? StagedLedgerRow { get; set; }

    private ChargeUsageCommandHandler CreateHandler(UsageBillingOptions? options = null)
        => new(
            _usageRecords.Object,
            _usageRates.Object,
            _wallets.Object,
            _ledger.Object,
            _accounts.Object,
            Options.Create(options ?? new UsageBillingOptions()),
            Mock.Of<ILogger<ChargeUsageCommandHandler>>());

    private ChargeUsageCommand Command(int quantity = 1, DateTime? occurredAt = null)
        => new(_applicationId, BillingItem.OpdRegistration, _sourceEntityId, quantity, _userId, occurredAt ?? Now);

    private void WithAccountMode(BillingMode mode, DateTime? pendingEffectiveFrom = null)
    {
        var account = ClinicAccount.CreateDefault(_applicationId);
        if (mode != BillingMode.Subscription || pendingEffectiveFrom is not null)
            account.SetBillingMode(mode, Now, applyNow: false, pendingEffectiveFrom);

        _accounts
            .Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
    }

    private void WithRate(decimal? rate)
    {
        var row = rate is { } value
            ? ClinicUsageRate.Create(_applicationId, BillingItem.OpdRegistration, value, Now, "admin@example.test")
            : null;

        _usageRates
            .Setup(r => r.GetActiveAsync(_applicationId, BillingItem.OpdRegistration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(row);
    }

    private ClinicWallet WithWallet(decimal balance)
    {
        var wallet = ClinicWallet.Create(_applicationId, 100m, "INR");
        if (balance > 0m)
            wallet.Credit(balance, Now);

        _wallets
            .Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        return wallet;
    }

    private ClinicUsageRecord? CaptureStagedRecord() => StagedRecord;

    private ClinicWalletTransaction? CaptureStagedLedgerRow() => StagedLedgerRow;

    private void VerifyNothingWasStagedOrSaved()
    {
        _usageRecords.Verify(
            r => r.AddAsync(It.IsAny<ClinicUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoMoneyMoved();
    }

    /// <summary>
    /// The wallet is only ever touched by the charge path, the ledger never gains a row, and nothing is
    /// saved — <c>ChargeUsageCommandHandler</c> deliberately leaves persistence to its parent handler.
    /// </summary>
    private void VerifyNoMoneyMoved()
    {
        _wallets.Verify(
            r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _ledger.Verify(
            r => r.AddAsync(It.IsAny<ClinicWalletTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _wallets.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _ledger.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _usageRecords.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -- replay ---------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_WhenTheOperationWasAlreadyMetered_ReturnsTheOriginalChargeAndWritesNothing()
    {
        _usageRecords
            .Setup(r => r.GetBySourceAsync(
                _applicationId, BillingItem.OpdRegistration, _sourceEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClinicUsageRecord.Create(
                _applicationId, BillingItem.OpdRegistration, _sourceEntityId, 25m,
                BillingMode.PayAsYouGo, Now, 4));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Charged.Should().BeFalse("the first call already did the work");
        result.Amount.Should().Be(100m, "the replay reports what was originally charged");
        result.BalanceAfter.Should().BeNull("nothing moved this time");
        result.IsFree.Should().BeFalse();

        VerifyNothingWasStagedOrSaved();
    }

    [Fact]
    public async Task Handle_WhenAlreadyMetered_DoesNotEvenReadTheModeOrTheRate()
    {
        // The pre-check runs first for a reason: a replay must not be able to fail on configuration.
        _usageRecords
            .Setup(r => r.GetBySourceAsync(
                It.IsAny<Guid>(), It.IsAny<BillingItem>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClinicUsageRecord.Create(
                _applicationId, BillingItem.OpdRegistration, _sourceEntityId, 0m,
                BillingMode.Subscription, Now, 1));

        await CreateHandler().Handle(Command(), CancellationToken.None);

        _accounts.Verify(
            r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _usageRates.Verify(
            r => r.GetActiveAsync(It.IsAny<Guid>(), It.IsAny<BillingItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNothingWasStagedOrSaved();
    }

    // -- free rows ------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_WhenTheClinicHasNoAccountRow_ReturnsImmediatelyWithoutWritingAnyRow()
    {
        // No account row → effective mode is Subscription. Subscription clinics are not metered at the
        // operation level — the early return means no usage row, no wallet touch and no DB writes.
        WithRate(50m);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Charged.Should().BeFalse();
        result.Amount.Should().Be(0m);
        result.BalanceAfter.Should().BeNull();
        result.IsFree.Should().BeTrue();

        VerifyNothingWasStagedOrSaved();
    }

    [Fact]
    public async Task Handle_WhenTheClinicIsOnSubscription_ReturnsImmediatelyWithoutWritingAnyRow()
    {
        // Subscription clinics pay by plan — no per-operation rows are written.
        WithAccountMode(BillingMode.Subscription);
        WithRate(75m);

        var result = await CreateHandler().Handle(Command(quantity: 3), CancellationToken.None);

        result.Charged.Should().BeFalse();
        result.IsFree.Should().BeTrue();

        VerifyNothingWasStagedOrSaved();
    }

    [Fact]
    public async Task Handle_WhenTheItemIsUnpriced_MetersTheRowFree()
    {
        // No rate row means rate 0 means free: an unpriced item must never block a registration.
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Charged.Should().BeFalse();
        result.IsFree.Should().BeTrue();

        var staged = CaptureStagedRecord();
        staged!.PayerMode.Should().Be(BillingMode.PayAsYouGo);
        staged.UnitRate.Should().Be(0m);
        staged.Amount.Should().Be(0m);
        VerifyNoMoneyMoved();
    }

    [Fact]
    public async Task Handle_WhenTheRateIsZero_MetersTheRowFree()
    {
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(0m);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Charged.Should().BeFalse();
        result.IsFree.Should().BeTrue();
        VerifyNoMoneyMoved();
    }

    [Fact]
    public async Task Handle_WhenMeteringIsSwitchedOff_StillWritesTheRowWithTheRate_ButFree()
    {
        // The kill switch pauses charging without losing data: nothing needs backfilling on resume.
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(40m);

        var result = await CreateHandler(new UsageBillingOptions { Enabled = false })
            .Handle(Command(), CancellationToken.None);

        result.Charged.Should().BeFalse();
        result.Amount.Should().Be(0m);
        result.IsFree.Should().BeTrue();

        CaptureStagedRecord()!.UnitRate.Should().Be(40m, "the price is snapshotted while charging is paused");
        VerifyNoMoneyMoved();
    }

    [Fact]
    public async Task Handle_WhenTheQueuedModeIsNotLiveYet_ReturnsImmediatelyWithoutWritingAnyRow()
    {
        // A clinic that queued PAYG still has paid-for subscription days left; the effective mode is
        // Subscription until the queued date, so no usage row is written.
        WithAccountMode(BillingMode.PayAsYouGo, pendingEffectiveFrom: Now.AddDays(30));
        WithRate(25m);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Charged.Should().BeFalse();
        result.IsFree.Should().BeTrue();
        VerifyNothingWasStagedOrSaved();
    }

    // -- charged rows ---------------------------------------------------------------------------

    [Fact]
    public async Task Handle_WhenPrepaidAndFunded_DebitsTheWalletAndWritesTheLedgerRow()
    {
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(25m);
        var wallet = WithWallet(500m);

        var result = await CreateHandler().Handle(Command(quantity: 4), CancellationToken.None);

        result.Charged.Should().BeTrue();
        result.Amount.Should().Be(100m, "4 x 25");
        result.BalanceAfter.Should().Be(400m);
        result.IsFree.Should().BeFalse();
        wallet.Balance.Should().Be(400m);

        var record = CaptureStagedRecord();
        record.Should().NotBeNull();
        record!.ChargedAt.Should().Be(Now);
        record.Quantity.Should().Be(4);
        record.UnitRate.Should().Be(25m);
        record.CreatedByUserId.Should().Be(_userId);
        record.SourceEntityId.Should().Be(_sourceEntityId);
        record.WalletTransactionId.Should().NotBeNull("a charged row points at the debit that paid for it");

        var ledgerRow = CaptureStagedLedgerRow();
        ledgerRow.Should().NotBeNull();
        ledgerRow!.Type.Should().Be(WalletTransactionType.Debit);
        ledgerRow.Amount.Should().Be(100m);
        ledgerRow.BalanceAfter.Should().Be(400m);
        ledgerRow.ReferenceType.Should().Be(WalletReferenceType.Usage);
        ledgerRow.ReferenceId.Should().Be(record.Id, "the ledger row references the usage row");
        ledgerRow.OccurredAt.Should().Be(Now);
        record.WalletTransactionId.Should().Be(ledgerRow.Id);
    }

    [Fact]
    public async Task Handle_NeverSavesItself_EvenWhenItCharged()
    {
        // Load-bearing: the usage row and the debit must commit with the parent operation, so a
        // refusal anywhere further up commits nothing at all.
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(25m);
        WithWallet(500m);

        await CreateHandler().Handle(Command(), CancellationToken.None);

        _wallets.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _ledger.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _usageRecords.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -- refusals -------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_WhenTheWalletCannotCoverIt_Throws403InsufficientWalletBalance()
    {
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(25m);
        var wallet = WithWallet(50m);

        var act = async () => await CreateHandler().Handle(Command(quantity: 4), CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<ErrorCodeException>();
        thrown.Which.StatusCode.Should().Be(403);
        thrown.Which.ErrorCode.Should().Be(UsageBillingErrorCodes.InsufficientWalletBalance);
        thrown.Which.Message.Should().Contain("Top up the wallet");
        wallet.Balance.Should().Be(50m, "a refused charge leaves the balance untouched");
    }

    [Fact]
    public async Task Handle_WhenTheWalletCannotCoverIt_CommitsNothing()
    {
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(25m);
        WithWallet(50m);

        var act = async () => await CreateHandler().Handle(Command(quantity: 4), CancellationToken.None);
        await act.Should().ThrowAsync<ErrorCodeException>();

        // The usage row is staged before the debit is attempted, but nothing is written: only the
        // parent handler's transaction could have committed it, and it never gets that far.
        _ledger.Verify(
            r => r.AddAsync(It.IsAny<ClinicWalletTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _wallets.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _ledger.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _usageRecords.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPrepaidAndNeverToppedUp_CreatesAZeroWalletThenRefuses()
    {
        // A zero wallet is created so the clinic gets the explicit top-up message rather than a null
        // reference — the first metered operation of a prepaid clinic is expected to fail this way.
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(25m);

        ClinicWallet? created = null;
        _wallets
            .Setup(r => r.AddAsync(It.IsAny<ClinicWallet>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicWallet, CancellationToken>((w, _) => created = w)
            .Returns(Task.CompletedTask);

        var act = async () => await CreateHandler().Handle(Command(), CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<ErrorCodeException>();
        thrown.Which.ErrorCode.Should().Be(UsageBillingErrorCodes.InsufficientWalletBalance);
        created.Should().NotBeNull();
        created!.Balance.Should().Be(0m);
        created.Currency.Should().Be("INR");
        created.LowBalanceThreshold.Should().Be(100m, "the configured default is applied at creation");
    }

    // -- amounts and instants -------------------------------------------------------------------

    [Fact]
    public async Task Handle_WhenPrepaid_FractionalAmountsRoundAwayFromZero()
    {
        // 12.345 x 3 = 37.035 — money is stored at 2 decimals, rounded away from zero.
        WithAccountMode(BillingMode.PayAsYouGo);
        WithRate(12.345m);
        var wallet = WithWallet(100m);

        var result = await CreateHandler().Handle(Command(quantity: 3), CancellationToken.None);

        result.Amount.Should().Be(37.04m);
        result.BalanceAfter.Should().Be(62.96m);
        CaptureStagedLedgerRow()!.Amount.Should().Be(37.04m, "the ledger agrees with the usage row to the paisa");
        wallet.Balance.Should().Be(62.96m);
    }

    [Fact]
    public async Task Handle_WhenPrepaid_UsesTheOccurredAtInstantForTheRowAndForTheMode()
    {
        // The mode is resolved as of the operation's own instant, not as of "now".
        var modeGoesLive = Now.AddDays(10);
        WithAccountMode(BillingMode.PayAsYouGo, pendingEffectiveFrom: modeGoesLive);
        WithRate(25m);
        WithWallet(500m);

        var result = await CreateHandler()
            .Handle(Command(occurredAt: modeGoesLive.AddDays(1)), CancellationToken.None);

        result.Charged.Should().BeTrue("by the operation's own instant the queued mode was live");
        CaptureStagedRecord()!.ChargedAt.Should().Be(modeGoesLive.AddDays(1));
    }

    [Fact]
    public void ChargeUsageCommand_ActionLogContext_CarriesIdentifiersOnly()
    {
        // ActionLoggingBehavior reads this once, before the handler runs, so the amount and the resulting
        // wallet movement cannot appear here — and nothing patient-identifying may ever reach a log line.
        var command = new ChargeUsageCommand(
            _applicationId, BillingItem.IpdDischarge, _sourceEntityId, 4, _userId, Now);

        command.ActionLogContext.Should().Be(
            $"applicationId={_applicationId}; billingItem=IpdDischarge; quantity=4; sourceEntityId={_sourceEntityId}");
        command.ActionLogContext.Should().NotContain(_userId.ToString());
    }
}
