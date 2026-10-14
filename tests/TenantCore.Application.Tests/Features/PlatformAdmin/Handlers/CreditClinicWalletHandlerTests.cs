using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Handlers;

/// <summary>
/// The one place money enters a wallet the gateway never confirmed. Idempotency on the admin's
/// reference is the whole point: replaying a call must answer with the current balance, never credit
/// twice, and the balance plus its ledger row must land in a single SaveChanges.
/// </summary>
public class CreditClinicWalletHandlerTests
{
    private readonly Mock<IClinicWalletRepository> _wallets = new();
    private readonly Mock<IClinicWalletTransactionRepository> _ledger = new();
    private readonly List<ClinicWalletTransaction> _ledgerRows = [];
    private readonly Guid _applicationId = Guid.NewGuid();

    public CreditClinicWalletHandlerTests()
    {
        _wallets.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ledger.Setup(r => r.AddAsync(It.IsAny<ClinicWalletTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicWalletTransaction, CancellationToken>((row, _) => _ledgerRows.Add(row))
            .Returns(Task.CompletedTask);
        _wallets.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicWallet?)null);
    }

    private CreditClinicWalletHandler CreateHandler(UsageBillingOptions? options = null) =>
        new(_wallets.Object, _ledger.Object,
            Options.Create(options ?? new UsageBillingOptions()),
            NullLogger<CreditClinicWalletHandler>.Instance);

    private CreditClinicWalletCommand Command(decimal amount, Guid? referenceId = null) =>
        new(_applicationId, amount, "Goodwill credit", referenceId ?? Guid.NewGuid(),
            Guid.NewGuid(), "admin@example.test");

    [Fact]
    public async Task Handle_FirstCreditForAClinic_CreatesTheWalletAndItsLedgerRowInOneSave()
    {
        ClinicWallet? created = null;
        _wallets.Setup(r => r.AddAsync(It.IsAny<ClinicWallet>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicWallet, CancellationToken>((w, _) => created = w)
            .Returns(Task.CompletedTask);

        var balance = await CreateHandler(new UsageBillingOptions
        {
            Currency = "INR",
            DefaultLowBalanceThreshold = 250m
        }).Handle(Command(500m), CancellationToken.None);

        balance.Should().Be(500m);
        created.Should().NotBeNull();
        created!.Balance.Should().Be(500m);
        created.LowBalanceThreshold.Should().Be(250m, "the configured threshold is applied at creation");
        created.Currency.Should().Be("INR");
        _ledgerRows.Should().ContainSingle();
        _wallets.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once,
            "a credit without a ledger row is a bug, not an option");
    }

    [Fact]
    public async Task Handle_CreditOnAnExistingWallet_AddsToTheBalance()
    {
        var wallet = ClinicWallet.Create(_applicationId, 100m, "INR");
        wallet.Credit(300m, DateTime.UtcNow);
        _wallets.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var balance = await CreateHandler().Handle(Command(250m), CancellationToken.None);

        balance.Should().Be(550m);
        wallet.Balance.Should().Be(550m);
        _wallets.Verify(r => r.AddAsync(It.IsAny<ClinicWallet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReplayedReference_ReturnsTheCurrentBalanceWithoutCreditingAgain()
    {
        var referenceId = Guid.NewGuid();
        var wallet = ClinicWallet.Create(_applicationId, 100m, "INR");
        wallet.Credit(250m, DateTime.UtcNow);
        _ledger.Setup(r => r.ExistsAsync(
                _applicationId, WalletReferenceType.AdminCredit, referenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _wallets.Setup(r => r.GetByApplicationIdNoTrackingAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var balance = await CreateHandler().Handle(Command(250m, referenceId), CancellationToken.None);

        balance.Should().Be(250m, "answering with the balance is safe; crediting again is not");
        wallet.Balance.Should().Be(250m);
        _ledgerRows.Should().BeEmpty();
        _wallets.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReplayedReferenceOnAClinicWithNoWallet_ReturnsZero()
    {
        var referenceId = Guid.NewGuid();
        _ledger.Setup(r => r.ExistsAsync(
                _applicationId, WalletReferenceType.AdminCredit, referenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _wallets.Setup(r => r.GetByApplicationIdNoTrackingAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicWallet?)null);

        var balance = await CreateHandler().Handle(Command(250m, referenceId), CancellationToken.None);

        balance.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_LedgerRow_IsAttributedToTheAdminAndCarriesTheReference()
    {
        var referenceId = Guid.NewGuid();

        await CreateHandler().Handle(Command(400m, referenceId), CancellationToken.None);

        var row = _ledgerRows.Should().ContainSingle().Subject;
        row.ReferenceType.Should().Be(WalletReferenceType.AdminCredit);
        row.ReferenceId.Should().Be(referenceId);
        row.CreatedByAdminEmail.Should().Be("admin@example.test");
        row.CreatedByUserId.Should().BeNull();
        row.Amount.Should().Be(400m);
        row.BalanceAfter.Should().Be(400m);
    }

    [Fact]
    public void CreditClinicWalletCommand_ActionLogContext_CarriesIdentifiersOnly()
    {
        // The amount and the reference identify the movement. The admin's free-text reason lives on the
        // entity and in the admin audit — it must never be copied into an action log.
        var referenceId = Guid.NewGuid();
        var command = new CreditClinicWalletCommand(
            _applicationId, 750m, "Goodwill credit", referenceId, Guid.NewGuid(), "admin@example.test");

        command.ActionLogContext.Should().Be(
            $"applicationId={_applicationId}; amount=750; referenceId={referenceId}; admin=admin@example.test");
        command.ActionLogContext.Should().NotContain("Goodwill credit");
    }
}
