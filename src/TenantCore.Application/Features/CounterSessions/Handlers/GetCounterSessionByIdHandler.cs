using MediatR;
using TenantCore.Application.Features.CounterSessions.Queries;
using TenantCore.Application.Features.CounterSessions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Handlers;

public sealed class GetCounterSessionByIdHandler(ICounterSessionRepository repository) : IRequestHandler<GetCounterSessionByIdQuery, CounterSessionDto>
{
    public async Task<CounterSessionDto> Handle(GetCounterSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (session is null || session.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(CounterSession), request.Id);

        return CounterSessionTranslator.ToDto(session);
    }
}
