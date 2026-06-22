using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Application.Features.ExpenseRecords.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/expense-records")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class ExpenseRecordsController(ISender sender) : ClinicControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExpenseRecordSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int utcOffset = 0,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetExpenseRecordsQuery(GetApplicationId(), from, to, utcOffset), ct));

    [HttpGet("by-session/{sessionId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseRecordSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySession(Guid sessionId, CancellationToken ct)
        => Ok(await sender.Send(new GetExpenseRecordsBySessionQuery(sessionId, GetApplicationId()), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetExpenseRecordByIdQuery(id, GetApplicationId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRecordRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateExpenseRecordCommand(request, GetCurrentUserId(), GetApplicationId()), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/pay")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(ExpenseRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pay(Guid id, [FromBody] PayExpenseRecordRequest request, CancellationToken ct)
        => Ok(await sender.Send(new PayExpenseRecordCommand(id, request, GetApplicationId()), ct));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(ExpenseRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseRecordRequest request, CancellationToken ct)
        => Ok(await sender.Send(new UpdateExpenseRecordCommand(id, request, GetApplicationId()), ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteExpenseRecordCommand(id, GetApplicationId()), ct);
        return NoContent();
    }
}
