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
    private readonly Mock<INotificationPublisher> _notificationPublisher = new();

    private SendEmailTaskHandler CreateHandler() => new(_notificationPublisher.Object);

    [Fact]
    public async Task HandleAsync_ValidPayload_PublishesNotificationForRecipient()
    {
        var payload = OnboardingEmailTemplates.BuildPayload("ClinicReady", "doctor@example.test", new { ClinicName = "Sunrise Clinic" });
        var task = WorkflowTask.Enqueue(WorkflowTaskType.SendEmail, "email:x", nameof(ClinicOnboardingRequest), Guid.NewGuid(), payload);

        var handler = CreateHandler();
        await handler.HandleAsync(task, CancellationToken.None);

        _notificationPublisher.Verify(p => p.PublishEmailNotificationAsync(
            It.Is<EmailNotificationDto>(n =>
                n.RecipientEmail == "doctor@example.test"
                && n.RecipientName == "Sunrise Clinic"
                && n.TemplateId == "ClinicReady"
                && n.Subject == "Your clinic is ready"
                && n.TemplateData["ClinicName"] == "Sunrise Clinic"),
            It.IsAny<CancellationToken>()), Times.Once);
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
