using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Queries;

public sealed record GetOpdRegistrationByIdQuery(Guid Id, Guid ApplicationId) : IRequest<OpdRegistrationDto>;
