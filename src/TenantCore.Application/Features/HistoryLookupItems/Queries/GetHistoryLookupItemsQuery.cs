using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.HistoryLookupItems.Queries;

public sealed record GetHistoryLookupItemsQuery(Guid ApplicationId) : IRequest<IEnumerable<HistoryLookupItemDto>>;
