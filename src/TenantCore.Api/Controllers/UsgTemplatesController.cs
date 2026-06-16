using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.UsgTemplates.Commands;
using TenantCore.Application.Features.UsgTemplates.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/usg-templates")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class UsgTemplatesController(ISender sender) : ClinicControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ClinicUsgTemplateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClinicTemplate(CancellationToken ct)
        => Ok(await sender.Send(new GetClinicUsgTemplateQuery(GetApplicationId()), ct));

    [HttpGet("default")]
    [ProducesResponseType(typeof(ClinicUsgTemplateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefault(CancellationToken ct)
        => Ok(await sender.Send(new GetDefaultUsgTemplateQuery(), ct));

    [HttpPut]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(ClinicUsgTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromBody] UpsertClinicUsgTemplateRequest request, CancellationToken ct)
        => Ok(await sender.Send(new UpsertClinicUsgTemplateCommand(request, GetApplicationId()), ct));

    [HttpDelete]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(ClinicUsgTemplateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reset(CancellationToken ct)
        => Ok(await sender.Send(new ResetClinicUsgTemplateCommand(GetApplicationId()), ct));
}
