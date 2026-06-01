using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Wards.Queries;

public sealed record GetWardsQuery(Guid ApplicationId) : IRequest<IEnumerable<WardDto>>;
