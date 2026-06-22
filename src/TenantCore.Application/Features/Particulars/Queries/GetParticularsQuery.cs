using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Particulars.Queries;

public sealed record GetParticularsQuery(Guid ApplicationId) : IRequest<IEnumerable<ParticularSummaryDto>>;
