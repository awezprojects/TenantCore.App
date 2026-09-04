using MediatR;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Queries;

/// <summary>ApplicationId is carried so the handler can mark the Trial plan as already-used for this clinic.</summary>
public sealed record GetSubscriptionPlansQuery(Guid ApplicationId) : IRequest<IEnumerable<SubscriptionPlanDto>>;
