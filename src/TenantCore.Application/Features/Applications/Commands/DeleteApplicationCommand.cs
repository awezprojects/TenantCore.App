using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public record DeleteApplicationCommand(Guid ApplicationId) : IRequest;
