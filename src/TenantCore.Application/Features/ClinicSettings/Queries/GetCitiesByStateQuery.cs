using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Queries;

public sealed record GetCitiesByStateQuery(Guid StateId) : IRequest<IEnumerable<CityDto>>;
