using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.UsgTemplates.Commands;

public sealed record ResetClinicUsgTemplateCommand(Guid ApplicationId) : IRequest<ClinicUsgTemplateDto>;
