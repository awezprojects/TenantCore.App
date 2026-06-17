using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record ToggleUserApplicationMappingCommand(Guid ApplicationId, Guid UserId, Guid ModifiedBy, bool IsActive) : IRequest;
