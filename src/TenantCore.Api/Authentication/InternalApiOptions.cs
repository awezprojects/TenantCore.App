namespace TenantCore.Api.Authentication;

/// <summary>Bound from the "InternalApi" configuration section. Guards api/internal/* — the Admin portal sends this key.</summary>
public sealed class InternalApiOptions
{
    public string AdminPortalKey { get; set; } = string.Empty;
}
