using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Logs.Commands;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/logs")]
[Produces("application/json")]
public class LogsController(ISender sender) : ClinicControllerBase
{
    // Anonymous — pre-login/pre-auth frontend crashes (e.g. on the login screen)
    // must still be captured. Never returns anything beyond 204/400, never echoes
    // caller-supplied data back.
    [HttpPost("frontend-error")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogFrontendError([FromBody] FrontendErrorLogRequest request, CancellationToken ct)
    {
        var contextApplicationId = GetApplicationId() == Guid.Empty ? (Guid?)null : GetApplicationId();

        await sender.Send(new LogFrontendErrorCommand(request, contextApplicationId), ct);
        return NoContent();
    }
}
