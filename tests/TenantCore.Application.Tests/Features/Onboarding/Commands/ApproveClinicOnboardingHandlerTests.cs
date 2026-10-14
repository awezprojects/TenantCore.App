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

public class ApproveClinicOnboardingHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public ApproveClinicOnboardingHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ApproveClinicOnboardingHandler CreateHandler() =>
        new(_requestRepository.Object, _workflowEnqueuer.Object);

    private static ClinicOnboardingRequest CreateSubmittedRequest() => ClinicOnboardingRequest.Submit(
        Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
        null, null, null, "Doc", "MR1", "MCI", 1, null, null);

    [Fact]
    public async Task Handle_SubscriptionApproval_MovesToProvisioningAndEnqueuesProvisionClinic()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, false, null, "looks good", Guid.NewGuid(), "admin@example.test",
            BillingModeOverride: BillingMode.Subscription, RegistrationFeeOverride: 500m, FreeTestDaysOverride: 30);
        await handler.Handle(command, CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Provisioning);
        request.IsTrialGrant.Should().BeFalse();
        request.ApprovalBillingMode.Should().Be(BillingMode.Subscription);
        request.ApprovalRegistrationFee.Should().Be(500m);
        request.ApprovalFreeTestDays.Should().Be(30);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _requestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PayAsYouGoApproval_MovesToProvisioningWithPaygBillingIntent()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, false, null, null, Guid.NewGuid(), "admin@example.test",
            BillingModeOverride: BillingMode.PayAsYouGo, PayAsYouGoOpdRate: 10m);
        await handler.Handle(command, CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Provisioning);
        request.IsTrialGrant.Should().BeFalse();
        request.ApprovalBillingMode.Should().Be(BillingMode.PayAsYouGo);
        request.ApprovalOpdRate.Should().Be(10m);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TrialGrant_MovesToProvisioningWithTrialFlagAndEnqueuesProvisionClinic()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, true, null, "trial ok", Guid.NewGuid(), "admin@example.test");
        await handler.Handle(command, CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Provisioning);
        request.IsTrialGrant.Should().BeTrue();

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TrialGrantWithBillingIntent_StoresBillingIntentAlongside()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, true, null, null, Guid.NewGuid(), "admin@example.test",
            RegistrationFeeOverride: 200m, FreeTestDaysOverride: 60);
        await handler.Handle(command, CancellationToken.None);

        request.IsTrialGrant.Should().BeTrue();
        request.ApprovalRegistrationFee.Should().Be(200m);
        request.ApprovalFreeTestDays.Should().Be(60);
    }

    [Fact]
    public async Task Handle_ClinicCodeOverride_IsStoredOnApproval()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, false, "OVERRIDE1", null, Guid.NewGuid(), "admin@example.test");
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
        var command = new ApproveClinicOnboardingCommand(request.Id, false, null, null, Guid.NewGuid(), "admin@example.test");
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsNotFoundException()
    {
        var requestId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(requestId, false, null, null, Guid.NewGuid(), "admin@example.test");
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NoBillingIntent_DoesNotSetBillingIntent()
    {
        var request = CreateSubmittedRequest();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var command = new ApproveClinicOnboardingCommand(request.Id, false, null, null, Guid.NewGuid(), "admin@example.test");
        await handler.Handle(command, CancellationToken.None);

        request.ApprovalBillingMode.Should().BeNull();
        request.ApprovalRegistrationFee.Should().BeNull();
    }
}
