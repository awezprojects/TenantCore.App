using FluentAssertions;
using MediatR;
using Moq;
using TenantCore.Application.Features.Onboarding.Tasks;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Tasks;

/// <summary>
/// Marks the one-time registration fee charged once its payment is confirmed. A replay is a no-op
/// rather than a second charge, a waiver always wins, and a clinic with no account row is left alone
/// — creating one here would be inventing state.
/// </summary>
public class RecordRegistrationFeeTaskHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _payments = new();
    private readonly Mock<IClinicAccountRepository> _accounts = new();
    private readonly Mock<ISender> _sender = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public RecordRegistrationFeeTaskHandlerTests()
    {
        _payments.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _accounts.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private RecordRegistrationFeeTaskHandler CreateHandler() =>
        new(_payments.Object, _accounts.Object, _sender.Object);

    private SubscriptionPayment GivenPaidFee(decimal amount = 1000m)
    {
        var payment = SubscriptionPayment.CreateForRegistrationFee(
            _applicationId, amount, "INR", "Dr Mehta", "doctor@example.test", "9876543210", "Sunrise Clinic");
        payment.SetLink("plink_9", "https://razorpay.test/pay/plink_9", DateTime.UtcNow.AddDays(7));
        payment.MarkPaid("pay_9", "card");

        _payments.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        return payment;
    }

    private ClinicAccount GivenAccount()
    {
        var account = ClinicAccount.CreateDefault(_applicationId);
        _accounts.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        return account;
    }

    private static WorkflowTask FeeTask(Guid paymentId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.RecordRegistrationFee, $"record-registration-fee:{paymentId}",
            nameof(SubscriptionPayment), paymentId);

    [Fact]
    public void TaskType_IsRecordRegistrationFee() =>
        CreateHandler().TaskType.Should().Be(WorkflowTaskType.RecordRegistrationFee);

    [Fact]
    public async Task HandleAsync_PaidFeeDue_MarksItChargedAndSaves()
    {
        var payment = GivenPaidFee();
        var account = GivenAccount();

        await CreateHandler().HandleAsync(FeeTask(payment.Id), CancellationToken.None);

        account.RegistrationFeeChargedAt.Should().NotBeNull();
        account.RegistrationFeeChargedBy.Should().Be($"payment:{payment.Id}");
        account.HasRegistrationFeeDue.Should().BeFalse();
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReplayedTask_FeeAlreadyCharged_IsANoOp()
    {
        var payment = GivenPaidFee();
        var account = GivenAccount();
        account.MarkRegistrationFeeCharged("payment:earlier", DateTime.UtcNow);

        await CreateHandler().HandleAsync(FeeTask(payment.Id), CancellationToken.None);

        account.RegistrationFeeChargedBy.Should().Be("payment:earlier");
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WaivedFee_IsNeverCharged()
    {
        var payment = GivenPaidFee();
        var account = GivenAccount();
        account.WaiveRegistrationFee("Startup partner", "admin@example.test");

        await CreateHandler().HandleAsync(FeeTask(payment.Id), CancellationToken.None);

        account.RegistrationFeeChargedAt.Should().BeNull();
        account.RegistrationFeeWaived.Should().BeTrue();
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ClinicWithNoAccountRow_IsLeftAlone()
    {
        var payment = GivenPaidFee();
        _accounts.Setup(r => r.GetByApplicationIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicAccount?)null);

        await CreateHandler().HandleAsync(FeeTask(payment.Id), CancellationToken.None);

        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _accounts.Verify(r => r.AddAsync(It.IsAny<ClinicAccount>(), It.IsAny<CancellationToken>()), Times.Never,
            "the fee was only ever a plan default — there is nothing to mark");
    }

    [Fact]
    public async Task HandleAsync_PaymentMissing_FailsPermanently()
    {
        var missingId = Guid.NewGuid();
        _payments.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        var action = () => CreateHandler().HandleAsync(FeeTask(missingId), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_PaymentThatIsNotARegistrationFee_FailsPermanently()
    {
        var payment = SubscriptionPayment.CreateForWalletTopUp(
            _applicationId, 500m, "INR", "Dr Mehta", "doctor@example.test", null, "Sunrise Clinic");
        payment.SetLink("plink_3", "https://razorpay.test/pay/plink_3", DateTime.UtcNow.AddDays(7));
        payment.MarkPaid("pay_3", "card");
        _payments.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var action = () => CreateHandler().HandleAsync(FeeTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<PermanentWorkflowException>();
        exception.Which.Message.Should().Contain("not a registration fee");
    }

    [Fact]
    public async Task HandleAsync_PaymentNotPaidYet_ThrowsRetryableException()
    {
        var payment = SubscriptionPayment.CreateForRegistrationFee(
            _applicationId, 1000m, "INR", "Dr Mehta", "doctor@example.test", null, "Sunrise Clinic");
        _payments.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var action = () => CreateHandler().HandleAsync(FeeTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }
}
