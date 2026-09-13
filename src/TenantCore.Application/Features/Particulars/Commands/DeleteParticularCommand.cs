using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Particulars.Commands;

public sealed record DeleteParticularCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Particular Deletion";
}
