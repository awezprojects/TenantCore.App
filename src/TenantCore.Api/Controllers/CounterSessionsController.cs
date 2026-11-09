using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.CounterSessions.Commands;
using TenantCore.Application.Features.CounterSessions.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/counter-sessions")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class CounterSessionsController(ISender sender) : ClinicControllerBase
{
    // Reception/admin only — this is the clinic-wide cash-session report (used by the Finance
    // Dashboard); it is not needed by any doctor-facing page (doctors use GetActive/GetAllActive
    // to check the live session before collecting a fee). DEF-FB-03: also forbidden while the
    // caller has explicitly chosen to view as "Doctor" this session, even if their role would
    // otherwise allow it — see ClinicControllerBase.IsDoctorPanelView().
    [HttpGet]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(IEnumerable<CounterSessionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        return Ok(await sender.Send(new GetCounterSessionsQuery(GetApplicationId(), from, to), ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CounterSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetCounterSessionByIdQuery(id, GetApplicationId()), ct));

    [HttpGet("active")]
    [ProducesResponseType(typeof(CounterSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => Ok(await sender.Send(new GetActiveCounterSessionQuery(GetApplicationId()), ct));

    [HttpGet("all-active")]
    [Authorize(Policy = AuthPolicies.RequireClinical)]
    [ProducesResponseType(typeof(IEnumerable<CounterSessionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllActive(CancellationToken ct)
        => Ok(await sender.Send(new GetAllActiveCounterSessionsQuery(GetApplicationId()), ct));

    [HttpPost("open")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Open([FromBody] OpenCounterSessionRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new OpenCounterSessionCommand(request, GetCurrentUserId(), GetApplicationId()), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(CounterSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new CloseCounterSessionCommand(id, GetApplicationId()), ct));
}
