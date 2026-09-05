using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Queries;
using TenantCore.Application.Features.Clinics.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/clinic-settings")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class ClinicSettingsController(ISender sender) : ClinicControllerBase
{
    [HttpGet("fees")]
    [ProducesResponseType(typeof(ClinicFeeConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFees(CancellationToken ct)
        => Ok(await sender.Send(new GetClinicFeeConfigQuery(GetApplicationId()), ct));

    [HttpPut("fees")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(ClinicFeeConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFees([FromBody] UpdateClinicFeeConfigDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateClinicFeeConfigCommand(GetApplicationId(), dto.OpdFee), ct));

    [HttpGet("doctors")]
    [ProducesResponseType(typeof(IEnumerable<DoctorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors(CancellationToken ct)
    {
        var applicationId = GetApplicationId();
        return Ok(await sender.Send(new GetClinicDoctorsQuery(applicationId), ct));
    }

    [HttpGet("feature-flags")]
    [ProducesResponseType(typeof(ClinicFeatureFlagsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatureFlags(CancellationToken ct)
        => Ok(await sender.Send(new GetClinicFeatureFlagsQuery(GetApplicationId()), ct));

    [HttpPut("feature-flags")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(ClinicFeatureFlagsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFeatureFlags([FromBody] UpdateClinicFeatureFlagsDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateClinicFeatureFlagsCommand(GetApplicationId(), dto.PrepaidOpdEnabled), ct));

    [HttpGet("states")]
    [ProducesResponseType(typeof(IEnumerable<StateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStates(CancellationToken ct)
        => Ok(await sender.Send(new GetStatesQuery(), ct));

    [HttpGet("states/{stateId:guid}/cities")]
    [ProducesResponseType(typeof(IEnumerable<CityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCitiesByState(Guid stateId, CancellationToken ct)
        => Ok(await sender.Send(new GetCitiesByStateQuery(stateId), ct));

    [HttpGet("location")]
    [ProducesResponseType(typeof(ClinicLocationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocation(CancellationToken ct)
        => Ok(await sender.Send(new GetClinicLocationQuery(GetApplicationId()), ct));

    [HttpPut("location")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(ClinicLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertLocation([FromBody] UpsertClinicLocationDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpsertClinicLocationCommand(GetApplicationId(), dto.StateId, dto.CityId), ct));
}
