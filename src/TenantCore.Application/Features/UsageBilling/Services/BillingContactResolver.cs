using TenantCore.Application.Services;

namespace TenantCore.Application.Features.UsageBilling.Services;

/// <inheritdoc />
public sealed class BillingContactResolver(IAuthApplicationService authApplicationService) : IBillingContactResolver
{
    public async Task<BillingContact> ResolveAsync(Guid applicationId, Guid actingUserId, CancellationToken ct = default)
    {
        var application = await authApplicationService.GetApplicationByIdAsync(applicationId, ct);
        var users = await authApplicationService.GetApplicationUsersAsync(applicationId, ct);
        var actingUser = users?.FirstOrDefault(u => u.UserId == actingUserId);

        var email = actingUser?.EmailId ?? application?.OfficialEmail ?? string.Empty;
        var name = actingUser?.FullName ?? application?.ContactPerson ?? application?.ApplicationName ?? string.Empty;
        var phone = actingUser?.MobileNo ?? application?.ContactNumber;
        var clinicName = application?.ApplicationName ?? string.Empty;

        // The notification consumer dead-letters a payload with a blank name (workspace rule 9), and a
        // link with no email cannot be sent at all. Both fall back to the clinic's own identity rather
        // than to an empty string, so the payment can still be created and collected another way.
        return new BillingContact(
            string.IsNullOrWhiteSpace(name) ? clinicName : name,
            email,
            phone,
            clinicName);
    }
}
