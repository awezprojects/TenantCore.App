using MediatR;
using TenantCore.Application.Features.Beds.Queries;
using TenantCore.Application.Features.Beds.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Beds.Handlers;

public sealed class GetBedsByRoomHandler(IBedRepository repository) : IRequestHandler<GetBedsByRoomQuery, IEnumerable<BedDto>>
{
    public async Task<IEnumerable<BedDto>> Handle(GetBedsByRoomQuery request, CancellationToken cancellationToken)
    {
        var beds = await repository.GetByRoomAsync(request.RoomId, request.ApplicationId, cancellationToken);
        return beds.Select(BedTranslator.ToDto);
    }
}
