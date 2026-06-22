using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Queries;

public sealed record GetCounterSessionByIdQuery(Guid Id, Guid ApplicationId) : IRequest<CounterSessionDto>;
