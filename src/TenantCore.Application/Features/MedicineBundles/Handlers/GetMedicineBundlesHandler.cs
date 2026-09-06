using MediatR;
using TenantCore.Application.Features.MedicineBundles.Queries;
using TenantCore.Application.Features.MedicineBundles.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Handlers;

public sealed class GetMedicineBundlesHandler(IMedicineBundleRepository repository)
    : IRequestHandler<GetMedicineBundlesQuery, IEnumerable<MedicineBundleDto>>
{
    public async Task<IEnumerable<MedicineBundleDto>> Handle(GetMedicineBundlesQuery request, CancellationToken cancellationToken)
    {
        var bundles = await repository.GetAllForApplicationAsync(request.ApplicationId, cancellationToken);
        return MedicineBundleTranslator.ToDtoList(bundles);
    }
}
