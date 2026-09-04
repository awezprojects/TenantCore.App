using MediatR;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Queries;

/// <summary>Not tenant-scoped — reads the global reminder-threshold configuration for the admin portal.</summary>
public sealed record GetSubscriptionAlertSettingsQuery : IRequest<IEnumerable<SubscriptionAlertSettingDto>>;
