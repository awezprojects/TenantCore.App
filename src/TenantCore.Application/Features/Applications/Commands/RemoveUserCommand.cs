using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public record RemoveUserCommand(
    Guid ApplicationId,
    Guid UserId,
    Guid RemovedBy) : IRequest;
