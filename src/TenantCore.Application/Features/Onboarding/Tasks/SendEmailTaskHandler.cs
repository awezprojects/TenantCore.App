using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;

namespace TenantCore.Application.Features.Onboarding.Tasks;

public sealed class SendEmailTaskHandler(INotificationPublisher notificationPublisher) : IWorkflowTaskHandler
{
    // Best-effort "who is this for" — templates don't all carry the same model shape, so this
    // tries the fields that hold a person's name before falling back to the clinic name.
    private static readonly string[] NameFields = ["RequesterName", "DoctorName", "ClinicName"];

    public WorkflowTaskType TaskType => WorkflowTaskType.SendEmail;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(task.PayloadJson))
            throw new PermanentWorkflowException("SendEmail task has no payload.");

        var (to, template, subject, templateData) = OnboardingEmailTemplates.RenderForQueue(task.PayloadJson);
        if (string.IsNullOrWhiteSpace(to))
            throw new PermanentWorkflowException("SendEmail task payload has no recipient.");

        var recipientName = NameFields
            .Select(field => templateData.GetValueOrDefault(field))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        await notificationPublisher.PublishEmailNotificationAsync(new EmailNotificationDto
        {
            RecipientEmail = to,
            RecipientName = recipientName,
            TemplateId = template,
            Subject = subject,
            TemplateData = templateData
        }, ct);
    }
}
