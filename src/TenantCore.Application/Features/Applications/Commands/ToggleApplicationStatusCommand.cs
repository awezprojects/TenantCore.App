using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record ToggleApplicationStatusCommand(Guid ApplicationId, Guid ModifiedBy, bool IsActive) : IRequest;
