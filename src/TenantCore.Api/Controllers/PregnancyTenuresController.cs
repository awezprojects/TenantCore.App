using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.PregnancyTenures.Commands;
using TenantCore.Application.Features.PregnancyTenures.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class PregnancyTenuresController(ISender sender) : ClinicControllerBase
{
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IEnumerable<PregnancyTenureSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue(CancellationToken ct)
        => Ok(await sender.Send(new GetOverdueEddPatientsQuery(GetApplicationId()), ct));

    [HttpGet("active/{patientId:guid}")]
    [ProducesResponseType(typeof(PregnancyTenureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActiveForPatient(Guid patientId, CancellationToken ct)
    {
        var result = await sender.Send(new GetActivePregnancyTenureForPatientQuery(patientId, GetApplicationId()), ct);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpGet("patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PregnancyTenureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForPatient(Guid patientId, CancellationToken ct)
        => Ok(await sender.Send(new GetPatientPregnancyTenuresQuery(patientId, GetApplicationId()), ct));

    [HttpPut("{id:guid}/close")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(PregnancyTenureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(Guid id, [FromBody] ClosePregnancyTenureRequest request, CancellationToken ct)
        => Ok(await sender.Send(new ClosePregnancyTenureCommand(id, request, GetApplicationId()), ct));
}
