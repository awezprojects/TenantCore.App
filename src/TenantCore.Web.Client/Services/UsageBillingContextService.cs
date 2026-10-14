using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;
using TenantCore.Web.Client.Clients;

namespace TenantCore.Web.Client.Services;

/// <summary>
/// Caches the current clinic's billing summary (mode, wallet, registration fee) so the top-bar
/// chip, the billing screens and the create flows all read one shared value instead of each
/// calling the API independently. Call RefreshAsync after ClinicContext resolves the selected
/// clinic, after a wallet top-up is confirmed and after any call is refused with
/// <c>insufficient_wallet_balance</c>.
/// </summary>
public class UsageBillingContextService(IUsageBillingApiClient usageBillingApi)
{
    public ClinicBillingSummaryDto? Summary { get; private set; }
    public bool IsLoaded { get; private set; }

    /// <summary>The failure message of the last refresh, so a page can show something readable.</summary>
    public string? LastError { get; private set; }

    /// <summary>True once summary has loaded and the clinic is metered rather than on a package.</summary>
    public bool IsPayAsYouGo => IsLoaded && Summary?.EffectiveBillingMode == BillingMode.PayAsYouGo;

    /// <summary>True once summary has loaded and the wallet is at or below its warning level.</summary>
    public bool WalletIsLow => IsLoaded && Summary?.WalletIsLow == true;

    /// <summary>True once summary has loaded and the wallet has no balance (zero or negative).</summary>
    public bool WalletIsEmpty => IsLoaded && IsPayAsYouGo && (Summary?.WalletBalance ?? 0m) <= 0m;

    /// <summary>True once summary has loaded and the one-time registration fee is still outstanding.</summary>
    public bool RegistrationFeeDue => IsLoaded && Summary?.RegistrationFeeDue == true;

    public event Action? OnBillingChanged;

    public async Task RefreshAsync()
    {
        var result = await usageBillingApi.GetSummaryAsync();
        Summary = result.Success ? result.Data : null;
        LastError = result.Success ? null : result.Message;
        IsLoaded = true;
        OnBillingChanged?.Invoke();
    }

    public void Clear()
    {
        Summary = null;
        LastError = null;
        IsLoaded = false;
        OnBillingChanged?.Invoke();
    }
}
