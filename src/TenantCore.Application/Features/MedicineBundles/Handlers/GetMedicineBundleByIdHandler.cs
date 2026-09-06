using MediatR;
using TenantCore.Application.Features.MedicineBundles.Queries;
using TenantCore.Application.Features.MedicineBundles.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Handlers;

public sealed class GetMedicineBundleByIdHandler(IMedicineBundleRepository repository)
    : IRequestHandler<GetMedicineBundleByIdQuery, MedicineBundleDto>
{
    public async Task<MedicineBundleDto> Handle(GetMedicineBundleByIdQuery request, CancellationToken cancellationToken)
    {
        var bundle = await repository.GetByIdWithItemsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineBundle), request.Id);

        if (bundle.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(MedicineBundle), request.Id);

        return MedicineBundleTranslator.ToDto(bundle);
    }
}
