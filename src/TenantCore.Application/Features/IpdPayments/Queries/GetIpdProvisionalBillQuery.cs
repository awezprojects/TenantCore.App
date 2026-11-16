using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Queries;

public sealed record GetIpdProvisionalBillQuery(Guid IpdRegistrationId, Guid ApplicationId) : IRequest<IpdProvisionalBillDto>;
