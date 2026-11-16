using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdDeposits.Queries;

public sealed record GetIpdDepositsQuery(Guid IpdRegistrationId, Guid ApplicationId) : IRequest<IEnumerable<IpdDepositDto>>;
