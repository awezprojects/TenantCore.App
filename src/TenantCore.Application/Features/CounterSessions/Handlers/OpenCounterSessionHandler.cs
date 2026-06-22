using MediatR;
using TenantCore.Application.Features.CounterSessions.Commands;
using TenantCore.Application.Features.CounterSessions.Translators;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.CounterSessions.Handlers;

public sealed class OpenCounterSessionHandler(ICounterSessionRepository repository) : IRequestHandler<OpenCounterSessionCommand, Guid>
{
    public async Task<Guid> Handle(OpenCounterSessionCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetActiveSessionAsync(request.ApplicationId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("A counter session is already open.");

        var session = CounterSessionTranslator.ToEntity(request);
        await repository.AddAsync(session, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return session.Id;
    }
}
