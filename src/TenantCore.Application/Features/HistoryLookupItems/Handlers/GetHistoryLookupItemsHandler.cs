using MediatR;
using TenantCore.Application.Features.HistoryLookupItems.Queries;
using TenantCore.Application.Features.HistoryLookupItems.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.HistoryLookupItems.Handlers;

public sealed class GetHistoryLookupItemsHandler(IHistoryLookupItemRepository repository)
    : IRequestHandler<GetHistoryLookupItemsQuery, IEnumerable<HistoryLookupItemDto>>
{
    public async Task<IEnumerable<HistoryLookupItemDto>> Handle(
        GetHistoryLookupItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await repository.GetForApplicationAsync(request.ApplicationId, cancellationToken);
        return HistoryLookupItemTranslator.ToDtoList(items);
    }
}
