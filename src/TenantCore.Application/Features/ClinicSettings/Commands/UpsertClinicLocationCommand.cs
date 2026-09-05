using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Commands;

public sealed record UpsertClinicLocationCommand(Guid ApplicationId, Guid StateId, Guid CityId) : IRequest<ClinicLocationDto>;
