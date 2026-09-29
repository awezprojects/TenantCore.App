using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Handlers;
using TenantCore.Application.Features.Onboarding.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Onboarding.Commands;

public class RetryClinicOnboardingHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IWorkflowTaskRepository> _workflowTaskRepository = new();
    private readonly Mock<IOnboardingSelfHealer> _selfHealer = new();

    public RetryClinicOnboardingHandlerTests()
    {
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private RetryClinicOnboardingHandler CreateHandler() => new(_requestRepository.Object, _workflowTaskRepository.Object, _selfHealer.Object);

    private static ClinicOnboardingRequest CreateRequestNeedingAttention()
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.FlagAttention("clinic code taken");
        return request;
    }

    [Fact]
    public async Task Handle_FailedTaskExists_ResetsTaskAndClearsAttention()
    {
        var request = CreateRequestNeedingAttention();
        var failedTask = WorkflowTask.Enqueue(WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id);
        failedTask.TryLease("instance-1", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow);
        failedTask.Fail("clinic code taken");

        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _workflowTaskRepository.Setup(r => r.GetFailedForAggregateAsync(nameof(ClinicOnboardingRequest), request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([failedTask]);

        var handler = CreateHandler();
        await handler.Handle(new RetryClinicOnboardingCommand(request.Id, null, Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        request.NeedsAttention.Should().BeFalse();
        failedTask.Status.Should().Be(WorkflowTaskStatus.Pending);
        failedTask.AttemptCount.Should().Be(0);
        _selfHealer.Verify(h => h.HealRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ClinicCodeOverrideProvided_IsApplied()
    {
        var request = CreateRequestNeedingAttention();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _workflowTaskRepository.Setup(r => r.GetFailedForAggregateAsync(nameof(ClinicOnboardingRequest), request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        await handler.Handle(new RetryClinicOnboardingCommand(request.Id, "NEWCODE1", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        request.EffectiveClinicCode.Should().Be("NEWCODE1");
    }

    [Fact]
    public async Task Handle_NoFailedTask_InvokesSelfHealerToReEnqueueNextStep()
    {
        var request = CreateRequestNeedingAttention();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _workflowTaskRepository.Setup(r => r.GetFailedForAggregateAsync(nameof(ClinicOnboardingRequest), request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        await handler.Handle(new RetryClinicOnboardingCommand(request.Id, null, Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        request.NeedsAttention.Should().BeFalse();
        _selfHealer.Verify(h => h.HealRequestAsync(request.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsNotFoundException()
    {
        var requestId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new RetryClinicOnboardingCommand(requestId, null, Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
