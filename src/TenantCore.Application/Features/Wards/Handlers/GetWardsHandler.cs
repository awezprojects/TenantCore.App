using MediatR;
using TenantCore.Application.Features.Wards.Queries;
using TenantCore.Application.Features.Wards.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Wards.Handlers;

public sealed class GetWardsHandler(IWardRepository repository) : IRequestHandler<GetWardsQuery, IEnumerable<WardDto>>
{
    public async Task<IEnumerable<WardDto>> Handle(GetWardsQuery request, CancellationToken cancellationToken)
    {
        var wards = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
        return wards.Select(WardTranslator.ToDto);
    }
}
