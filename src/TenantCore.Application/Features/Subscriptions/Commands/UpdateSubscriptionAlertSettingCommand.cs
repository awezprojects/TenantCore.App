using MediatR;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Commands;

public sealed record UpdateSubscriptionAlertSettingCommand(
    Guid Id,
    UpdateSubscriptionAlertSettingRequest Request) : IRequest<SubscriptionAlertSettingDto>;
