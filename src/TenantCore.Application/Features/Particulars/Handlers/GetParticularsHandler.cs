using MediatR;
using TenantCore.Application.Features.Particulars.Queries;
using TenantCore.Application.Features.Particulars.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Particulars.Handlers;

public sealed class GetParticularsHandler(IParticularRepository repository) : IRequestHandler<GetParticularsQuery, IEnumerable<ParticularSummaryDto>>
{
    public async Task<IEnumerable<ParticularSummaryDto>> Handle(GetParticularsQuery request, CancellationToken cancellationToken)
    {
        var particulars = await repository.GetAllAsync(cancellationToken);
        return particulars
            .Where(p => p.ApplicationId == request.ApplicationId)
            .Select(p => ParticularTranslator.ToSummaryDto(p));
    }
}
