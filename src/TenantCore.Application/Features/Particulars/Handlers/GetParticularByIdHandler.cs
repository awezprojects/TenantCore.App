using MediatR;
using TenantCore.Application.Features.Particulars.Queries;
using TenantCore.Application.Features.Particulars.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Particulars.Handlers;

public sealed class GetParticularByIdHandler(IParticularRepository repository) : IRequestHandler<GetParticularByIdQuery, ParticularDto>
{
    public async Task<ParticularDto> Handle(GetParticularByIdQuery request, CancellationToken cancellationToken)
    {
        var particular = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (particular is null || particular.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Particular), request.Id);

        return ParticularTranslator.ToDto(particular);
    }
}
