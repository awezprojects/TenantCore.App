using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Handlers;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Commands;

public class ApproveClinicOnboardingHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public ApproveClinicOnboardingHandlerTests()
    {
        _paymentGateway.Setup(g => g.IsConfigured).Returns(true);
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ApproveClinicOnboardingHandler CreateHandler() =>
        new(_requestRepository.Object, _planRepository.Object, _paymentRepository.Object, _paymentGateway.Object, _workflowEnqueuer.Object);

    private static ClinicOnboardingRequest CreateSubmittedRequest() => ClinicOnboardingRequest.Submit(
        Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
        null, null, null, "Doc", "MR1", "MCI", 1, null, null);

    private static SubscriptionPlan CreatePlan(bool isTrial = false, decimal price = 999m, bool isActive = true)
    {
        var plan = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), isTrial ? SubscriptionPlanCode.Trial : SubscriptionPlanCode.Monthly,
            isTrial ? "Trial" : "Monthly", "d", 30, price, "INR", isTrial, false, 1);
        if (!isActive)
            typeof(SubscriptionPlan).GetProperty(nameof(SubscriptionPlan.IsActive))!.SetValue(plan, false);
        return plan;
    }

    [Fact]
    public async Task Handle_PaidPlanApproval_CreatesPendingPaymentForAdminAmountAndSnapshotsListPrice()
    {
        var request = CreateSubmittedRequest();
        var plan = CreatePlan(price: 999m);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        SubscriptionPayment? saved = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, plan.Id, false, 750m, "Discount agreed", null, "looks good", Guid.NewGuid(), "admin@example.test");
        await handler.Handle(command, CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Approved);
        request.PlanListPrice.Should().Be(999m);
        request.ApprovedAmount.Should().Be(750m);
        request.AmountReason.Should().Be("Discount agreed");
        request.IsTrialGrant.Should().BeFalse();

        saved.Should().NotBeNull();
        saved!.Amount.Should().Be(750m); // the admin's amount, not the plan's list price
        saved.PlanListPrice.Should().Be(999m);
        saved.Status.Should().Be(SubscriptionPaymentStatus.Pending);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, $"create-link:{saved.Id}", nameof(SubscriptionPayment), saved.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _requestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TrialGrant_MovesToProvisioningAndEnqueuesProvisionClinic()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, null, true, null, null, null, "trial ok", Guid.NewGuid(), "admin@example.test");
        await handler.Handle(command, CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Provisioning);
        request.IsTrialGrant.Should().BeTrue();

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ClinicCodeOverride_IsStoredOnApproval()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, null, true, null, null, "OVERRIDE1", "trial ok", Guid.NewGuid(), "admin@example.test");
        await handler.Handle(command, CancellationToken.None);

        request.EffectiveClinicCode.Should().Be("OVERRIDE1");
    }

    [Fact]
    public async Task Handle_RequestNotSubmitted_ThrowsInvalidOperationException()
    {
        var request = CreateSubmittedRequest();
        request.ApproveWithTrial(null, null, Guid.NewGuid(), "admin@example.test"); // now Provisioning
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, null, true, null, null, null, null, Guid.NewGuid(), "admin@example.test");
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_PlanInactiveOrMissing_ThrowsNotFoundException()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _planRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPlan?)null);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, Guid.NewGuid(), false, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PaymentsNotConfiguredForPaidPlan_ThrowsInvalidOperationException()
    {
        var request = CreateSubmittedRequest();
        _paymentGateway.Setup(g => g.IsConfigured).Returns(false);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, Guid.NewGuid(), false, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _planRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TrialPlanSelectedWithoutGrantTrial_ThrowsInvalidOperationException()
    {
        var request = CreateSubmittedRequest();
        var trialPlan = CreatePlan(isTrial: true, price: 0m);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _planRepository.Setup(r => r.GetByIdAsync(trialPlan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trialPlan);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, trialPlan.Id, false, 0m, null, null, null, Guid.NewGuid(), "admin@example.test");
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_AmountDiffersFromListPriceWithoutReason_ThrowsInvalidOperationException()
    {
        var request = CreateSubmittedRequest();
        var plan = CreatePlan(price: 999m);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, plan.Id, false, 750m, null, null, null, Guid.NewGuid(), "admin@example.test");
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_AmountEqualsListPrice_ReasonNotRequired()
    {
        var request = CreateSubmittedRequest();
        var plan = CreatePlan(price: 999m);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, plan.Id, false, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        await handler.Handle(command, CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Approved);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsNotFoundException()
    {
        var requestId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(requestId, null, true, null, null, null, null, Guid.NewGuid(), "admin@example.test");
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
