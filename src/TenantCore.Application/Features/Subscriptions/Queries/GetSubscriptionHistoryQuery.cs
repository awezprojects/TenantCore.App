using MediatR;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Queries;

public sealed record GetSubscriptionHistoryQuery(Guid ApplicationId) : IRequest<IEnumerable<SubscriptionHistoryItemDto>>;
