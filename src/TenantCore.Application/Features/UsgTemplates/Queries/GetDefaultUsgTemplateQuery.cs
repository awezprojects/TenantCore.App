using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.UsgTemplates.Queries;

public sealed record GetDefaultUsgTemplateQuery : IRequest<ClinicUsgTemplateDto>;
