using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;

namespace TenantCore.Application.Features.Onboarding.Tasks;

public sealed class SendEmailTaskHandler(IEmailService emailService) : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.SendEmail;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(task.PayloadJson))
            throw new PermanentWorkflowException("SendEmail task has no payload.");

        var (to, subject, htmlBody) = OnboardingEmailTemplates.Render(task.PayloadJson);
        if (string.IsNullOrWhiteSpace(to))
            throw new PermanentWorkflowException("SendEmail task payload has no recipient.");

        await emailService.SendAsync(to, subject, htmlBody, ct: ct);
    }
}
