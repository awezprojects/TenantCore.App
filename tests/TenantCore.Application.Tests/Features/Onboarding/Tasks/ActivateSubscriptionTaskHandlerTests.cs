using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Tasks;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Tasks;

public class ActivateSubscriptionTaskHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IClinicSubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public ActivateSubscriptionTaskHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ActivateSubscriptionTaskHandler CreateHandler() =>
        new(_requestRepository.Object, _paymentRepository.Object, _planRepository.Object, _subscriptionRepository.Object, _workflowEnqueuer.Object);

    private static SubscriptionPlan CreatePlan(bool isTrial = false, decimal price = 999m, int durationDays = 30) =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), isTrial ? SubscriptionPlanCode.Trial : SubscriptionPlanCode.Monthly,
            isTrial ? "Trial" : "Monthly", "d", durationDays, price, "INR", isTrial, false, 1);

    private static ClinicOnboardingRequest CreateProvisionedTrialRequest(Guid applicationId)
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.ApproveWithTrial(null, null, Guid.NewGuid(), "admin@example.test");
        request.SetProvisionedClinic(applicationId);
        return request;
    }

    private static WorkflowTask CreateTrialTask(Guid requestId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.ActivateSubscription, $"activate-subscription:trial:{requestId}", nameof(ClinicOnboardingRequest), requestId);

    private static WorkflowTask CreatePaymentTask(Guid paymentId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.ActivateSubscription, $"activate-subscription:{paymentId}", nameof(SubscriptionPayment), paymentId);

    [Fact]
    public async Task HandleAsync_TrialGrant_ActivatesAndEnqueuesClinicReadyEmail()
    {
        var applicationId = Guid.NewGuid();
        var request = CreateProvisionedTrialRequest(applicationId);
        var trialPlan = CreatePlan(isTrial: true, price: 0m, durationDays: 14);

        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _subscriptionRepository.Setup(r => r.GetByOnboardingRequestIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _planRepository.Setup(r => r.GetByCodeAsync(SubscriptionPlanCode.Trial, It.IsAny<CancellationToken>())).ReturnsAsync(trialPlan);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTrialTask(request.Id), CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Active);
        _subscriptionRepository.Verify(r => r.AddAsync(It.Is<ClinicSubscription>(s => s.ApplicationId == applicationId && s.OnboardingRequestId == request.Id), It.IsAny<CancellationToken>()), Times.Once);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, It.Is<string>(k => k.Contains("clinic-ready")), nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TrialGrant_NotProvisionedYet_ThrowsRetryableException()
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.ApproveWithTrial(null, null, Guid.NewGuid(), "admin@example.test"); // never provisioned
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTrialTask(request.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_TrialGrant_ExistingSubscriptionForRequest_LinksRatherThanDuplicating()
    {
        var applicationId = Guid.NewGuid();
        var request = CreateProvisionedTrialRequest(applicationId);
        var trialPlan = CreatePlan(isTrial: true, price: 0m, durationDays: 14);
        var existing = ClinicSubscription.Create(applicationId, trialPlan, DateTime.UtcNow, "Clinic", "e@x.com", "n", onboardingRequestId: request.Id);

        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _subscriptionRepository.Setup(r => r.GetByOnboardingRequestIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTrialTask(request.Id), CancellationToken.None);

        request.ClinicSubscriptionId.Should().Be(existing.Id);
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PaidOnboarding_ActivatesAndEnqueuesClinicReadyEmail()
    {
        var applicationId = Guid.NewGuid();
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        var plan = CreatePlan(price: 999m);
        request.ApproveWithPaidPlan(plan.Id, plan.Code, plan.Price, plan.Price, null, null, null, Guid.NewGuid(), "admin@example.test");
        var payment = SubscriptionPayment.CreateForOnboarding(request.Id, plan.Id, plan.Code, plan.Name, plan.Price, plan.Price, "INR", request.RequesterName, request.RequesterEmail, request.RequesterPhone);
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        payment.MarkPaid("pay_1", "upi");
        request.SetCurrentPayment(payment.Id);
        request.SetProvisionedClinic(applicationId);

        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _subscriptionRepository.Setup(r => r.GetByPaymentIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _subscriptionRepository.Setup(r => r.GetLatestForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(CreatePaymentTask(payment.Id), CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Active);
        payment.ClinicSubscriptionId.Should().NotBeNull();
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, It.Is<string>(k => k.Contains("clinic-ready")), nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingSubscriptionForPayment_LinksRatherThanDuplicating()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan(price: 999m);
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.ApproveWithPaidPlan(plan.Id, plan.Code, plan.Price, plan.Price, null, null, null, Guid.NewGuid(), "admin@example.test");
        var payment = SubscriptionPayment.CreateForOnboarding(request.Id, plan.Id, plan.Code, plan.Name, plan.Price, plan.Price, "INR", request.RequesterName, request.RequesterEmail, request.RequesterPhone);
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        payment.MarkPaid("pay_1", "upi");
        request.SetCurrentPayment(payment.Id);
        request.SetProvisionedClinic(applicationId);

        var existing = ClinicSubscription.Create(applicationId, plan, DateTime.UtcNow, "Clinic", "e@x.com", "n", subscriptionPaymentId: payment.Id);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _subscriptionRepository.Setup(r => r.GetByPaymentIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        await handler.HandleAsync(CreatePaymentTask(payment.Id), CancellationToken.None);

        payment.ClinicSubscriptionId.Should().Be(existing.Id);
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Renewal_ActiveExistingSubscription_StartsDayAfterItEnds()
    {
        var applicationId = Guid.NewGuid();
        var oldPlan = CreatePlan(price: 999m, durationDays: 30);
        var newPlan = CreatePlan(price: 999m, durationDays: 30);
        var existing = ClinicSubscription.Create(applicationId, oldPlan, DateTime.UtcNow.AddDays(-10), "Clinic", "e@x.com", "n");
        var expectedStart = existing.EndDate.AddDays(1);

        var payment = SubscriptionPayment.CreateForRenewal(applicationId, newPlan.Id, newPlan.Code, newPlan.Name, newPlan.Price, "INR", "Admin", "admin@clinic.test", "9876543210", Guid.NewGuid());
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        payment.MarkPaid("pay_1", "card");

        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _subscriptionRepository.Setup(r => r.GetByPaymentIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _planRepository.Setup(r => r.GetByIdAsync(newPlan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newPlan);
        _subscriptionRepository.Setup(r => r.GetLatestForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        ClinicSubscription? created = null;
        _subscriptionRepository.Setup(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicSubscription, CancellationToken>((s, _) => created = s)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.HandleAsync(CreatePaymentTask(payment.Id), CancellationToken.None);

        created.Should().NotBeNull();
        created!.StartDate.Should().Be(expectedStart);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, It.Is<string>(k => k.Contains("renewal-activated")), nameof(SubscriptionPayment), payment.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Renewal_NoExistingSubscription_StartsImmediately()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan(price: 999m, durationDays: 30);
        var payment = SubscriptionPayment.CreateForRenewal(applicationId, plan.Id, plan.Code, plan.Name, plan.Price, "INR", "Admin", "admin@clinic.test", "9876543210", Guid.NewGuid());
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        payment.MarkPaid("pay_1", "card");

        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _subscriptionRepository.Setup(r => r.GetByPaymentIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.GetLatestForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);

        ClinicSubscription? created = null;
        _subscriptionRepository.Setup(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicSubscription, CancellationToken>((s, _) => created = s)
            .Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var handler = CreateHandler();
        await handler.HandleAsync(CreatePaymentTask(payment.Id), CancellationToken.None);
        var after = DateTime.UtcNow;

        created.Should().NotBeNull();
        created!.StartDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task HandleAsync_PaymentNotPaidYet_ThrowsRetryableException()
    {
        var plan = CreatePlan(price: 999m);
        var payment = SubscriptionPayment.CreateForRenewal(Guid.NewGuid(), plan.Id, plan.Code, plan.Name, plan.Price, "INR", "Admin", "admin@clinic.test", "9876543210", Guid.NewGuid());
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreatePaymentTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }
}
