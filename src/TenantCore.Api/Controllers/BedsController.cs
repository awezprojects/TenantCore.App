using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Beds.Commands;
using TenantCore.Application.Features.Beds.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/beds")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class BedsController(ISender sender) : ClinicControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByRoom([FromQuery] Guid roomId, CancellationToken ct)
        => Ok(await sender.Send(new GetBedsByRoomQuery(roomId, GetApplicationId()), ct));

    [HttpGet("available")]
    [ProducesResponseType(typeof(IEnumerable<BedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable([FromQuery] Guid? wardId, CancellationToken ct)
        => Ok(await sender.Send(new GetAvailableBedsQuery(GetApplicationId(), wardId), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(BedDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateBedDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateBedCommand(GetApplicationId(), dto.RoomId, dto.BedNumber), ct);
        return CreatedAtAction(nameof(GetByRoom), new { roomId = result.RoomId }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteBedCommand(id, GetApplicationId()), ct);
        return NoContent();
    }
}
