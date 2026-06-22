using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Commands;

public sealed record DisputeAmountHandoverCommand(Guid Id, ResolveAmountHandoverRequest Request, Guid ApplicationId) : IRequest<AmountHandoverDto>;
