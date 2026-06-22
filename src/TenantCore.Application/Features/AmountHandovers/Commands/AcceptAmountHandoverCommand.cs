using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Commands;

public sealed record AcceptAmountHandoverCommand(Guid Id, Guid ApplicationId) : IRequest<AmountHandoverDto>;
