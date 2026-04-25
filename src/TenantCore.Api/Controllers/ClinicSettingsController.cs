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
public class ClinicSettingsController(ISender sender) : ControllerBase
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

    private Guid GetApplicationId()
    {
        var claim = User.FindFirst("app_ids");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
