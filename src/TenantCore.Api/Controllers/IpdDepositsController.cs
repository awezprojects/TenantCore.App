using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.IpdDeposits.Commands;
using TenantCore.Application.Features.IpdDeposits.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/ipd-deposits")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class IpdDepositsController(ISender sender) : ClinicControllerBase
{
    [HttpGet("{ipdRegistrationId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<IpdDepositDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByRegistration(Guid ipdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new GetIpdDepositsQuery(ipdRegistrationId, GetApplicationId()), ct));

    [HttpPost("{ipdRegistrationId:guid}")]
    [Authorize(Policy = AuthPolicies.RequireClinicStaff)]
    [ProducesResponseType(typeof(IpdDepositDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Collect(Guid ipdRegistrationId, [FromBody] CollectIpdDepositRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CollectIpdDepositCommand(
            ipdRegistrationId, GetApplicationId(), request.Amount, GetCurrentUserId(), request.Notes), ct);
        return CreatedAtAction(nameof(GetByRegistration), new { ipdRegistrationId }, result);
    }
}
