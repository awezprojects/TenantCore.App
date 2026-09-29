using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Services;

namespace TenantCore.Infrastructure.ExternalServices;

/// <summary>
/// Calls TenantCore.Auth's internal clinic-provisioning endpoint. Runs from the background
/// worker, which has no HTTP request and no user bearer token — authenticates with the shared
/// X-Internal-Service-Key header instead (see plan's decision 6: shared key until the Entra move).
/// </summary>
public sealed class AuthProvisioningService(
    IHttpClientFactory httpClientFactory,
    IOptions<AuthInternalApiOptions> options,
    IWorkflowCorrelationContext correlationContext,
    ILogger<AuthProvisioningService> logger) : IAuthProvisioningService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ClientName = "AuthInternalApi";

    // Mirrors TenantCore.Auth's ProvisionClinicErrorCodes — permanent failures the App should
    // never retry automatically; they need an admin to change something (usually the clinic code).
    private static readonly string[] PermanentErrorCodes = ["CodeTaken", "OwnerNotFound", "OwnerInactive", "RolesNotConfigured", "Invalid"];

    public async Task<ProvisioningOutcome> ProvisionClinicAsync(
        Guid provisioningReference, Guid ownerUserId,
        string clinicName, string clinicCode, string? address, string? contactNumber,
        string? contactPerson, string? registrationNumber, string? officialEmail, string? website,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ServiceKey))
            return ProvisioningOutcome.Permanent("AuthInternalApi:ServiceKey is not configured.");

        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Remove("X-Internal-Service-Key");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Internal-Service-Key", options.Value.ServiceKey);

        // Cross-references this call with the workflow attempt's own ActionLogs entry and with
        // Auth's own request logs for the same call — the background worker has no HttpContext to
        // draw a correlation id from otherwise.
        if (correlationContext.CorrelationId is { } correlationId)
        {
            client.DefaultRequestHeaders.Remove("X-Correlation-Id");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", correlationId.ToString());
        }

        var payload = new
        {
            provisioningReference,
            ownerUserId,
            clinicName,
            clinicCode,
            address,
            contactNumber,
            contactPerson,
            registrationNumber,
            officialEmail,
            website
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("api/internal/clinics/provision", payload, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Auth provisioning call failed — network/timeout for reference {ProvisioningReference}", provisioningReference);
            return ProvisioningOutcome.Transient(ex.Message);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // A config problem (key mismatch), not something a retry fixes — but flag loudly,
            // since every subsequent provisioning attempt will fail identically until fixed.
            logger.LogError("Auth provisioning call rejected — service key mismatch for reference {ProvisioningReference}.", provisioningReference);
            return ProvisioningOutcome.Permanent("Auth rejected the service key — check AuthInternalApi:ServiceKey matches Auth's InternalApi:ServiceKey.");
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            var isTransient = (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout;
            logger.LogWarning("Auth provisioning returned {StatusCode} for reference {ProvisioningReference}", (int)response.StatusCode, provisioningReference);
            return isTransient
                ? ProvisioningOutcome.Transient($"Auth returned HTTP {(int)response.StatusCode}.")
                : ProvisioningOutcome.Permanent($"Auth returned HTTP {(int)response.StatusCode}.");
        }

        ApiResponseWrapper<ProvisionClinicResponseBody>? wrapper;
        try
        {
            wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<ProvisionClinicResponseBody>>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Auth provisioning response for reference {ProvisioningReference}", provisioningReference);
            return ProvisioningOutcome.Transient("Failed to parse Auth's response.");
        }

        if (wrapper is { Success: true } && wrapper.Data is { ApplicationId: var applicationId } && applicationId != Guid.Empty)
            return ProvisioningOutcome.Success(applicationId);

        var errorCode = wrapper?.Data?.ErrorCode;
        var message = wrapper?.Message ?? "Auth provisioning failed.";

        if (errorCode != null && PermanentErrorCodes.Contains(errorCode))
            return ProvisioningOutcome.Permanent(message);

        // Success:false with no recognised permanent error code — treat cautiously as transient
        // rather than silently giving up, since an unrecognised code is more likely a version
        // mismatch than a genuinely permanent business rejection.
        return ProvisioningOutcome.Transient(message);
    }

    private sealed class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private sealed class ProvisionClinicResponseBody
    {
        public Guid ApplicationId { get; set; }
        public string? ClinicName { get; set; }
        public string? ClinicCode { get; set; }
        public Guid OwnerUserId { get; set; }
        public bool AlreadyExisted { get; set; }
        public string? ErrorCode { get; set; }
    }
}
