using MediatR;
using TenantCore.Application.Features.OpdParticulars.Queries;
using TenantCore.Application.Features.OpdParticulars.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdParticulars.Handlers;

public sealed class GetOpdParticularsHandler(IOpdParticularRepository repository) : IRequestHandler<GetOpdParticularsQuery, IEnumerable<OpdParticularDto>>
{
    public async Task<IEnumerable<OpdParticularDto>> Handle(GetOpdParticularsQuery request, CancellationToken cancellationToken)
    {
        var particulars = await repository.GetByOpdRegistrationIdAsync(request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        return particulars.Select(OpdParticularTranslator.ToDto);
    }
}
