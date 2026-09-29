using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Onboarding.Commands;

public sealed record RetryClinicOnboardingCommand(Guid Id, string? ClinicCode, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction
{
    public string ActionName => "Clinic Onboarding Request Retried";
}
