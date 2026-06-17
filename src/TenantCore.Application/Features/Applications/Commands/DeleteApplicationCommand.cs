using MediatR;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record DeleteApplicationCommand(Guid ApplicationId) : IRequest;
