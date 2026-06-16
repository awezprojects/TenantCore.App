using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PregnancyTenures.Commands;
using TenantCore.Application.Features.PregnancyTenures.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Handlers;

public sealed class ClosePregnancyTenureHandler(
    IPregnancyTenureRepository repository,
    ILogger<ClosePregnancyTenureHandler> logger)
    : IRequestHandler<ClosePregnancyTenureCommand, PregnancyTenureDto>
{
    public async Task<PregnancyTenureDto> Handle(ClosePregnancyTenureCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Closing pregnancy tenure {TenureId}", request.TenureId);

        var tenure = await repository.GetByIdAsync(request.TenureId, cancellationToken)
            ?? throw new NotFoundException(nameof(PregnancyTenure), request.TenureId);

        if (tenure.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(PregnancyTenure), request.TenureId);

        if (tenure.Status == TenantCore.Shared.Enums.PregnancyTenureStatus.Closed)
            throw new InvalidOperationException("Pregnancy tenure is already closed.");

        tenure.Status = TenantCore.Shared.Enums.PregnancyTenureStatus.Closed;
        tenure.Outcome = request.Request.Outcome;
        tenure.Notes = request.Request.Notes;
        tenure.ClosedAt = DateTime.UtcNow;

        repository.Update(tenure);
        await repository.SaveChangesAsync(cancellationToken);

        return PregnancyTenureTranslator.ToDto(tenure);
    }
}
