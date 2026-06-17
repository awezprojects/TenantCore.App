using Microsoft.JSInterop;

namespace TenantCore.Web.Client.Services;

public class ClinicContextService
{
    private readonly IJSRuntime _js;
    private const string AppIdKey      = "cc_selected_app_id";
    private const string AppNameKey    = "cc_selected_app_name";
    private const string ActivePanelKey = "cc_active_panel";

    public Guid   SelectedApplicationId   { get; private set; }
    public string SelectedApplicationName { get; private set; } = string.Empty;
    public string ActivePanel             { get; private set; } = string.Empty;

    public event Action? OnClinicChanged;

    public ClinicContextService(IJSRuntime js) => _js = js;

    public async Task SetClinicAsync(Guid appId, string appName)
    {
        SelectedApplicationId   = appId;
        SelectedApplicationName = appName;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", AppIdKey,   appId.ToString());
            await _js.InvokeVoidAsync("localStorage.setItem", AppNameKey, appName);
        }
        catch { }
        OnClinicChanged?.Invoke();
    }

    // Always call SetClinicAsync immediately after — it fires OnClinicChanged so subscribers see the updated panel.
    public async Task SetActivePanelAsync(string panel)
    {
        ActivePanel = panel;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", ActivePanelKey, panel);
        }
        catch { }
    }

    public async Task ClearClinicAsync()
    {
        SelectedApplicationId   = Guid.Empty;
        SelectedApplicationName = string.Empty;
        ActivePanel             = string.Empty;
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", AppIdKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", AppNameKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", ActivePanelKey);
        }
        catch { }
        OnClinicChanged?.Invoke();
    }

    public async Task InitializeAsync()
    {
        try
        {
            var idStr  = await _js.InvokeAsync<string?>("localStorage.getItem", AppIdKey);
            var name   = await _js.InvokeAsync<string?>("localStorage.getItem", AppNameKey);
            var panel  = await _js.InvokeAsync<string?>("localStorage.getItem", ActivePanelKey);
            if (Guid.TryParse(idStr, out var id))
            {
                SelectedApplicationId   = id;
                SelectedApplicationName = name ?? string.Empty;
            }
            ActivePanel = panel ?? string.Empty;
        }
        catch { }
    }
}
