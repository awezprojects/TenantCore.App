using MediatR;
using Microsoft.Extensions.Configuration;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class SubmitClinicOnboardingHandler(
    IClinicOnboardingRequestRepository requestRepository,
    IWorkflowEnqueuer workflowEnqueuer,
    IConfiguration configuration)
    : IRequestHandler<SubmitClinicOnboardingCommand, Guid>
{
    public async Task<Guid> Handle(SubmitClinicOnboardingCommand command, CancellationToken ct)
    {
        var existingOpen = await requestRepository.GetOpenForUserAsync(command.UserId, ct);
        if (existingOpen != null)
            throw new InvalidOperationException("You already have an open clinic request. Cancel it or wait for a decision before submitting another.");

        var r = command.Request;
        var entity = ClinicOnboardingRequest.Submit(
            command.UserId, command.RequesterName, command.RequesterEmail, r.RequesterPhone,
            r.ClinicName, r.PreferredClinicCode, r.Address, r.City, r.State, r.Pincode,
            r.ClinicContactNumber, r.OfficialEmail, r.Website,
            r.DoctorName, r.MedicalRegistrationNumber, r.MedicalCouncil, r.ExpectedStaffCount,
            r.ReferralSource, r.Notes);

        await requestRepository.AddAsync(entity, ct);

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.SendEmail, $"email:request-received:{entity.Id}", nameof(ClinicOnboardingRequest), entity.Id,
            OnboardingEmailTemplates.BuildPayload("RequestReceived", entity.RequesterEmail, new { entity.ClinicName, RequesterName = entity.RequesterName }),
            ct);

        var opsEmail = configuration["Onboarding:OpsNotificationEmail"];
        if (!string.IsNullOrWhiteSpace(opsEmail))
        {
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.SendEmail, $"email:ops-new-request:{entity.Id}", nameof(ClinicOnboardingRequest), entity.Id,
                OnboardingEmailTemplates.BuildPayload("OpsNewRequest", opsEmail, new { entity.ClinicName, entity.DoctorName, entity.RequesterEmail }),
                ct);
        }

        await requestRepository.SaveChangesAsync(ct);
        return entity.Id;
    }
}
