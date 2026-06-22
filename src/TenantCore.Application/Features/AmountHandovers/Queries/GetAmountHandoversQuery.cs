using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Queries;

public sealed record GetAmountHandoversQuery(
    Guid ApplicationId,
    DateTime? From = null,
    DateTime? To = null,
    int UtcOffsetMinutes = 0) : IRequest<IEnumerable<AmountHandoverSummaryDto>>;
