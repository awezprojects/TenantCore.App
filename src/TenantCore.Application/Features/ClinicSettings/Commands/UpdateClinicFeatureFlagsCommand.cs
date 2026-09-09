using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Commands;

public sealed record UpdateClinicFeatureFlagsCommand(
    Guid ApplicationId,
    bool PrepaidOpdEnabled,
    bool BillingEnabled) : IRequest<ClinicFeatureFlagsDto>;
