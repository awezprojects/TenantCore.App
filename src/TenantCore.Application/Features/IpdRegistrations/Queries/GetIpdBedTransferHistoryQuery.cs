using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Queries;

public sealed record GetIpdBedTransferHistoryQuery(Guid Id, Guid ApplicationId) : IRequest<IEnumerable<IpdBedTransferHistoryDto>>;
