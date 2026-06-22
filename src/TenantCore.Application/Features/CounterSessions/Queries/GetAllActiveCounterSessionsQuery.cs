using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Queries;

public sealed record GetAllActiveCounterSessionsQuery(Guid ApplicationId) : IRequest<IEnumerable<CounterSessionSummaryDto>>;
