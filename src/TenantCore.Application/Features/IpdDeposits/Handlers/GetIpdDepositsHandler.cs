using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdDeposits.Queries;
using TenantCore.Application.Features.IpdDeposits.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdDeposits.Handlers;

public sealed class GetIpdDepositsHandler(
    IIpdDepositRepository depositRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdDepositsQuery, IEnumerable<IpdDepositDto>>
{
    public async Task<IEnumerable<IpdDepositDto>> Handle(GetIpdDepositsQuery request, CancellationToken cancellationToken)
    {
        if (!accessValidator.CanAccess(request.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var deposits = await depositRepository.GetByRegistrationAsync(request.IpdRegistrationId, request.ApplicationId, cancellationToken);
        return IpdDepositTranslator.ToDtoList(deposits);
    }
}
