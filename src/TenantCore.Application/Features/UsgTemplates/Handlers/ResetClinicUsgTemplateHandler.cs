using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.UsgTemplates.Commands;
using TenantCore.Application.Features.UsgTemplates.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.UsgTemplates.Handlers;

public sealed class ResetClinicUsgTemplateHandler(
    IClinicUsgTemplateRepository repository,
    ILogger<ResetClinicUsgTemplateHandler> logger)
    : IRequestHandler<ResetClinicUsgTemplateCommand, ClinicUsgTemplateDto>
{
    public async Task<ClinicUsgTemplateDto> Handle(ResetClinicUsgTemplateCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Resetting USG template for application {ApplicationId}", request.ApplicationId);

        var existing = await repository.GetByApplicationIdWithRowsAsync(request.ApplicationId, cancellationToken);

        if (existing is not null)
        {
            repository.RemoveRows(existing.Rows.ToList());
            existing.IsCustomized = false;
            repository.Update(existing);
            await repository.SaveChangesAsync(cancellationToken);
        }

        return UsgTemplateTranslator.ToDefaultDto(request.ApplicationId);
    }
}
