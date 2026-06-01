using MediatR;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Application.Features.Wards.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Wards.Handlers;

public sealed class UpdateWardHandler(IWardRepository repository) : IRequestHandler<UpdateWardCommand, WardDto>
{
    public async Task<WardDto> Handle(UpdateWardCommand request, CancellationToken cancellationToken)
    {
        var ward = await repository.GetByIdWithRoomsAsync(request.Id, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Ward), request.Id);

        if (await repository.ExistsByNameAsync(request.ApplicationId, request.Name, excludeId: request.Id, cancellationToken: cancellationToken))
            throw new InvalidOperationException(TenantCore.Shared.Errors.UserMessages.WardNameTaken);

        ward.Update(request.Name, request.Description);
        repository.Update(ward);
        await repository.SaveChangesAsync(cancellationToken);
        return WardTranslator.ToDto(ward);
    }
}
