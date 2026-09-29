using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Api.Authentication;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Shared.Dtos.Onboarding;

namespace TenantCore.Api.Controllers.Internal;

/// <summary>
/// Every mutation the TenantCore.Admin portal needs, guarded by the "InternalService" scheme
/// (X-Internal-Service-Key) — never reachable with a user JWT. Hidden from Swagger outside
/// Development (Swagger UI/JSON is itself only registered under Development in Program.cs, so
/// this is never publicly discoverable in production regardless).
/// </summary>
[ApiController]
[Route("api/internal/onboarding")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = InternalServiceKeyAuthenticationHandler.SchemeName)]
public class InternalOnboardingController(ISender sender) : ControllerBase
{
    [HttpPost("requests/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveClinicOnboardingRequest request, CancellationToken ct)
    {
        await sender.Send(new ApproveClinicOnboardingCommand(
            id, request.SubscriptionPlanId, request.GrantTrial, request.Amount, request.AmountReason,
            request.ClinicCode, request.ReviewNote, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("requests/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectClinicOnboardingRequest request, CancellationToken ct)
    {
        await sender.Send(new RejectClinicOnboardingCommand(id, request.Reason, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("requests/{id:guid}/resend-payment-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendPaymentLink(Guid id, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        await sender.Send(new ResendPaymentLinkEmailCommand(id, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("requests/{id:guid}/change-amount")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeAmount(Guid id, [FromBody] ChangePaymentAmountRequest request, CancellationToken ct)
    {
        await sender.Send(new ChangePaymentAmountCommand(id, request.Amount, request.Reason, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("requests/{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Retry(Guid id, [FromBody] RetryClinicOnboardingRequest request, CancellationToken ct)
    {
        await sender.Send(new RetryClinicOnboardingCommand(id, request.ClinicCode, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("/api/internal/workflow-tasks/{taskId:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryWorkflowTask(Guid taskId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        await sender.Send(new RetryWorkflowTaskCommand(taskId, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }
}
