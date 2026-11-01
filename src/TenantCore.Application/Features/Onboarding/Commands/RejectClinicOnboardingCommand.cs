using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Onboarding.Commands;

public sealed record RejectClinicOnboardingCommand(Guid Id, string Reason, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction
{
    public string ActionName => "Clinic Onboarding Request Rejected";
}
