using MediatR;

namespace TenantCore.Application.Features.Onboarding.Commands;

public sealed record CheckOnboardingPaymentCommand(Guid Id, Guid UserId) : IRequest;
