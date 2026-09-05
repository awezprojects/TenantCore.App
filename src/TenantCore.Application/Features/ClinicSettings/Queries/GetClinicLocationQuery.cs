using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Queries;

public sealed record GetClinicLocationQuery(Guid ApplicationId) : IRequest<ClinicLocationDto?>;
