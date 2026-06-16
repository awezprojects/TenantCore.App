using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantCore.Application.Features.DoctorProfiles.Commands;
using TenantCore.Application.Features.DoctorProfiles.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/doctor-profile")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class DoctorProfileController(ISender sender) : ControllerBase
{
    private Guid GetCurrentUserId()
    {
        // MapInboundClaims = false in JWT options means ClaimTypes.NameIdentifier (long URI) is never
        // mapped back — the token arrives with the short JWT name "nameid" written by JwtSecurityTokenHandler.
        var claim = User.FindFirst("nameid")
                 ?? User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetDoctorProfileQuery(userId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertMyProfile([FromBody] UpsertDoctorProfileDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(
            new UpsertDoctorProfileCommand(userId, dto.RegistrationNumber, dto.SpecialityId, dto.QualificationDetails),
            ct);
        return Ok(result);
    }
}
