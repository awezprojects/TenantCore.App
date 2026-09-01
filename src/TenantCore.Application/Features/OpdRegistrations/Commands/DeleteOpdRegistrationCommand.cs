using MediatR;

namespace TenantCore.Application.Features.OpdRegistrations.Commands;

public sealed record DeleteOpdRegistrationCommand(Guid Id, Guid ApplicationId) : IRequest;
