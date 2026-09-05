using MediatR;
using TenantCore.Application.Features.ClinicSettings.Queries;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class GetStatesHandler(IStateRepository repository)
    : IRequestHandler<GetStatesQuery, IEnumerable<StateDto>>
{
    public async Task<IEnumerable<StateDto>> Handle(GetStatesQuery request, CancellationToken cancellationToken)
    {
        var states = await repository.GetAllOrderedAsync(cancellationToken);
        return states.Select(LocationTranslator.ToDto);
    }
}
