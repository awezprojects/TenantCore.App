using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdParticulars.Commands;

public sealed record UpdateOpdParticularCommand(Guid Id, UpdateOpdParticularRequest Request, Guid ApplicationId) : IRequest<OpdParticularDto>;
