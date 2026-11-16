using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.IpdParticulars.Commands;

public sealed record RemoveIpdParticularCommand(Guid Id, Guid ApplicationId) : IRequest, IActionLogContext
{
    public string? ActionLogContext => $"ipdParticularId={Id}";
}
