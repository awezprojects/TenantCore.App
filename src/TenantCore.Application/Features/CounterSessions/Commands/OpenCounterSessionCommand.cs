using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Commands;

public sealed record OpenCounterSessionCommand(OpenCounterSessionRequest Request, Guid OpenedByUserId, Guid ApplicationId) : IRequest<Guid>;
