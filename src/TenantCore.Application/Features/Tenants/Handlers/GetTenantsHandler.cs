using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Tenants.Queries;
using TenantCore.Application.Features.Tenants.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Handlers;

public sealed class GetTenantsHandler(
    ITenantRepository repository,
    ILogger<GetTenantsHandler> logger)
    : IRequestHandler<GetTenantsQuery, PagedResult<TenantDto>>
{
    public async Task<PagedResult<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting tenants page {Page} pageSize {PageSize}", request.Page, request.PageSize);
        var (items, total) = await repository.GetPagedAsync(request.Page, request.PageSize, request.Search, cancellationToken);
        return new PagedResult<TenantDto>
        {
            Items = items.Select(TenantTranslator.ToDto).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
