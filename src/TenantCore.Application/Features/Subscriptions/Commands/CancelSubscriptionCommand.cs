using MediatR;

namespace TenantCore.Application.Features.Subscriptions.Commands;

public sealed record CancelSubscriptionCommand(
    Guid ApplicationId,
    Guid SubscriptionId,
    Guid ActingUserId) : IRequest;
