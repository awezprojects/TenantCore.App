using MediatR;
using TenantCore.Shared.Dtos.Onboarding;

namespace TenantCore.Application.Features.Onboarding.Queries;

public sealed record GetMyClinicOnboardingRequestByIdQuery(Guid Id, Guid UserId) : IRequest<ClinicOnboardingRequestDto>;
