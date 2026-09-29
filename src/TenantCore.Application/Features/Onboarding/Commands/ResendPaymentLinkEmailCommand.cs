using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Onboarding.Commands;

public sealed record ResendPaymentLinkEmailCommand(Guid Id, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction
{
    public string ActionName => "Payment Link Resent";
}
