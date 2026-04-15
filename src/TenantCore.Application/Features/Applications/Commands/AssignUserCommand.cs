using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public record AssignUserCommand(
    Guid ApplicationId,
    Guid UserId,
    Guid RoleId,
    Guid AssignedBy) : IRequest;
