using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.DoctorProfiles.Commands;
using TenantCore.Application.Features.DoctorProfiles.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/doctor-profile")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class DoctorProfileController(ISender sender) : ClinicControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetDoctorProfileQuery(userId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // DEF-MR-11: this is the doctor's own global profile (registration number, speciality,
    // qualifications) — not any one clinic's data — so it must not require a clinic to be
    // selected. RequireClinical checks the role for the CURRENTLY SELECTED clinic (via
    // X-Application-Id), which doesn't exist yet right after login; RequireAnyClinical checks
    // the same roles across every clinic the user belongs to instead.
    [HttpPut]
    [Authorize(Policy = AuthPolicies.RequireAnyClinical)]
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

    // Looks up another doctor's (non-sensitive, already-printed-on-every-Rx) profile info by
    // user id — used by the prescription print page to resolve the *prescribing* doctor's print
    // template preference, not the viewer's own, since anyone with access to a prescription can
    // open its print page (not only the doctor who wrote it).
    [HttpGet("by-user/{userId:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new GetDoctorProfileQuery(userId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // DEF-MR-11: same reasoning as UpsertMyProfile above — this is the doctor's own print
    // preference, not clinic data.
    [HttpPut("template")]
    [Authorize(Policy = AuthPolicies.RequireAnyClinical)]
    [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMyPrescriptionTemplate([FromBody] SetPrescriptionTemplateDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new SetDoctorPrescriptionTemplateCommand(userId, dto.Template), ct);
        return Ok(result);
    }
}
