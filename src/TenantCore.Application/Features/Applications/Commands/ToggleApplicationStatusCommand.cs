using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public record ToggleApplicationStatusCommand(Guid ApplicationId, Guid ModifiedBy, bool IsActive) : IRequest;
