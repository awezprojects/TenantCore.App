using MediatR;
using TenantCore.Application.Features.Particulars.Commands;
using TenantCore.Application.Features.Particulars.Translators;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Particulars.Handlers;

public sealed class CreateParticularHandler(IParticularRepository repository) : IRequestHandler<CreateParticularCommand, Guid>
{
    public async Task<Guid> Handle(CreateParticularCommand request, CancellationToken cancellationToken)
    {
        var particular = ParticularTranslator.ToEntity(request);
        await repository.AddAsync(particular, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return particular.Id;
    }
}
