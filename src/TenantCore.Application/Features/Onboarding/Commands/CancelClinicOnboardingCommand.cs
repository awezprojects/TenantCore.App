using MediatR;

namespace TenantCore.Application.Features.Onboarding.Commands;

public sealed record CancelClinicOnboardingCommand(Guid Id, Guid UserId) : IRequest;
