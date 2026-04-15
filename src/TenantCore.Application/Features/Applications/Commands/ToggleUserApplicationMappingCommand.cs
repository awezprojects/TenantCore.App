using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public record ToggleUserApplicationMappingCommand(Guid ApplicationId, Guid UserId, Guid ModifiedBy, bool IsActive) : IRequest;
