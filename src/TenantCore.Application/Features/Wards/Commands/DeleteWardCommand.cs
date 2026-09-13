using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Wards.Commands;

public sealed record DeleteWardCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Ward Deletion";
}
