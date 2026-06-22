using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Queries;

public sealed record GetActiveCounterSessionQuery(Guid ApplicationId) : IRequest<CounterSessionDto?>;
