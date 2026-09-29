using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Application.Features.Onboarding.Tasks;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;

namespace TenantCore.Application.Tests.Features.Onboarding.Tasks;

public class SendEmailTaskHandlerTests
{
    private readonly Mock<IEmailService> _emailService = new();

    private SendEmailTaskHandler CreateHandler() => new(_emailService.Object);

    [Fact]
    public async Task HandleAsync_ValidPayload_SendsRenderedTemplateToRecipient()
    {
        var payload = OnboardingEmailTemplates.BuildPayload("ClinicReady", "doctor@example.test", new { ClinicName = "Sunrise Clinic" });
        var task = WorkflowTask.Enqueue(WorkflowTaskType.SendEmail, "email:x", nameof(ClinicOnboardingRequest), Guid.NewGuid(), payload);

        var handler = CreateHandler();
        await handler.HandleAsync(task, CancellationToken.None);

        _emailService.Verify(e => e.SendAsync(
            "doctor@example.test", "Your clinic is ready", It.Is<string>(b => b.Contains("Sunrise Clinic")),
            null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NoPayload_ThrowsPermanentWorkflowException()
    {
        var task = WorkflowTask.Enqueue(WorkflowTaskType.SendEmail, "email:x", nameof(ClinicOnboardingRequest), Guid.NewGuid());

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(task, CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_PayloadWithNoRecipient_ThrowsPermanentWorkflowException()
    {
        var payload = OnboardingEmailTemplates.BuildPayload("ClinicReady", string.Empty, new { ClinicName = "Sunrise Clinic" });
        var task = WorkflowTask.Enqueue(WorkflowTaskType.SendEmail, "email:x", nameof(ClinicOnboardingRequest), Guid.NewGuid(), payload);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(task, CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }
}
