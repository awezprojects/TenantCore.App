using MediatR;

namespace TenantCore.Application.Features.OpdParticulars.Commands;

public sealed record CollectOpdParticularCommand(
    Guid ParticularId,
    Guid ApplicationId,
    Guid CollectedByUserId,
    Guid? CounterSessionId) : IRequest;
