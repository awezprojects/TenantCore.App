using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdParticulars.Queries;

public sealed record GetIpdParticularsQuery(Guid IpdRegistrationId, Guid ApplicationId) : IRequest<IEnumerable<IpdParticularDto>>;
