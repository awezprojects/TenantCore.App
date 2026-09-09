using TenantCore.Shared.Dtos;
using TenantCore.Web.Client.Clients;

namespace TenantCore.Web.Client.Services;

/// <summary>
/// Caches the current clinic's feature flags so NavMenu, AuthorizedLayout and
/// every OPD/IPD/billing page read one shared value instead of each calling
/// the API independently. Call RefreshAsync after ClinicContext resolves the
/// selected clinic, and again after the Feature Flags settings page saves.
/// </summary>
public class FeatureFlagsContextService(IClinicApiClient clinicApi)
{
    public ClinicFeatureFlagsDto? Flags { get; private set; }
    public bool IsLoaded { get; private set; }

    /// <summary>True until flags have loaded — billing UI stays visible by default so it doesn't flash hidden on every page load.</summary>
    public bool IsBillingEnabled => !IsLoaded || (Flags?.BillingEnabled ?? true);

    public event Action? OnFeatureFlagsChanged;

    public async Task RefreshAsync()
    {
        var result = await clinicApi.GetFeatureFlagsAsync();
        Flags = result.Success ? result.Data : null;
        IsLoaded = true;
        OnFeatureFlagsChanged?.Invoke();
    }

    public void Clear()
    {
        Flags = null;
        IsLoaded = false;
        OnFeatureFlagsChanged?.Invoke();
    }
}
