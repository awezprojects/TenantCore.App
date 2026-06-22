using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Particulars.Queries;

public sealed record GetParticularByIdQuery(Guid Id, Guid ApplicationId) : IRequest<ParticularDto>;
