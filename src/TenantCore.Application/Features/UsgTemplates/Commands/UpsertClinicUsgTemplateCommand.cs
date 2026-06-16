using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.UsgTemplates.Commands;

public sealed record UpsertClinicUsgTemplateCommand(UpsertClinicUsgTemplateRequest Request, Guid ApplicationId) : IRequest<ClinicUsgTemplateDto>;
