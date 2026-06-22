using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Queries;

public sealed record GetAmountHandoverByIdQuery(Guid Id, Guid ApplicationId) : IRequest<AmountHandoverDto>;
