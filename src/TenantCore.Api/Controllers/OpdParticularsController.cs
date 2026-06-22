using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Application.Features.OpdParticulars.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/opd-particulars")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class OpdParticularsController(ISender sender) : ClinicControllerBase
{
    [HttpGet("by-opd/{opdRegistrationId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<OpdParticularDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOpd(Guid opdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new GetOpdParticularsQuery(opdRegistrationId, GetApplicationId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add([FromBody] AddOpdParticularRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new AddOpdParticularCommand(request, GetApplicationId()), ct);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(OpdParticularDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOpdParticularRequest request, CancellationToken ct)
        => Ok(await sender.Send(new UpdateOpdParticularCommand(id, request, GetApplicationId()), ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        await sender.Send(new RemoveOpdParticularCommand(id, GetApplicationId()), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/collect")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Collect(Guid id, [FromBody] CollectOpdParticularRequest request, CancellationToken ct)
    {
        await sender.Send(new CollectOpdParticularCommand(id, GetApplicationId(), GetCurrentUserId(), request.CounterSessionId), ct);
        return NoContent();
    }

    [HttpPost("by-opd/{opdRegistrationId:guid}/collect-all")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CollectAll(Guid opdRegistrationId, [FromBody] CollectAllOpdParticularsRequest request, CancellationToken ct)
    {
        await sender.Send(new CollectAllOpdParticularsCommand(opdRegistrationId, GetApplicationId(), GetCurrentUserId(), request.CounterSessionId), ct);
        return NoContent();
    }
}
