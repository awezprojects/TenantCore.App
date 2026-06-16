using MediatR;
using TenantCore.Application.Features.MedicineDosageForms.Queries;
using TenantCore.Application.Features.MedicineDosageForms.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Handlers;

public sealed class GetMedicineDosageFormsHandler(IMedicineDosageFormRepository repository)
    : IRequestHandler<GetMedicineDosageFormsQuery, PagedResult<MedicineDosageFormDto>>
{
    public async Task<PagedResult<MedicineDosageFormDto>> Handle(GetMedicineDosageFormsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var (items, total) = await repository.GetPagedAsync(
            request.Page, pageSize, request.Search, cancellationToken);

        return new PagedResult<MedicineDosageFormDto>
        {
            Items = MedicineDosageFormTranslator.ToDtoList(items).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = pageSize
        };
    }
}
