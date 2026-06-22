using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Commands;

public sealed record CollectAmountHandoverCommand(
    CollectAmountHandoverRequest Request,
    Guid CollectedByUserId,
    string CollectedByRoleName,
    Guid ApplicationId) : IRequest<Guid>;
