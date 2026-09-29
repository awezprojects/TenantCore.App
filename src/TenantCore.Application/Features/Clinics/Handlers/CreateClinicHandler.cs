using MediatR;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Clinics.Commands;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Clinics.Handlers;

public sealed class CreateClinicHandler(IAuthClinicService clinicService, IOptions<OnboardingOptions> onboardingOptions)
    : IRequestHandler<CreateClinicCommand, ApplicationResponseDto>
{
    public async Task<ApplicationResponseDto> Handle(CreateClinicCommand request, CancellationToken cancellationToken)
    {
        // Self-service creation is off by default ("Approval" mode) — clinics are created only
        // through the onboarding request workflow, after admin approval and a confirmed payment.
        // TenantCore.Auth enforces the same rule independently (ClinicSettings.SelfServiceCreationEnabled).
        if (string.Equals(onboardingOptions.Value.Mode, "Approval", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Clinic creation is by request only. Submit a clinic request from your dashboard instead.");

        return await clinicService.CreateClinicAsync(request.Request, cancellationToken);
    }
}
