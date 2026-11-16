using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Queries;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class GetIpdBedTransferHistoryHandler(
    IIpdBedTransferHistoryRepository historyRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdBedTransferHistoryQuery, IEnumerable<IpdBedTransferHistoryDto>>
{
    public async Task<IEnumerable<IpdBedTransferHistoryDto>> Handle(
        GetIpdBedTransferHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!accessValidator.CanAccess(request.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var history = await historyRepository.GetByRegistrationAsync(request.Id, request.ApplicationId, cancellationToken);
        return IpdBedTransferHistoryTranslator.ToDtoList(history);
    }
}
