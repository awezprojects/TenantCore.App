using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Queries;

public record GetTenantsQuery(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<TenantDto>>;
