using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Queries;

public sealed record GetIpdRegistrationByIdQuery(Guid Id, Guid ApplicationId) : IRequest<IpdRegistrationDto>;
