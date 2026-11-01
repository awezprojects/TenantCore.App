using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Onboarding;

namespace TenantCore.Application.Tests.Features.Onboarding.Commands;

public class SubmitClinicOnboardingHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();
    private readonly Mock<IConfiguration> _configuration = new();

    public SubmitClinicOnboardingHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private SubmitClinicOnboardingHandler CreateHandler() => new(_requestRepository.Object, _workflowEnqueuer.Object, _configuration.Object);

    private static SubmitClinicOnboardingRequest CreateRequest() => new()
    {
        ClinicName = "Sunrise Clinic",
        PreferredClinicCode = "SUNRISE",
        Address = "123 Main St",
        City = "Pune",
        State = "MH",
        Pincode = "411001",
        RequesterPhone = "9876543210",
        DoctorName = "Dr. Jane",
        MedicalRegistrationNumber = "MR12345",
        MedicalCouncil = "MCI",
        ExpectedStaffCount = 5
    };

    [Fact]
    public async Task Handle_NoOpenRequest_SavesRequestAndEnqueuesUserAndOpsEmailsInOneSave()
    {
        var userId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetOpenForUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);
        _configuration.Setup(c => c["Onboarding:OpsNotificationEmail"]).Returns("ops@example.test");

        var handler = CreateHandler();
        var command = new SubmitClinicOnboardingCommand(userId, "Requester Name", "requester@example.test", CreateRequest());

        var id = await handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
        _requestRepository.Verify(r => r.AddAsync(It.IsAny<ClinicOnboardingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, It.Is<string>(k => k.Contains("request-received")),
            nameof(ClinicOnboardingRequest), id, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, It.Is<string>(k => k.Contains("ops-new-request")),
            nameof(ClinicOnboardingRequest), id, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _requestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoOpsEmailConfigured_EnqueuesOnlyTheUserEmail()
    {
        var userId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetOpenForUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);
        _configuration.Setup(c => c["Onboarding:OpsNotificationEmail"]).Returns((string?)null);

        var handler = CreateHandler();
        var command = new SubmitClinicOnboardingCommand(userId, "Requester Name", "requester@example.test", CreateRequest());

        await handler.Handle(command, CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OpenRequestAlreadyExists_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var existing = ClinicOnboardingRequest.Submit(
            userId, "Old", "old@example.test", "9999999999", "Clinic", "CODE1", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        _requestRepository.Setup(r => r.GetOpenForUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = CreateHandler();
        var command = new SubmitClinicOnboardingCommand(userId, "Requester Name", "requester@example.test", CreateRequest());
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _requestRepository.Verify(r => r.AddAsync(It.IsAny<ClinicOnboardingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_RequesterSnapshotComesFromCommandClaimsNotBody()
    {
        var userId = Guid.NewGuid();
        ClinicOnboardingRequest? saved = null;
        _requestRepository.Setup(r => r.GetOpenForUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);
        _requestRepository.Setup(r => r.AddAsync(It.IsAny<ClinicOnboardingRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicOnboardingRequest, CancellationToken>((e, _) => saved = e)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new SubmitClinicOnboardingCommand(userId, "Claims Name", "claims@example.test", CreateRequest());

        await handler.Handle(command, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.RequesterName.Should().Be("Claims Name");
        saved.RequesterEmail.Should().Be("claims@example.test");
        saved.RequestedByUserId.Should().Be(userId);
        saved.ClinicName.Should().Be("Sunrise Clinic");
    }
}
