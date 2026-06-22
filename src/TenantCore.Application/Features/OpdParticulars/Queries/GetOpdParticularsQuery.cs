using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdParticulars.Queries;

public sealed record GetOpdParticularsQuery(Guid OpdRegistrationId, Guid ApplicationId) : IRequest<IEnumerable<OpdParticularDto>>;
