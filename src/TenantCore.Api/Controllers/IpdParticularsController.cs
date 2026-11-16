using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.IpdParticulars.Commands;
using TenantCore.Application.Features.IpdParticulars.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/ipd-particulars")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class IpdParticularsController(ISender sender) : ClinicControllerBase
{
    [HttpGet("{ipdRegistrationId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<IpdParticularDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByRegistration(Guid ipdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new GetIpdParticularsQuery(ipdRegistrationId, GetApplicationId()), ct));

    [HttpPost("{ipdRegistrationId:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicStaff)]
    [ProducesResponseType(typeof(IpdParticularDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid ipdRegistrationId, [FromBody] AddIpdParticularRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new AddIpdParticularCommand(
            ipdRegistrationId, GetApplicationId(), request.ParticularId, request.Amount, GetCurrentUserId()), ct);
        return CreatedAtAction(nameof(GetByRegistration), new { ipdRegistrationId }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicStaff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        await sender.Send(new RemoveIpdParticularCommand(id, GetApplicationId()), ct);
        return NoContent();
    }
}
