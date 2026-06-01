using MediatR;

namespace TenantCore.Application.Features.Beds.Commands;

public sealed record DeleteBedCommand(Guid Id, Guid ApplicationId) : IRequest;
