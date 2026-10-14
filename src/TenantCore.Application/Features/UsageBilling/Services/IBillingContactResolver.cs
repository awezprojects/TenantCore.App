namespace TenantCore.Application.Features.UsageBilling.Services;

/// <summary>
/// Who to bill for this clinic: the acting clinic user when there is one, else the application's own
/// contact details. Resolved fresh on every use — no persistent Auth-side "who pays" lookup exists.
/// </summary>
/// <param name="Name">Never blank: Razorpay's customer name and the notification payload both need one.</param>
public sealed record BillingContact(string Name, string Email, string? Phone, string ClinicName);

/// <summary>
/// Resolves the billing contact for a clinic-initiated payment (a top-up, or the registration fee).
/// A blank contact is what silently dead-letters the notification, so the fallbacks exist to make
/// that impossible rather than to be tidy.
/// </summary>
public interface IBillingContactResolver
{
    Task<BillingContact> ResolveAsync(Guid applicationId, Guid actingUserId, CancellationToken ct = default);
}
