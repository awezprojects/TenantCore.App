using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Queries;

public sealed record GetIpdFinalBillQuery(Guid IpdRegistrationId, Guid ApplicationId) : IRequest<IpdFinalBillDto>;
