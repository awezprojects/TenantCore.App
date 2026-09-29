using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Onboarding.Commands;

public sealed record ChangePaymentAmountCommand(Guid Id, decimal Amount, string Reason, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction
{
    public string ActionName => "Payment Amount Changed";
}
