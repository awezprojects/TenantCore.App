using MediatR;
using TenantCore.Application.Features.Particulars.Commands;
using TenantCore.Application.Features.Particulars.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Particulars.Handlers;

public sealed class UpdateParticularHandler(IParticularRepository repository) : IRequestHandler<UpdateParticularCommand, ParticularDto>
{
    public async Task<ParticularDto> Handle(UpdateParticularCommand request, CancellationToken cancellationToken)
    {
        var particular = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (particular is null || particular.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Particular), request.Id);

        particular.Update(request.Request.Name, request.Request.DefaultAmount, request.Request.IsActive);
        repository.Update(particular);
        await repository.SaveChangesAsync(cancellationToken);
        return ParticularTranslator.ToDto(particular);
    }
}
