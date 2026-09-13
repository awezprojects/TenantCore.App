using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.OpdRegistrations.Commands;

public sealed record DeleteOpdRegistrationCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "OPD Registration Deactivation";
}
