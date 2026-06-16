# ADR-007 — Blazor WebAssembly Client

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** `TenantCore.Web.Client`  
**Path:** `src/TenantCore.Web.Client/`

---

## Decision

The frontend is a **Blazor WebAssembly** (WASM) application hosted by and served from `TenantCore.Api`. It communicates with the backend exclusively through HTTP using typed HTTP clients. It shares DTO types via `TenantCore.Shared`.

---

## Hosting Model

`TenantCore.Api` hosts the Blazor WASM app via `Microsoft.AspNetCore.Components.WebAssembly.Server`. The SPA fallback is configured so all unmatched routes serve `index.html`:

```csharp
// Program.cs (TenantCore.Api)
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
// ... other middleware ...
app.MapFallbackToFile("index.html");
```

This means the Blazor app and the API share the same origin — no CORS issues in production.

---

## Folder Structure

```
TenantCore.Web.Client/
├── Clients/              # Typed HTTP clients for API communication
│   ├── PatientClient.cs
│   ├── PrescriptionClient.cs
│   └── ...
├── Components/           # Reusable Blazor components
│   ├── PatientCard.razor
│   └── ...
├── Pages/                # Blazor page components (routable)
│   ├── Patients/
│   │   ├── PatientList.razor
│   │   └── PatientDetail.razor
│   └── ...
├── Services/             # Client-side services (state, auth token handling)
├── Layout/               # App layout components (NavMenu, MainLayout)
├── wwwroot/              # Static assets (CSS, images, fonts)
├── App.razor             # Root component
└── Program.cs            # WASM bootstrap
```

---

## HTTP Client Pattern

All API communication uses **typed HTTP client classes** in the `Clients/` folder. They call `TenantCore.Api` endpoints and use DTOs from `TenantCore.Shared`.

```csharp
// Clients/PatientClient.cs
public class PatientClient
{
    private readonly HttpClient _http;

    public PatientClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id)
        => await _http.GetFromJsonAsync<PatientDto>($"api/patients/{id}");

    public async Task<Guid> CreateAsync(CreatePatientRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/patients", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
```

**Rules:**
- One client class per controller/feature area
- HTTP clients use `System.Net.Http.Json` extension methods (`GetFromJsonAsync`, `PostAsJsonAsync`, etc.)
- HTTP clients are registered in `Program.cs` and injected into components via DI
- Never hardcode base URLs — the base URL is configured via `HttpClient.BaseAddress` in registration

---

## Component Structure

```razor
@* Pages/Patients/PatientList.razor *@
@page "/patients"
@inject PatientClient PatientClient

<h3>Patients</h3>

@if (_patients is null)
{
    <p>Loading...</p>
}
else
{
    @foreach (var patient in _patients)
    {
        <PatientCard Patient="patient" />
    }
}

@code {
    private IEnumerable<PatientSummaryDto>? _patients;

    protected override async Task OnInitializedAsync()
    {
        _patients = await PatientClient.GetAllAsync();
    }
}
```

**Rules:**
- Pages (routable) go in `Pages/`, reusable components go in `Components/`
- Pages inject HTTP clients, not repositories or domain services
- Separate the markup and code-behind when the `@code` block grows beyond ~30 lines (use `.razor.cs` partial class)
- Use `StateHasChanged()` only when mutating state outside of Blazor's event cycle

---

## Authentication in the Client

