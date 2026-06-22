using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Commands;

public sealed record CreateAmountHandoverCommand(CreateAmountHandoverRequest Request, Guid HandedOverByUserId, Guid ApplicationId) : IRequest<Guid>;
