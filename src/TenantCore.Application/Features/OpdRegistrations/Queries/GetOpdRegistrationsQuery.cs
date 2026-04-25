using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Queries;

public sealed record GetOpdRegistrationsQuery(
    Guid ApplicationId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<PagedResult<OpdRegistrationDto>>;
