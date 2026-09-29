using MediatR;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Queries;

public sealed record GetSubscriptionPaymentsQuery(Guid ApplicationId) : IRequest<IEnumerable<SubscriptionPaymentDto>>;
