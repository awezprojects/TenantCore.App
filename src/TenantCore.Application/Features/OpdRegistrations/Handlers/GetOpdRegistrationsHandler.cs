using MediatR;
using TenantCore.Application.Features.OpdRegistrations.Queries;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class GetOpdRegistrationsHandler(IOpdRegistrationRepository repository)
    : IRequestHandler<GetOpdRegistrationsQuery, PagedResult<OpdRegistrationDto>>
{
    public async Task<PagedResult<OpdRegistrationDto>> Handle(
        GetOpdRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var (items, total) = await repository.GetPagedAsync(
            request.ApplicationId, request.Page, pageSize, request.Search, cancellationToken);

        return new PagedResult<OpdRegistrationDto>
        {
            Items = items.Select(OpdRegistrationTranslator.ToDto).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = pageSize
        };
    }
}
