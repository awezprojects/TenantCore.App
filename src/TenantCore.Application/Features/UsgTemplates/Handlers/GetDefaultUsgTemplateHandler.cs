using MediatR;
using TenantCore.Application.Features.Obstetrics.Helpers;
using TenantCore.Application.Features.UsgTemplates.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.UsgTemplates.Handlers;

public sealed class GetDefaultUsgTemplateHandler : IRequestHandler<GetDefaultUsgTemplateQuery, ClinicUsgTemplateDto>
{
    public Task<ClinicUsgTemplateDto> Handle(GetDefaultUsgTemplateQuery request, CancellationToken cancellationToken)
    {
        var dto = new ClinicUsgTemplateDto
        {
            ApplicationId = Guid.Empty,
            IsCustomized  = false,
            Rows          = DefaultUsgTemplateDefinition.Rows,
        };
        return Task.FromResult(dto);
    }
}
