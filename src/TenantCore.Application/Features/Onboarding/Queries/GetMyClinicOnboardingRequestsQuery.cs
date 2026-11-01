using MediatR;
using TenantCore.Shared.Dtos.Onboarding;

namespace TenantCore.Application.Features.Onboarding.Queries;

public sealed record GetMyClinicOnboardingRequestsQuery(Guid UserId) : IRequest<IEnumerable<ClinicOnboardingRequestDto>>;
