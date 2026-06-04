using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Clinics.Queries;

public sealed record GetClinicDashboardQuery : IRequest<List<ClinicDashboardItemDto>>;
