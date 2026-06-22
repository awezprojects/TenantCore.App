using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Queries;

public sealed record GetPendingAmountHandoversQuery(Guid ApplicationId) : IRequest<IEnumerable<AmountHandoverSummaryDto>>;
