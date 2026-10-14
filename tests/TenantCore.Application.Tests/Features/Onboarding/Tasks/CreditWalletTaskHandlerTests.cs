using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Application.Features.Onboarding.Tasks;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Tasks;

/// <summary>
/// Credits the wallet once a <see cref="PaymentPurpose.WalletTopUp"/> payment is confirmed.
/// Idempotent twice over on purpose: the ledger pre-check catches a replayed task, and the unique
/// index on (clinic, reference) catches a genuine race. A credit and its ledger row are always one
/// SaveChanges.
/// </summary>
public class CreditWalletTaskHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _payments = new();
    private readonly Mock<IClinicWalletRepository> _wallets = new();
    private readonly Mock<IClinicWalletTransactionRepository> _ledger = new();
    private readonly List<ClinicWalletTransaction> _ledgerRows = [];
    private readonly Guid _applicationId = Guid.NewGuid();

    public CreditWalletTaskHandlerTests()
    {
        _payments.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _wallets.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _wallets.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicWallet?)null);
        _ledger.Setup(r => r.AddAsync(It.IsAny<ClinicWalletTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicWalletTransaction, CancellationToken>((row, _) => _ledgerRows.Add(row))
            .Returns(Task.CompletedTask);
    }

    private CreditWalletTaskHandler CreateHandler() =>
        new(_payments.Object, _wallets.Object, _ledger.Object, Options.Create(new UsageBillingOptions()));

    private SubscriptionPayment GivenPaidTopUp(decimal amount = 500m)
    {
        var payment = SubscriptionPayment.CreateForWalletTopUp(
            _applicationId, amount, "INR", "Dr Mehta", "doctor@example.test", "9876543210", "Sunrise Clinic");
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        payment.MarkPaid("pay_1", "card");

        _payments.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        return payment;
    }

    private static WorkflowTask CreditTask(Guid paymentId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.CreditWallet, $"credit-wallet:{paymentId}",
            nameof(SubscriptionPayment), paymentId);

    [Fact]
    public void TaskType_IsCreditWallet() =>
        CreateHandler().TaskType.Should().Be(WorkflowTaskType.CreditWallet);

    [Fact]
    public async Task HandleAsync_PaidTopUp_CreditsTheWalletAndWritesOneLedgerRow()
    {
        var payment = GivenPaidTopUp(500m);

        await CreateHandler().HandleAsync(CreditTask(payment.Id), CancellationToken.None);

        _wallets.Verify(r => r.AddAsync(It.IsAny<ClinicWallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _ledgerRows.Should().ContainSingle();
        _ledgerRows[0].ReferenceType.Should().Be(WalletReferenceType.TopUpPayment);
        _ledgerRows[0].ReferenceId.Should().Be(payment.Id);
        _ledgerRows[0].Amount.Should().Be(500m);
        _ledgerRows[0].BalanceAfter.Should().Be(500m);
        _wallets.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once,
            "one save — the balance and the ledger row can never diverge");
    }

    [Fact]
    public async Task HandleAsync_ReplayedTask_DoesNotMoveMoneyTwice()
    {
        var payment = GivenPaidTopUp(500m);
        _ledger.Setup(r => r.ExistsAsync(
                _applicationId, WalletReferenceType.TopUpPayment, payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateHandler().HandleAsync(CreditTask(payment.Id), CancellationToken.None);

        _ledgerRows.Should().BeEmpty();
        _wallets.Verify(r => r.AddAsync(It.IsAny<ClinicWallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _wallets.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExistingWallet_AddsToTheBalanceInsteadOfCreatingOne()
    {
        var payment = GivenPaidTopUp(200m);
        var wallet = ClinicWallet.Create(_applicationId, 100m, "INR");
        wallet.Credit(300m, DateTime.UtcNow);
        _wallets.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        await CreateHandler().HandleAsync(CreditTask(payment.Id), CancellationToken.None);

        wallet.Balance.Should().Be(500m);
        _ledgerRows.Single().BalanceAfter.Should().Be(500m);
        _wallets.Verify(r => r.AddAsync(It.IsAny<ClinicWallet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PaymentNotPaidYet_ThrowsRetryableException()
    {
        var payment = SubscriptionPayment.CreateForWalletTopUp(
            _applicationId, 500m, "INR", "Dr Mehta", "doctor@example.test", null, "Sunrise Clinic");
        _payments.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var action = () => CreateHandler().HandleAsync(CreditTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_PaymentMissing_FailsPermanently()
    {
        var missingId = Guid.NewGuid();
        _payments.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        var action = () => CreateHandler().HandleAsync(CreditTask(missingId), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_PaymentThatIsNotATopUp_FailsPermanently()
    {
        var payment = SubscriptionPayment.CreateForRegistrationFee(
            _applicationId, 1000m, "INR", "Dr Mehta", "doctor@example.test", null, "Sunrise Clinic");
        payment.SetLink("plink_2", "https://razorpay.test/pay/plink_2", DateTime.UtcNow.AddDays(7));
        payment.MarkPaid("pay_2", "card");
        _payments.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var action = () => CreateHandler().HandleAsync(CreditTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<PermanentWorkflowException>();
        exception.Which.Message.Should().Contain("not a wallet top-up");
    }
}
