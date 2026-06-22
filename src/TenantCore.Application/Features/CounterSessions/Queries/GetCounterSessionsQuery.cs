using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Queries;

public sealed record GetCounterSessionsQuery(Guid ApplicationId, DateTime? From, DateTime? To) : IRequest<IEnumerable<CounterSessionSummaryDto>>;
