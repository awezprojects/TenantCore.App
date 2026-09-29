namespace TenantCore.Infrastructure.ExternalServices;

/// <summary>Bound from the "AuthInternalApi" configuration section. ServiceKey must equal TenantCore.Auth's InternalApi:ServiceKey.</summary>
public sealed class AuthInternalApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ServiceKey { get; set; } = string.Empty;
}
