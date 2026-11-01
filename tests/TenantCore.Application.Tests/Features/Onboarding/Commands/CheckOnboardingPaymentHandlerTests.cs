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

public class CheckOnboardingPaymentHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public CheckOnboardingPaymentHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CheckOnboardingPaymentHandler CreateHandler() => new(_requestRepository.Object, _workflowEnqueuer.Object);

    private static ClinicOnboardingRequest CreateAwaitingPaymentRequest(Guid userId, Guid paymentId)
    {
        var request = ClinicOnboardingRequest.Submit(
            userId, "Requester", "requester@example.test", "9876543210", "Clinic", "CODE1", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        request.MarkAwaitingPayment();
        return request;
    }

    [Fact]
    public async Task Handle_OwnerInAwaitingPayment_EnqueuesConfirmPaymentForCurrentPayment()
    {
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var request = CreateAwaitingPaymentRequest(userId, paymentId);
        _requestRepository.Setup(r => r.GetByIdForUserAsync(request.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        await handler.Handle(new CheckOnboardingPaymentCommand(request.Id, userId), CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ConfirmPayment, $"confirm-payment:{paymentId}", nameof(SubscriptionPayment), paymentId,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _requestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SecondCallWithinThirtySeconds_ThrowsTooManyRequestsException()
    {
        var userId = Guid.NewGuid();
        var request = CreateAwaitingPaymentRequest(userId, Guid.NewGuid());
        request.RecordPaymentCheck(); // simulates a check that just happened
        _requestRepository.Setup(r => r.GetByIdForUserAsync(request.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var action = () => handler.Handle(new CheckOnboardingPaymentCommand(request.Id, userId), CancellationToken.None);

        await action.Should().ThrowAsync<TooManyRequestsException>();
    }

    [Fact]
    public async Task Handle_WrongStatus_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var request = ClinicOnboardingRequest.Submit(
            userId, "Requester", "requester@example.test", "9876543210", "Clinic", "CODE1", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null); // still Submitted
        _requestRepository.Setup(r => r.GetByIdForUserAsync(request.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var action = () => handler.Handle(new CheckOnboardingPaymentCommand(request.Id, userId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_AnotherUsersRequest_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdForUserAsync(requestId, userId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new CheckOnboardingPaymentCommand(requestId, userId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
