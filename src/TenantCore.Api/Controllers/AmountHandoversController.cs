using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/amount-handovers")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class AmountHandoversController(ISender sender) : ClinicControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AmountHandoverSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int utcOffset = 0,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetAmountHandoversQuery(GetApplicationId(), from, to, utcOffset), ct));

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<AmountHandoverSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending(CancellationToken ct)
        => Ok(await sender.Send(new GetPendingAmountHandoversQuery(GetApplicationId()), ct));

    [HttpGet("by-session/{counterSessionId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AmountHandoverSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySession(Guid counterSessionId, CancellationToken ct)
        => Ok(await sender.Send(new GetAmountHandoversBySessionQuery(counterSessionId, GetApplicationId()), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AmountHandoverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetAmountHandoverByIdQuery(id, GetApplicationId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAmountHandoverRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateAmountHandoverCommand(request, GetCurrentUserId(), GetApplicationId()), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("collect")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Collect([FromBody] CollectAmountHandoverRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CollectAmountHandoverCommand(
            request, GetCurrentUserId(), GetCurrentUserRole(), GetApplicationId()), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(AmountHandoverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new AcceptAmountHandoverCommand(id, GetApplicationId()), ct));

    [HttpPost("{id:guid}/dispute")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(AmountHandoverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Dispute(Guid id, [FromBody] ResolveAmountHandoverRequest request, CancellationToken ct)
        => Ok(await sender.Send(new DisputeAmountHandoverCommand(id, request, GetApplicationId()), ct));
}
