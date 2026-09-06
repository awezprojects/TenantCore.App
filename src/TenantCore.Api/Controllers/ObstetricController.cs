using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.HistoryLookupItems.Commands;
using TenantCore.Application.Features.HistoryLookupItems.Queries;
using TenantCore.Application.Features.Obstetrics.Commands;
using TenantCore.Application.Features.Obstetrics.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/obstetric")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class ObstetricController(ISender sender) : ClinicControllerBase
{
    [HttpGet("prescriptions/{prescriptionId:guid}/dates")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(ObstetricDatesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetObstetricDates(Guid prescriptionId, CancellationToken ct)
        => Ok(await sender.Send(new GetObstetricDatesQuery(prescriptionId, GetApplicationId()), ct));

    [HttpGet("patients/{patientId:guid}/usg-chart")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(UsgChartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsgChart(Guid patientId, CancellationToken ct)
        => Ok(await sender.Send(new GetUsgChartByPatientQuery(patientId, GetApplicationId()), ct));

    [HttpPut("prescriptions/{prescriptionId:guid}/lmp")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(ObstetricDatesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetLmp(Guid prescriptionId, [FromBody] SetLmpRequest request, CancellationToken ct)
        => Ok(await sender.Send(new SetObstetricLmpCommand(prescriptionId, request, GetApplicationId()), ct));

    [HttpDelete("prescriptions/{prescriptionId:guid}/lmp")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(ObstetricDatesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearLmp(Guid prescriptionId, CancellationToken ct)
        => Ok(await sender.Send(new ClearObstetricLmpCommand(prescriptionId, GetApplicationId()), ct));

    [HttpPut("prescriptions/{prescriptionId:guid}/edd-by-usg")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(ObstetricDatesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetEddByUsg(Guid prescriptionId, [FromBody] SetEddByUsgRequest request, CancellationToken ct)
        => Ok(await sender.Send(new SetObstetricEddByUsgCommand(prescriptionId, request, GetApplicationId()), ct));

    [HttpGet("history-lookup")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(IEnumerable<HistoryLookupItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoryLookupItems(CancellationToken ct)
        => Ok(await sender.Send(new GetHistoryLookupItemsQuery(GetApplicationId()), ct));

    [HttpPost("history-lookup")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(HistoryLookupItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddHistoryLookupItem([FromBody] AddHistoryLookupItemDto dto, CancellationToken ct)
        => Ok(await sender.Send(new AddHistoryLookupItemCommand(GetApplicationId(), dto.Type, dto.Value), ct));
}
