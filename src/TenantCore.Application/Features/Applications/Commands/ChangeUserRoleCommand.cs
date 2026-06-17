using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record ChangeUserRoleCommand(Guid ApplicationId, Guid UserId, Guid ModifiedBy, Guid NewRoleId) : IRequest;