The Blazor client receives a JWT from TenantCore.Auth (via TenantCore.Api's `/api/auth` endpoints) and stores it in browser storage. All subsequent API requests include it as a `Bearer` token in the `Authorization` header.

- Token storage: `localStorage` or `sessionStorage` (check implementation for current choice)
- The `HttpClient` base address points to the API origin (same origin = no CORS)
- Auth state is managed via a custom `AuthenticationStateProvider`

---

## Step-by-Step: Adding a New Page/Feature

1. Add a typed client method in `Clients/{Feature}Client.cs` (or create a new client if none exists)
2. Create the page component in `Pages/{Feature}/{FeatureName}.razor` with `@page "/route"`
3. Inject the typed client via `@inject`
4. Load data in `OnInitializedAsync` (for `AuthorizedLayout` pages — auth is already initialized by the layout)
5. Add navigation link to `Layout/NavMenu.razor`
6. If the feature needs its own reusable components, add them to `Components/{Feature}/`

**For print pages** — see the **Print Pages** section below. The lifecycle rules are fundamentally different: use `OnAfterRenderAsync(firstRender)` with explicit auth init, not `OnInitializedAsync`.

---

## Print Pages (Critical — Read Before Building Any Print Report)

Print pages open in a **new browser tab** and use `PrintLayout` instead of `AuthorizedLayout`.

### Why `OnInitializedAsync` Is Unsafe for Print Pages

`PrintLayout` is intentionally minimal — it renders only MudBlazor providers and `@Body`. It performs **no auth or clinic context initialization**. This creates a Blazor WASM race condition:

- `App.razor` calls `AuthState.InitializeAsync()` in its own `OnInitializedAsync`
- A child page's `OnInitializedAsync` can start **before** `App.razor`'s completes
- Result: the Bearer token is not yet set → every API call returns **401 Unauthorized**
- `AuthorizedLayout` avoids this because its `OnInitializedAsync` explicitly awaits both `AuthState` and `ClinicContext` before rendering children, but `PrintLayout` has no such gate

### The Rule: All Loading Goes in `OnAfterRenderAsync(firstRender)`

`OnAfterRenderAsync(firstRender)` is guaranteed to run **after the full component tree has rendered at least once**, by which point `App.razor`'s initialization is complete. JS interop (localStorage reads) is also only safe here.

**Mandatory sequence for every print page:**

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    try
    {
        await AuthState.InitializeAsync();      // 1. load Bearer token from localStorage
        await ClinicContext.InitializeAsync();  // 2. load X-Application-Id from localStorage
        await LoadData();                       // 3. now safe to call APIs
    }
    finally
    {
        _loading = false;
        StateHasChanged();
    }
}
```

**Never** make API calls in `OnInitializedAsync` on a print page — even if you think auth is already loaded.

### Required Injections for Every Print Page

```razor
@layout PrintLayout
@inject AuthStateService AuthState
@inject ClinicContextService ClinicContext
@inject IJSRuntime JS
@using TenantCore.Web.Client.Services
```

- `AuthStateService` — initializes the Bearer token
- `ClinicContextService` — initializes `X-Application-Id` (required by `ClinicAuthorizationHandler` on all clinic-scoped HTTP clients)
- `IJSRuntime JS` — for `window.print()` and `window.close()`

### Print Page Template

```razor
@page "/some-entity/{Id:guid}/print"
@layout PrintLayout
@inject IYourApiClient YourClient
@inject IClinicApiClient ClinicClient
@inject IApplicationApiClient AppClient
@inject ClinicContextService ClinicContext
@inject AuthStateService AuthState
@inject IJSRuntime JS
@using TenantCore.Shared.Dtos
@using TenantCore.Shared.Dtos.Auth
@using TenantCore.Web.Client.Clients
@using TenantCore.Web.Client.Services

