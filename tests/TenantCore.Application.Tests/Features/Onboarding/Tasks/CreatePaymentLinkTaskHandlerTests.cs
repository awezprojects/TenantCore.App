using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Tasks;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Tasks;

public class CreatePaymentLinkTaskHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();
    private readonly Mock<IConfiguration> _configuration = new();

    public CreatePaymentLinkTaskHandlerTests()
    {
        _paymentGateway.Setup(g => g.IsConfigured).Returns(true);
        _configuration.Setup(c => c.GetSection("Razorpay:PaymentLinkExpiryDays")).Returns(Mock.Of<IConfigurationSection>(s => s.Value == null));
        _configuration.Setup(c => c["Razorpay:CallbackUrl"]).Returns("https://app.example.test/clinic-requests");
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CreatePaymentLinkTaskHandler CreateHandler() =>
        new(_paymentRepository.Object, _requestRepository.Object, _paymentGateway.Object, _workflowEnqueuer.Object, _configuration.Object);

    private static ClinicOnboardingRequest CreateApprovedRequest(Guid paymentId)
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        return request;
    }

    private static SubscriptionPayment CreatePendingPayment(Guid requestId) =>
        SubscriptionPayment.CreateForOnboarding(requestId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, 999m, "INR", "Dr. Jane", "jane@example.test", "9876543210");

    private static WorkflowTask CreateTask(Guid paymentId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.CreatePaymentLink, $"create-link:{paymentId}", nameof(SubscriptionPayment), paymentId);

    [Fact]
    public async Task HandleAsync_HappyPath_SetsLinkMarksAwaitingPaymentAndEnqueuesEmail()
    {
        var payment = CreatePendingPayment(Guid.NewGuid());
        var request = CreateApprovedRequest(payment.Id);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var link = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "created", null, null, null, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.CreatePaymentLinkAsync(
            payment.Id.ToString("N"), payment.AmountInMinorUnits, payment.Currency, It.IsAny<string>(),
            payment.PayerName, payment.PayerEmail, payment.PayerPhone, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GatewayResult.Ok(), link));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        payment.Status.Should().Be(SubscriptionPaymentStatus.LinkCreated);
        payment.PaymentLinkUrl.Should().Be("https://razorpay.test/pay/plink_1");
        request.Status.Should().Be(ClinicOnboardingStatus.AwaitingPayment);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, $"email:payment-link:{payment.Id}", nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyLinkCreated_DoesNothingAndNeverCallsGateway()
    {
        var payment = CreatePendingPayment(Guid.NewGuid());
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        _paymentGateway.Verify(g => g.CreatePaymentLinkAsync(
            It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReferenceResponse_AdoptsExistingLinkInsteadOfFailing()
    {
        var payment = CreatePendingPayment(Guid.NewGuid());
        var request = CreateApprovedRequest(payment.Id);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        _paymentGateway.Setup(g => g.CreatePaymentLinkAsync(
            It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GatewayResult.Permanent("DUPLICATE_REFERENCE"), (PaymentLinkInfo?)null));

        var existingLink = new PaymentLinkInfo("plink_existing", "https://razorpay.test/pay/plink_existing", "created", null, null, null, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.FindPaymentLinkByReferenceAsync(payment.Id.ToString("N"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GatewayResult.Ok(), existingLink));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        payment.GatewayPaymentLinkId.Should().Be("plink_existing");
        payment.Status.Should().Be(SubscriptionPaymentStatus.LinkCreated);
    }

    [Fact]
    public async Task HandleAsync_TransientGatewayError_ThrowsRetryableException()
    {
        var payment = CreatePendingPayment(Guid.NewGuid());
        var request = CreateApprovedRequest(payment.Id);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        _paymentGateway.Setup(g => g.CreatePaymentLinkAsync(
            It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GatewayResult.Transient("timeout"), (PaymentLinkInfo?)null));

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_PermanentGatewayError_ThrowsPermanentWorkflowException()
    {
        var payment = CreatePendingPayment(Guid.NewGuid());
        var request = CreateApprovedRequest(payment.Id);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        _paymentGateway.Setup(g => g.CreatePaymentLinkAsync(
            It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GatewayResult.Permanent("bad request"), (PaymentLinkInfo?)null));

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_PaymentNotFound_ThrowsPermanentWorkflowException()
    {
        var paymentId = Guid.NewGuid();
        _paymentRepository.Setup(r => r.GetByIdAsync(paymentId, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(paymentId), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_GatewayNotConfigured_ThrowsRetryableException()
    {
        var payment = CreatePendingPayment(Guid.NewGuid());
        _paymentGateway.Setup(g => g.IsConfigured).Returns(false);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }
}
