using TenantCore.Web.Client.Clients;

namespace TenantCore.Web.Client.Services;

/// <summary>
/// DEF-OPD-19: with access tokens now 60 minutes (DEF-AUTH-OPD-02) and nothing calling
/// <see cref="IAuthApiClient.RefreshTokenAsync"/> except one onboarding page, users were signed out
/// every 60 minutes. Proactively refreshes well before expiry for as long as the app stays open, so
/// a session survives past the access-token window without the user noticing.
///
/// Deliberately time-based rather than reading the exact expiry: simpler, and 20 minutes leaves
/// three attempts inside every 60-minute window if one tick is missed (e.g. the tab was asleep).
/// </summary>
public class TokenRefreshTimerService(AuthStateService authState, IAuthApiClient authApi) : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(20);
    private Timer? _timer;

    public void Start()
    {
        _timer ??= new Timer(_ => _ = TickAsync(), null, Interval, Interval);
    }

    private async Task TickAsync()
    {
        if (!authState.IsAuthenticated) return;

        var result = await authApi.RefreshTokenAsync();
        if (result.Success && result.Data is not null)
            await authState.SetAuthenticatedStateAsync(result.Data);
        // A failed refresh is left alone — the user simply gets signed out at natural expiry,
        // same as today, rather than this timer forcing a logout early.
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
