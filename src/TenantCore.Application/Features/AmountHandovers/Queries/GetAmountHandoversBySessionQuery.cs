using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Queries;

public sealed record GetAmountHandoversBySessionQuery(Guid CounterSessionId, Guid ApplicationId) : IRequest<IEnumerable<AmountHandoverSummaryDto>>;
