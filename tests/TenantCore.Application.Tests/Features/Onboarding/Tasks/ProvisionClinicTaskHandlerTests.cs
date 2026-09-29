using FluentAssertions;
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

public class ProvisionClinicTaskHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IAuthProvisioningService> _authProvisioningService = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public ProvisionClinicTaskHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ProvisionClinicTaskHandler CreateHandler() => new(_requestRepository.Object, _authProvisioningService.Object, _workflowEnqueuer.Object);

    private static ClinicOnboardingRequest CreateProvisioningRequest(bool isTrialGrant = false, string? clinicCodeOverride = null)
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "PREFERRED", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        if (isTrialGrant)
        {
            request.ApproveWithTrial(clinicCodeOverride, null, Guid.NewGuid(), "admin@example.test");
        }
        else
        {
            request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, clinicCodeOverride, null, Guid.NewGuid(), "admin@example.test");
            request.SetCurrentPayment(Guid.NewGuid());
            request.MarkAwaitingPayment();
            request.MarkPaymentReceived();
            request.MarkProvisioning();
        }
        return request;
    }

    private static WorkflowTask CreateTask(Guid requestId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.ProvisionClinic, $"provision:{requestId}", nameof(ClinicOnboardingRequest), requestId);

    [Fact]
    public async Task HandleAsync_Success_StoresApplicationIdAndEnqueuesActivation()
    {
        var request = CreateProvisioningRequest(isTrialGrant: true);
        var applicationId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _authProvisioningService.Setup(s => s.ProvisionClinicAsync(
            request.Id, request.RequestedByUserId, request.ClinicName, request.EffectiveClinicCode, request.Address,
            request.ClinicContactNumber, request.DoctorName, request.MedicalRegistrationNumber, request.OfficialEmail, request.Website, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProvisioningOutcome.Success(applicationId));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(request.Id), CancellationToken.None);

        request.ProvisionedApplicationId.Should().Be(applicationId);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ActivateSubscription, It.IsAny<string>(), nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyProvisioned_SkipsAuthCallAndJustEnqueuesActivation()
    {
        var request = CreateProvisioningRequest(isTrialGrant: true);
        request.SetProvisionedClinic(Guid.NewGuid());
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(request.Id), CancellationToken.None);

        _authProvisioningService.Verify(s => s.ProvisionClinicAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ActivateSubscription, It.IsAny<string>(), nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CodeConflict_IsPermanent()
    {
        var request = CreateProvisioningRequest(isTrialGrant: true);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _authProvisioningService.Setup(s => s.ProvisionClinicAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProvisioningOutcome.Permanent("clinic code taken"));

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(request.Id), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
        request.ProvisionedApplicationId.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Auth5xx_IsTransient()
    {
        var request = CreateProvisioningRequest(isTrialGrant: true);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _authProvisioningService.Setup(s => s.ProvisionClinicAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProvisioningOutcome.Transient("auth unavailable"));

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(request.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_ApprovedCodeOverridesPreferredCode()
    {
        var request = CreateProvisioningRequest(isTrialGrant: true, clinicCodeOverride: "OVERRIDDEN");
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        string? usedCode = null;
        _authProvisioningService.Setup(s => s.ProvisionClinicAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, string, string, string?, string?, string?, string?, string?, string?, CancellationToken>(
                (_, _, _, code, _, _, _, _, _, _, _) => usedCode = code)
            .ReturnsAsync(ProvisioningOutcome.Success(Guid.NewGuid()));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(request.Id), CancellationToken.None);

        usedCode.Should().Be("OVERRIDDEN");
        usedCode.Should().NotBe("PREFERRED");
    }

    [Fact]
    public async Task HandleAsync_RequestNotFound_ThrowsPermanentWorkflowException()
    {
        var requestId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(requestId), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }
}
