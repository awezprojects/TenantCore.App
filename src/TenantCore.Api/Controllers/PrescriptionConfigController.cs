using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.PrescriptionConfig.Commands;
using TenantCore.Application.Features.PrescriptionConfig.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/prescription-config")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class PrescriptionConfigController(ISender sender) : ClinicControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PrescriptionConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await sender.Send(new GetPrescriptionConfigQuery(GetApplicationId()), ct));

    [HttpPut]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(PrescriptionConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upsert([FromBody] UpdatePrescriptionConfigDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpsertPrescriptionConfigCommand(
            GetApplicationId(), dto.DefaultLanguage,
            dto.PrintMarginTop, dto.PrintMarginRight,
            dto.PrintMarginBottom, dto.PrintMarginLeft,
            dto.HideClinicHeader), ct));
}
