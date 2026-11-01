using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TenantCore.Api.Authentication;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Queries;
using TenantCore.Shared.Dtos.Onboarding;
using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Api.Controllers.Internal;

/// <summary>
/// The dummy-data tool the TenantCore.Admin portal drives, guarded by the "InternalService" scheme
/// (X-Internal-Service-Key) — never reachable with a user JWT.
///
/// Every route is additionally gated by DummyData:Enabled and refuses with 403 when it is off, so the
/// whole surface is inert in Production whatever the portal chooses to show. applicationId always
/// comes from the route, never from a body.
/// </summary>
[ApiController]
[Route("api/internal/clinics/{applicationId:guid}/dummy-data")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = InternalServiceKeyAuthenticationHandler.SchemeName)]
public class InternalDummyDataController(ISender sender, IOptions<DummyDataOptions> options) : ControllerBase
{
    /// <summary>Current counts for the clinic. Read-only — used by the Test Data page's stats grid.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DummyDataStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatus(Guid applicationId, CancellationToken ct)
    {
        EnsureEnabled();
        return Ok(await sender.Send(new GetDummyDataStatusQuery(applicationId), ct));
    }

    /// <summary>Creates the canonical dummy set in one transaction. 409 when the clinic already has dummy data.</summary>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(DummyDataStatusDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Seed(
        Guid applicationId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        EnsureEnabled();

        var status = await sender.Send(new SeedDummyDataCommand(
            applicationId, request.AdminUserId, request.AdminEmail), ct);

        return StatusCode(StatusCodes.Status201Created, status);
    }

    /// <summary>Cascade-deletes every dummy record. 409 when there is nothing to purge.</summary>
    [HttpDelete]
    [ProducesResponseType(typeof(DummyDataStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Purge(
        Guid applicationId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        EnsureEnabled();

        return Ok(await sender.Send(new PurgeDummyDataCommand(
            applicationId, request.AdminUserId, request.AdminEmail), ct));
    }

    /// <summary>
    /// Thrown rather than returned, so the refusal flows through ExceptionHandlingMiddleware like
    /// every other one and the portal can key off the stable errorCode.
    /// </summary>
    private void EnsureEnabled()
    {
        if (!options.Value.Enabled)
            throw new ErrorCodeException(
                StatusCodes.Status403Forbidden,
                "dummy_data_disabled",
                "The dummy-data tool is switched off in this environment.");
    }
}