@if (_loading)
{
    <div style="display:flex;justify-content:center;align-items:center;height:100vh;">
        <div>Loading...</div>
    </div>
}
else if (_data is null)
{
    <div style="display:flex;justify-content:center;align-items:center;height:100vh;flex-direction:column;gap:12px;">
        <div>Data not found.</div>
        <button @onclick='async () => await JS.InvokeVoidAsync("window.close")'
                style="padding:10px 20px;border-radius:8px;border:none;background:#EF4444;color:#fff;cursor:pointer;font-size:13px;font-weight:600;">
            ✕ Close
        </button>
    </div>
}
else
{
    <!-- Action bar (screen only, hidden on print) -->
    <div class="no-print" style="background:#1E3A5F;padding:10px 24px;display:flex;align-items:center;justify-content:space-between;gap:12px;position:sticky;top:0;z-index:100;">
        <div style="display:flex;align-items:center;gap:12px;">
            <button @onclick='async () => await JS.InvokeVoidAsync("window.close")'
                    style="padding:7px 16px;border-radius:8px;border:none;background:#EF4444;color:#fff;cursor:pointer;font-size:12px;font-weight:700;">
                ✕ Close
            </button>
        </div>
        <button @onclick="PrintPage"
                style="padding:9px 22px;border-radius:8px;border:none;background:#fff;color:#1E3A5F;cursor:pointer;font-size:13px;font-weight:800;">
            🖨️ Print
        </button>
    </div>

    <!-- Use rx-* CSS classes from PrescriptionPrint.razor / PrintUsgChart.razor -->
    <div class="rx-bg">
        <div class="rx-sheet">
            <!-- content -->
        </div>
    </div>
}

@code {
    [Parameter] public Guid Id { get; set; }

    private bool _loading = true;
    private YourDto? _data;
    private ApplicationResponseDto? _clinic;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            await AuthState.InitializeAsync();
            await ClinicContext.InitializeAsync();
            await Task.WhenAll(LoadData(), LoadClinic());
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task LoadData()
    {
        var r = await YourClient.GetByIdAsync(Id);
        if (r.Success) _data = r.Data;
    }

    private async Task LoadClinic()
    {
        var appId = ClinicContext.SelectedApplicationId;
        if (appId == Guid.Empty) return;
        var r = await AppClient.GetApplicationByIdAsync(appId);
        if (r.Success && r.Data is not null) _clinic = r.Data;
    }

    private async Task PrintPage() => await JS.InvokeVoidAsync("window.print");
}
```

### Opening a Print Page From Another Page

Print pages open in a new tab. Use JS `window.open` — **never** `Navigation.NavigateTo`:

```razor
@inject IJSRuntime JS

<!-- As a button -->
<button @onclick='async () => await JS.InvokeVoidAsync("window.open", $"/entity/{Id}/print", "_blank")'>
    Print
</button>

<!-- As a link -->
<a href="/entity/@Id/print" target="_blank">Print</a>
```

### Close Button (Not Back Button)

Print pages opened in a new tab must **close** the tab, not navigate. Never use a "← Back" button on a print page.

```razor
<!-- In the action bar -->
<button @onclick='async () => await JS.InvokeVoidAsync("window.close")'
        style="...;background:#EF4444;color:#fff;...">
    ✕ Close
</button>
```

Use the same red `#EF4444` close button in both the action bar and the error state.

### Visual Theme

All print pages use the `rx-*` CSS class system. Copy the `<style>` block from `PrescriptionPrint.razor` or `PrintUsgChart.razor` — do not invent a new print stylesheet. The classes cover layout (`.rx-bg`, `.rx-sheet`), header (`.rx-header`, `.rx-clinic-name`), patient strip (`.rx-patient-strip`), tables (`.rx-table`), signature (`.rx-signature`), disclaimer (`.rx-disclaimer`), and screen-only footer (`.rx-screen-footer`). The `@@media print` block at the bottom hides `.no-print` elements and resets backgrounds.

---

## What NOT to Do

- Do NOT reference `TenantCore.Api`, `TenantCore.Application`, `TenantCore.Infrastructure`, or `TenantCore.Domain` from this project
- Do NOT call EF Core, MediatR, or repositories from client code
- Do NOT hardcode JWT tokens or API URLs
- Do NOT put business logic in Blazor components — keep them presentation-only
- Do NOT make API calls in `OnInitializedAsync` on print pages — always use `OnAfterRenderAsync(firstRender)` with explicit auth init first
- Do NOT use `Navigation.NavigateTo` to open print pages — use `JS.InvokeVoidAsync("window.open", url, "_blank")`
- Do NOT put a "← Back" button on print pages — use a red "✕ Close" button that calls `window.close()`
