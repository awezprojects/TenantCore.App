using MediatR;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Queries;

/// <summary>IsClinicAdmin drives SubscriptionStatusDto.CanSubscribe — only a Clinic Admin may pick a plan.</summary>
public sealed record GetSubscriptionStatusQuery(Guid ApplicationId, bool IsClinicAdmin) : IRequest<SubscriptionStatusDto>;
