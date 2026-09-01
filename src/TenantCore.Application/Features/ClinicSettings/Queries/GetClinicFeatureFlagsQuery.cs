using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Queries;

public sealed record GetClinicFeatureFlagsQuery(Guid ApplicationId) : IRequest<ClinicFeatureFlagsDto?>;
