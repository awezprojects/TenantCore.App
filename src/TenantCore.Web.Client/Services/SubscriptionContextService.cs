using TenantCore.Shared.Dtos.Subscriptions;
using TenantCore.Web.Client.Clients;

namespace TenantCore.Web.Client.Services;

/// <summary>
/// Caches the current clinic's subscription status so AuthorizedLayout, the
/// dashboards and the top-bar pill all read one shared value instead of each
/// calling the API independently. Call RefreshAsync after ClinicContext
/// resolves the selected clinic, and again after Subscribe/Cancel succeeds.
/// </summary>
public class SubscriptionContextService(ISubscriptionApiClient subscriptionApi)
{
    public SubscriptionStatusDto? Status { get; private set; }
    public bool IsLoaded { get; private set; }

    /// <summary>True once status has loaded and the clinic has no currently-active subscription.</summary>
    public bool IsLocked => IsLoaded && Status?.HasActiveSubscription != true;

    public event Action? OnSubscriptionChanged;

    public async Task RefreshAsync()
    {
        var result = await subscriptionApi.GetStatusAsync();
        Status = result.Success ? result.Data : null;
        IsLoaded = true;
        OnSubscriptionChanged?.Invoke();
    }

    public void Clear()
    {
        Status = null;
        IsLoaded = false;
        OnSubscriptionChanged?.Invoke();
    }
}
