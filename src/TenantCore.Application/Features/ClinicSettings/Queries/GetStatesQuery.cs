using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Queries;

public sealed record GetStatesQuery : IRequest<IEnumerable<StateDto>>;
