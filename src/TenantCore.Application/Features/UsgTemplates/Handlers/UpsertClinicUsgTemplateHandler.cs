using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.UsgTemplates.Commands;
using TenantCore.Application.Features.UsgTemplates.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.UsgTemplates.Handlers;

public sealed class UpsertClinicUsgTemplateHandler(
    IClinicUsgTemplateRepository repository,
    ILogger<UpsertClinicUsgTemplateHandler> logger)
    : IRequestHandler<UpsertClinicUsgTemplateCommand, ClinicUsgTemplateDto>
{
    public async Task<ClinicUsgTemplateDto> Handle(UpsertClinicUsgTemplateCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Upserting USG template for application {ApplicationId}", request.ApplicationId);

        var existing = await repository.GetByApplicationIdWithRowsAsync(request.ApplicationId, cancellationToken);

        if (existing is null)
        {
            var template = new ClinicUsgTemplate
            {
                ApplicationId = request.ApplicationId,
                IsCustomized  = true,
            };

            var rows = request.Request.Rows
                .Select(r => UsgTemplateTranslator.RowFromDto(r, template.Id))
                .ToList();

            template.Rows = rows;

            await repository.AddAsync(template, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            return UsgTemplateTranslator.ToDto(template);
        }

        repository.RemoveRows(existing.Rows.ToList());
        existing.IsCustomized = true;
        repository.Update(existing);
        await repository.SaveChangesAsync(cancellationToken);

        var newRows = request.Request.Rows
            .Select(r => UsgTemplateTranslator.RowFromDto(r, existing.Id))
            .ToList();
        await repository.AddRowsAsync(newRows, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var reloaded = await repository.GetByApplicationIdWithRowsAsync(request.ApplicationId, cancellationToken);
        return UsgTemplateTranslator.ToDto(reloaded!);
    }
}
