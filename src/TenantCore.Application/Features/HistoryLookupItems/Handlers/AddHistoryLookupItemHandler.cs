using MediatR;
using TenantCore.Application.Features.HistoryLookupItems.Commands;
using TenantCore.Application.Features.HistoryLookupItems.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.HistoryLookupItems.Handlers;

public sealed class AddHistoryLookupItemHandler(IHistoryLookupItemRepository repository)
    : IRequestHandler<AddHistoryLookupItemCommand, HistoryLookupItemDto>
{
    public async Task<HistoryLookupItemDto> Handle(
        AddHistoryLookupItemCommand request, CancellationToken cancellationToken)
    {
        var value = request.Value.Trim();

        // Already exists (globally, or already added by this clinic) — reuse it instead
        // of creating a duplicate row.
        var existing = await repository.FindAsync(request.Type, request.ApplicationId, value, cancellationToken);
        if (existing is not null)
            return HistoryLookupItemTranslator.ToDto(existing);

        var item = HistoryLookupItem.CreateForClinic(request.ApplicationId, request.Type, value);
        await repository.AddAsync(item, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return HistoryLookupItemTranslator.ToDto(item);
    }
}
