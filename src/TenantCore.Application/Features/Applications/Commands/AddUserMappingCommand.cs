using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record AddUserMappingCommand(
    Guid ApplicationId,
    Guid UserId,
    Guid AssignedBy) : IRequest;
