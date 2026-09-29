using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos.Onboarding;

namespace TenantCore.Api.Controllers;

/// <summary>
/// User-facing clinic onboarding requests — every action is scoped to the caller's own user id
/// from the token. Never accepts or trusts an ApplicationId (no clinic exists yet for these).
/// Inherits ClinicControllerBase only to reuse its claim-parsing helpers (GetCurrentUserId); it
/// never calls GetApplicationId().
/// </summary>
[ApiController]
[Route("api/onboarding/requests")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class ClinicOnboardingController(ISender sender) : ClinicControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit([FromBody] SubmitClinicOnboardingRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? string.Empty;
        // No display-name claim exists on the token — the doctor's name from the form itself is
        // the best available snapshot, and the person submitting is almost always the doctor.
        var name = request.DoctorName;

        var id = await sender.Send(new SubmitClinicOnboardingCommand(userId, name, email, request), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<ClinicOnboardingRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => Ok(await sender.Send(new GetMyClinicOnboardingRequestsQuery(GetCurrentUserId()), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClinicOnboardingRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetMyClinicOnboardingRequestByIdQuery(id, GetCurrentUserId()), ct));

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await sender.Send(new CancelClinicOnboardingCommand(id, GetCurrentUserId()), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/check-payment")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CheckPayment(Guid id, CancellationToken ct)
    {
        await sender.Send(new CheckOnboardingPaymentCommand(id, GetCurrentUserId()), ct);
        return Accepted();
    }
}
