using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Commands;

public sealed record CloseCounterSessionCommand(Guid Id, Guid ApplicationId) : IRequest<CounterSessionDto>;
