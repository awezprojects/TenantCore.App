using MediatR;
using TenantCore.Application.Features.Beds.Queries;
using TenantCore.Application.Features.Beds.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Beds.Handlers;

public sealed class GetAvailableBedsHandler(IBedRepository repository) : IRequestHandler<GetAvailableBedsQuery, IEnumerable<BedDto>>
{
    public async Task<IEnumerable<BedDto>> Handle(GetAvailableBedsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Domain.Entities.Bed> beds;
        if (request.WardId.HasValue)
            beds = await repository.GetAvailableByWardAsync(request.WardId.Value, request.ApplicationId, cancellationToken);
        else
            beds = (await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken))
                   .Where(b => !b.IsOccupied && b.IsActive);

        return beds.Select(BedTranslator.ToDto);
    }
}
