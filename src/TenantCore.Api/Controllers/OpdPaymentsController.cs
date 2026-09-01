using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/opd-payments")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class OpdPaymentsController(ISender sender) : ClinicControllerBase
{
    [HttpGet("by-opd/{opdRegistrationId:guid}")]
    [ProducesResponseType(typeof(OpdPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOpd(Guid opdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new GetOpdPaymentByOpdIdQuery(opdRegistrationId, GetApplicationId()), ct));

    [HttpGet("by-session/{sessionId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SessionCollectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySession(Guid sessionId, CancellationToken ct)
        => Ok(await sender.Send(new GetOpdCollectionsBySessionQuery(sessionId, GetApplicationId()), ct));

    [HttpPost("ensure")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ensure([FromBody] EnsureOpdPaymentRequest request, CancellationToken ct)
        => Ok(await sender.Send(new EnsureOpdPaymentCommand(request.OpdRegistrationId, request.DoctorProfileId, GetApplicationId()), ct));

    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(OpdPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptOpdPaymentRequest request, CancellationToken ct)
        => Ok(await sender.Send(new AcceptOpdPaymentCommand(request, GetCurrentUserId(), GetApplicationId()), ct));

    [HttpPost("{id:guid}/discount")]
    [Authorize(Policy = AuthPolicies.RequireClinicStaff)]
    [ProducesResponseType(typeof(OpdPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApplyDiscount(Guid id, [FromBody] ApplyOpdDiscountRequest request, CancellationToken ct)
        => Ok(await sender.Send(new ApplyOpdDiscountCommand(request, GetApplicationId()), ct));

    [HttpPost("{id:guid}/accept-full")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(OpdPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptFull(Guid id, [FromBody] AcceptOpdPaymentFullRequest request, CancellationToken ct)
        => Ok(await sender.Send(new AcceptOpdPaymentFullCommand(request, GetCurrentUserId(), GetApplicationId()), ct));

    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(OpdPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Refund(Guid id, [FromBody] ProcessOpdRefundRequest request, CancellationToken ct)
        => Ok(await sender.Send(new ProcessOpdRefundCommand(request, GetCurrentUserId(), GetApplicationId()), ct));
}
