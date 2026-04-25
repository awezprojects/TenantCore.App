using MediatR;
using TenantCore.Application.Features.IpdRegistrations.Queries;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class GetIpdRegistrationsHandler(IIpdRegistrationRepository repository)
    : IRequestHandler<GetIpdRegistrationsQuery, PagedResult<IpdRegistrationDto>>
{
    public async Task<PagedResult<IpdRegistrationDto>> Handle(
        GetIpdRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var (items, total) = await repository.GetPagedAsync(
            request.ApplicationId, request.Page, pageSize, request.Search, cancellationToken);

        return new PagedResult<IpdRegistrationDto>
        {
            Items = items.Select(IpdRegistrationTranslator.ToDto).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = pageSize
        };
    }
}
