using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record DeleteApplicationCommand(Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Application (Clinic) Deletion";
}
