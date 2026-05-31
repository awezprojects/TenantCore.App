using Microsoft.JSInterop;

namespace TenantCore.Web.Client.Services;

public class ClinicContextService
{
    private readonly IJSRuntime _js;
    private const string AppIdKey   = "cc_selected_app_id";
    private const string AppNameKey = "cc_selected_app_name";

    public Guid   SelectedApplicationId   { get; private set; }
    public string SelectedApplicationName { get; private set; } = string.Empty;

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

    public async Task ClearClinicAsync()
    {
        SelectedApplicationId   = Guid.Empty;
        SelectedApplicationName = string.Empty;
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", AppIdKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", AppNameKey);
        }
        catch { }
        OnClinicChanged?.Invoke();
    }

    public async Task InitializeAsync()
    {
        try
        {
            var idStr = await _js.InvokeAsync<string?>("localStorage.getItem", AppIdKey);
            var name  = await _js.InvokeAsync<string?>("localStorage.getItem", AppNameKey);
            if (Guid.TryParse(idStr, out var id))
            {
                SelectedApplicationId   = id;
                SelectedApplicationName = name ?? string.Empty;
            }
        }
        catch { }
    }
}
