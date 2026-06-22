using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Queries;

public sealed record GetOpdPaymentByOpdIdQuery(Guid OpdRegistrationId, Guid ApplicationId) : IRequest<OpdPaymentDto?>;
