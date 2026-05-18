using MediatR;
using TenantCore.Application.Features.DosageRemarks.Queries;
using TenantCore.Application.Features.DosageRemarks.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DosageRemarks.Handlers;

public sealed class GetDosageRemarksHandler(IDosageRemarkRepository repository)
    : IRequestHandler<GetDosageRemarksQuery, PagedResult<DosageRemarkDto>>
{
    public async Task<PagedResult<DosageRemarkDto>> Handle(
        GetDosageRemarksQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var (items, total) = await repository.GetPagedAsync(
            request.ApplicationId, request.Page, pageSize, request.Form, cancellationToken);

        return new PagedResult<DosageRemarkDto>
        {
            Items = items.Select(DosageRemarkTranslator.ToDto).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = pageSize
        };
    }
}
