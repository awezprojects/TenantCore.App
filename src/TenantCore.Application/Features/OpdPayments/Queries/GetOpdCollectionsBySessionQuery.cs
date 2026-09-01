using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Queries;

public sealed record GetOpdCollectionsBySessionQuery(Guid SessionId, Guid ApplicationId) : IRequest<IEnumerable<SessionCollectionDto>>;
