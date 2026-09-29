using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Subscriptions.Commands;

namespace TenantCore.Api.Controllers;

/// <summary>
/// Razorpay's webhook delivery endpoint. Authenticity comes entirely from the HMAC signature
/// verified inside RecordPaymentWebhookHandler — this endpoint is intentionally anonymous
/// because Razorpay cannot send a bearer token. Body size capped so a malformed/oversized
/// delivery can never be used to exhaust memory.
/// </summary>
[ApiController]
[Route("api/payments/razorpay/webhook")]
[AllowAnonymous] // Authenticity = HMAC signature (X-Razorpay-Signature), verified in the handler.
public class RazorpayWebhookController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(64 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var eventId = Request.Headers["x-razorpay-event-id"].ToString();

        await sender.Send(new RecordPaymentWebhookCommand(rawBody, signature, eventId), ct);
        return Ok();
    }
}
