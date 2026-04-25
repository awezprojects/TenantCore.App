using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Queries;

public sealed record GetIpdRegistrationsQuery(
    Guid ApplicationId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<PagedResult<IpdRegistrationDto>>;
