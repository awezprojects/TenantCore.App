using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdParticulars.Commands;

public sealed record AddIpdParticularCommand(
    Guid IpdRegistrationId,
    Guid ApplicationId,
    Guid ParticularId,
    decimal Amount,
    Guid AddedByUserId) : IRequest<IpdParticularDto>, IActionLogContext
{
    public string? ActionLogContext => $"ipdRegistrationId={IpdRegistrationId}; particularId={ParticularId}";
}
