using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Onboarding.Commands;

public class RetryWorkflowTaskHandlerTests
{
    private readonly Mock<IWorkflowTaskRepository> _workflowTaskRepository = new();
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();

    public RetryWorkflowTaskHandlerTests()
    {
        _workflowTaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private RetryWorkflowTaskHandler CreateHandler() => new(_workflowTaskRepository.Object, _requestRepository.Object);

    private static WorkflowTask CreateFailedTask(string aggregateType, Guid aggregateId)
    {
        var task = WorkflowTask.Enqueue(WorkflowTaskType.ProvisionClinic, $"provision:{aggregateId}", aggregateType, aggregateId);
        task.TryLease("instance-1", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow);
        task.Fail("permanent failure");
        return task;
    }

    [Fact]
    public async Task Handle_FailedTaskForOnboardingRequest_ResetsTaskAndClearsRequestAttention()
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.FlagAttention("boom");
        var task = CreateFailedTask(nameof(ClinicOnboardingRequest), request.Id);

        _workflowTaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        await handler.Handle(new RetryWorkflowTaskCommand(task.Id, Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        task.Status.Should().Be(WorkflowTaskStatus.Pending);
        task.AttemptCount.Should().Be(0);
        request.NeedsAttention.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FailedTaskWithNoOnboardingRequestAggregate_ResetsTaskOnly()
    {
        var task = CreateFailedTask(nameof(SubscriptionPayment), Guid.NewGuid());
        _workflowTaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var handler = CreateHandler();
        await handler.Handle(new RetryWorkflowTaskCommand(task.Id, Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        task.Status.Should().Be(WorkflowTaskStatus.Pending);
        _requestRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonFailedTask_ThrowsInvalidOperationException()
    {
        var task = WorkflowTask.Enqueue(WorkflowTaskType.SendEmail, "email:x", nameof(SubscriptionPayment), Guid.NewGuid()); // Pending
        _workflowTaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var handler = CreateHandler();
        var action = () => handler.Handle(new RetryWorkflowTaskCommand(task.Id, Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_TaskNotFound_ThrowsNotFoundException()
    {
        var taskId = Guid.NewGuid();
        _workflowTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync((WorkflowTask?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new RetryWorkflowTaskCommand(taskId, Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
