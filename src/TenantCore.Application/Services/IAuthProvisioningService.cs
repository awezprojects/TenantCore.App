namespace TenantCore.Application.Services;

public enum ProvisioningOutcomeKind
{
    Success,
    Transient,
    Permanent
}

public sealed record ProvisioningOutcome(ProvisioningOutcomeKind Kind, Guid? ApplicationId, string? ErrorMessage)
{
    public static ProvisioningOutcome Success(Guid applicationId) => new(ProvisioningOutcomeKind.Success, applicationId, null);
    public static ProvisioningOutcome Transient(string error) => new(ProvisioningOutcomeKind.Transient, null, error);
    public static ProvisioningOutcome Permanent(string error) => new(ProvisioningOutcomeKind.Permanent, null, error);
}

/// <summary>
/// Port over TenantCore.Auth's internal, service-key-protected clinic provisioning endpoint.
/// Implemented in Infrastructure using a service key rather than a user bearer token — this is
/// called from the background worker, which has no HTTP request or user token available.
/// </summary>
public interface IAuthProvisioningService
{
    /// <summary>
    /// Idempotent by provisioningReference — a repeat call for the same reference returns the
    /// already-created clinic (Success) instead of a duplicate or an error.
    /// </summary>
    Task<ProvisioningOutcome> ProvisionClinicAsync(
        Guid provisioningReference, Guid ownerUserId,
        string clinicName, string clinicCode, string? address, string? contactNumber,
        string? contactPerson, string? registrationNumber, string? officialEmail, string? website,
        CancellationToken ct = default);
}
