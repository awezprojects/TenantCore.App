using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdParticulars.Queries;
using TenantCore.Application.Features.IpdParticulars.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdParticulars.Handlers;

public sealed class GetIpdParticularsHandler(
    IIpdParticularRepository ipdParticularRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdParticularsQuery, IEnumerable<IpdParticularDto>>
{
    public async Task<IEnumerable<IpdParticularDto>> Handle(GetIpdParticularsQuery request, CancellationToken cancellationToken)
    {
        if (!accessValidator.CanAccess(request.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var particulars = await ipdParticularRepository.GetByRegistrationAsync(request.IpdRegistrationId, request.ApplicationId, cancellationToken);
        return IpdParticularTranslator.ToDtoList(particulars);
    }
}
