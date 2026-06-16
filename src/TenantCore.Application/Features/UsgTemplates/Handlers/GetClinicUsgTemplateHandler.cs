using MediatR;
using TenantCore.Application.Features.UsgTemplates.Queries;
using TenantCore.Application.Features.UsgTemplates.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.UsgTemplates.Handlers;

public sealed class GetClinicUsgTemplateHandler(IClinicUsgTemplateRepository repository)
    : IRequestHandler<GetClinicUsgTemplateQuery, ClinicUsgTemplateDto>
{
    public async Task<ClinicUsgTemplateDto> Handle(GetClinicUsgTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await repository.GetByApplicationIdWithRowsAsync(request.ApplicationId, cancellationToken);

        if (template is null || !template.IsCustomized)
            return UsgTemplateTranslator.ToDefaultDto(request.ApplicationId);

        return UsgTemplateTranslator.ToDto(template);
    }
}
