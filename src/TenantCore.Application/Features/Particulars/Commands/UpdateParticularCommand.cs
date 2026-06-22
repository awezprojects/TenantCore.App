using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Particulars.Commands;

public sealed record UpdateParticularCommand(Guid Id, UpdateParticularRequest Request, Guid ApplicationId) : IRequest<ParticularDto>;
