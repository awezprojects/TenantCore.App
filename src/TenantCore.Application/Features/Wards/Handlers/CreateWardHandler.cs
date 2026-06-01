using MediatR;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Application.Features.Wards.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.Wards.Handlers;

public sealed class CreateWardHandler(IWardRepository repository) : IRequestHandler<CreateWardCommand, WardDto>
{
    public async Task<WardDto> Handle(CreateWardCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByNameAsync(request.ApplicationId, request.Name, cancellationToken: cancellationToken))
            throw new InvalidOperationException(UserMessages.WardNameTaken);

        var ward = Ward.Create(request.ApplicationId, request.Name, request.Description);
        await repository.AddAsync(ward, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return WardTranslator.ToDtoSimple(ward);
    }
}
