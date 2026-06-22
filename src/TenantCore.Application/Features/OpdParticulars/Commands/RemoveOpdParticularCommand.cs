using MediatR;

namespace TenantCore.Application.Features.OpdParticulars.Commands;

public sealed record RemoveOpdParticularCommand(Guid Id, Guid ApplicationId) : IRequest;
