using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Beds.Commands;

public sealed record DeleteBedCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Bed Deletion";
}
