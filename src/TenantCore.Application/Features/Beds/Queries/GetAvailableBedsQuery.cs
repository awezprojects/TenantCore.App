using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Beds.Queries;

public sealed record GetAvailableBedsQuery(Guid ApplicationId, Guid? WardId = null) : IRequest<IEnumerable<BedDto>>;
