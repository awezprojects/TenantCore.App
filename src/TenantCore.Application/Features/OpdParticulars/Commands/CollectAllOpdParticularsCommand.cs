using MediatR;

namespace TenantCore.Application.Features.OpdParticulars.Commands;

public sealed record CollectAllOpdParticularsCommand(
    Guid OpdRegistrationId,
    Guid ApplicationId,
    Guid CollectedByUserId,
    Guid? CounterSessionId) : IRequest;
