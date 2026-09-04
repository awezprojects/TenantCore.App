using MediatR;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Commands;

public sealed record SubscribeToPlanCommand(
    Guid ApplicationId,
    Guid SubscriptionPlanId,
    Guid ActingUserId) : IRequest<ClinicSubscriptionDto>;
