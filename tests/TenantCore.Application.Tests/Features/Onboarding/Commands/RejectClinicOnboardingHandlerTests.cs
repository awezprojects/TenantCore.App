using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Commands;

public class RejectClinicOnboardingHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public RejectClinicOnboardingHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private RejectClinicOnboardingHandler CreateHandler() => new(_requestRepository.Object, _workflowEnqueuer.Object);

    private static ClinicOnboardingRequest CreateSubmittedRequest() => ClinicOnboardingRequest.Submit(
        Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
        null, null, null, "Doc", "MR1", "MCI", 1, null, null);

    [Fact]
    public async Task Handle_SubmittedRequestWithoutPayment_RejectsAndEnqueuesOnlyEmail()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        await handler.Handle(new RejectClinicOnboardingCommand(request.Id, "Not a good fit", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Rejected);
        request.RejectionReason.Should().Be("Not a good fit");

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, It.Is<string>(k => k.Contains("rejected")), nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ApprovedWithOpenPayment_EnqueuesCancelPaymentLinkAndEmail()
    {
        var request = CreateSubmittedRequest();
        var paymentId = Guid.NewGuid();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        await handler.Handle(new RejectClinicOnboardingCommand(request.Id, "Policy violation", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Rejected);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, $"cancel-link:{paymentId}", nameof(SubscriptionPayment), paymentId,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AwaitingPaymentWithOpenPayment_CanBeRejected()
    {
        var request = CreateSubmittedRequest();
        var paymentId = Guid.NewGuid();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        request.MarkAwaitingPayment();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        await handler.Handle(new RejectClinicOnboardingCommand(request.Id, "Policy violation", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Rejected);
    }

    [Fact]
    public async Task Handle_InvalidStatus_ThrowsInvalidOperationException()
    {
        var request = CreateSubmittedRequest();
        request.ApproveWithTrial(null, null, Guid.NewGuid(), "admin@example.test"); // Provisioning — not rejectable
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var action = () => handler.Handle(new RejectClinicOnboardingCommand(request.Id, "reason", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsNotFoundException()
    {
        var requestId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new RejectClinicOnboardingCommand(requestId, "reason", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
