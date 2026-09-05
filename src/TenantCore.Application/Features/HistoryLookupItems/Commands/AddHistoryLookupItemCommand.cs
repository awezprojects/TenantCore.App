using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.HistoryLookupItems.Commands;

public sealed record AddHistoryLookupItemCommand(
    Guid ApplicationId,
    HistoryItemType Type,
    string Value) : IRequest<HistoryLookupItemDto>;
