using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public record ChangeUserRoleCommand(Guid ApplicationId, Guid UserId, Guid ModifiedBy, Guid NewRoleId) : IRequest;
