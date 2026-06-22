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

## UI Theme System (Critical — Read Before Building Any Page)

TenantCore.App uses **two distinct visual themes**. Using the wrong theme for a given page is a bug.

### Theme 1 — OPD Theme (custom CSS, `opd-*` classes)

**Use for:** All clinical and counter management pages — OPD, Expenses, Counter Session, Amount Handovers, and any future management/list pages.

The OPD theme is defined in `wwwroot/css/app.css` using the `opd-` prefix. These classes produce a clean, card-based layout with a white sidebar, slate typography, and color-coded status badges.

#### Required page shell

```razor
<div class="opd-page">

    <div class="opd-header">
        <div>
            <div class="opd-title">Page Title</div>
            <div class="opd-subtitle">Section &middot; @DateTime.Now.ToString("dddd, dd MMMM yyyy")</div>
        </div>
        <button class="opd-btn-primary">+ Add Something</button>
    </div>

    <!-- optional filter bar -->
    <div class="opd-filter-bar">
        <input class="opd-search-input" placeholder="Search..." @bind="_search" />
        <button class="opd-btn">↻ Refresh</button>
    </div>

    <!-- optional stats strip -->
    <div class="opd-stats">
        <div class="opd-stat-card done">
            <div class="opd-stat-label">Collected</div>
            <div class="opd-stat-icon">💰</div>
            <div class="opd-stat-value">Rs. 5,000</div>
        </div>
    </div>

    <div class="opd-table-card">
        <table class="opd-table">
            <thead><tr><th>Column</th></tr></thead>
            <tbody>
                <tr><td>Data</td></tr>
                <tr><td colspan="N" class="opd-empty">No records found.</td></tr>
            </tbody>
        </table>
    </div>

</div>
```

#### Available CSS classes

| Class | Purpose |
|-------|---------|
| `opd-page` | Outer page wrapper — max-width, padding |
| `opd-header` | Title row — flex, space-between, aligns title + action button |
| `opd-title` | Large bold page heading |
| `opd-subtitle` | Muted subtitle line (breadcrumb / date) |
| `opd-filter-bar` | Horizontal filter strip below the header |
| `opd-search-input` | Text input styled for the filter bar |
| `opd-stats` | Flex row of stat cards |
| `opd-stat-card` | Individual stat card — add modifier `done`, `waiting` for color tints |
| `opd-stat-label` | Small uppercase label inside stat card |
| `opd-stat-icon` | Emoji/icon inside stat card |
| `opd-stat-value` | Large number inside stat card |
| `opd-table-card` | White card wrapper around a table (border-radius, shadow) |
| `opd-table` | Styled `<table>` — full-width, row hover, `<th>` with slate bg |
| `opd-empty` | `<td>` colspan cell for empty-state rows |
| `opd-btn` | Secondary/outline button |
| `opd-btn-primary` | Primary filled button |
| `opd-btn start` | Green accent button (use with space, not dash: `class="opd-btn start"`) |
| `opd-tab` | Tab strip button |

#### Status badges — use inline `<span>` with explicit styles

**Never use `RenderFragment` return types for status chips/badges.** This pattern is fragile in Blazor switch expressions and causes phantom error messages to appear after successful operations. Use one of the two safe alternatives:

**Option A — inline in the template (preferred for simple status):**
```razor
@if (item.Status == MyStatus.Active)
{
    <span style="background:#DCFCE7;color:#166534;border-radius:12px;padding:2px 10px;font-size:11px;font-weight:600;">Active</span>
}
else
{
    <span style="background:#FEE2E2;color:#991B1B;border-radius:12px;padding:2px 10px;font-size:11px;font-weight:600;">Inactive</span>
}
```

**Option B — `static MarkupString` helper (for reuse in multiple rows):**
```csharp
private static MarkupString StatusBadge(MyStatus status) => status switch
{
    MyStatus.Active   => new MarkupString("<span style=\"background:#DCFCE7;color:#166534;border-radius:12px;padding:2px 10px;font-size:11px;font-weight:600;\">Active</span>"),
    MyStatus.Inactive => new MarkupString("<span style=\"background:#FEE2E2;color:#991B1B;border-radius:12px;padding:2px 10px;font-size:11px;font-weight:600;\">Inactive</span>"),
    _                 => new MarkupString("<span style=\"background:#F1F5F9;color:#475569;border-radius:12px;padding:2px 10px;font-size:11px;font-weight:600;\">Unknown</span>")
};
```

Then call it in the template as `@StatusBadge(item.Status)`.

#### Standard status color palette

| Meaning | Background | Text | Usage |
|---------|-----------|------|-------|
| Success / Paid / Active / Accepted | `#DCFCE7` | `#166534` | Positive states |
| Pending / Partial / Warning | `#FEF3C7` | `#92400E` | In-progress / attention needed |
| Error / Unpaid / Disputed / Danger | `#FEE2E2` | `#991B1B` | Problem states |
| Neutral / Closed / Inactive | `#F1F5F9` | `#475569` | Archived/closed states |
| Amounts in red (expenses) | color `#DC2626` | — | Outgoing money |
| Amounts in green (collected) | color `#16A34A` | — | Incoming money |

#### Dialogs within OPD pages

Forms and dialogs on OPD pages still use MudBlazor dialog components (`<MudDialog>`, `<MudTextField>`, `<MudSelect>`, `<MudNumericField>`) — that is intentional. Only the **page layout and tables** use the OPD custom CSS. Do not replace MudBlazor form inputs with raw HTML inputs.

```razor
<!-- Correct: OPD page layout + MudBlazor dialog -->
<div class="opd-page">
    <div class="opd-header">...</div>
    <div class="opd-table-card"><table class="opd-table">...</table></div>
</div>

<MudDialog @bind-Visible="_addVisible" Options="@(new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true })">
    <TitleContent><MudText Typo="Typo.h6">Add Item</MudText></TitleContent>
    <DialogContent>
        <MudStack Spacing="3">
            <MudTextField @bind-Value="_name" Label="Name *" Variant="Variant.Outlined" />
            <MudNumericField @bind-Value="_amount" Label="Amount *" Min="0.01m" Variant="Variant.Outlined" />
        </MudStack>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="@(() => _addVisible = false)">Cancel</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="SaveAsync">Save</MudButton>
    </DialogActions>
</MudDialog>
```

### Theme 2 — MudBlazor Material Design

**Use for:** Auth pages (login, register), dashboard/home pages, and any page that does **not** deal with clinical or counter workflows.

Do not use `<MudTable>`, `<MudDataGrid>`, MudBlazor layout components, or Material Design cards on OPD/counter management pages. Those are Theme 2 only.

### Theme decision rule

> **Is this page a management/list/detail page for clinical or counter data?** → OPD theme (`opd-*` classes).  
> **Is this an auth flow, dashboard, or admin settings page?** → MudBlazor Material Design.

---

## DateTime Handling (Critical — UTC Storage + Local Display)

All timestamps in this application are stored in the database as UTC (via `DateTime.UtcNow`). Two problems arise if this is ignored:

1. **Wrong display** — raw UTC shown to users in India shows times 5:30 hours behind local IST
2. **Wrong filtering** — a date filter like "Today (June 22 IST)" expressed as `yyyy-MM-dd` covers June 22 UTC midnight to midnight, but a record created at 1:16 AM IST (= 7:46 PM June 21 UTC) is silently excluded

### Rule 1 — Translators must stamp `DateTimeKind.Utc` on all UTC fields

EF Core reads `datetime2` columns back as `Kind.Unspecified`. If left unchanged, `System.Text.Json` omits the `Z` suffix from the JSON, and the client cannot tell if the value is UTC or local.

Every translator that maps a UTC-stored field MUST call `DateTime.SpecifyKind`:

```csharp
// In every translator ToDto / ToSummaryDto:
RecordedAt  = DateTime.SpecifyKind(entity.RecordedAt,  DateTimeKind.Utc),
HandedOverAt = DateTime.SpecifyKind(entity.HandedOverAt, DateTimeKind.Utc),
OpenedAt    = DateTime.SpecifyKind(entity.OpenedAt,    DateTimeKind.Utc),
// Nullable:
ClosedAt    = entity.ClosedAt.HasValue
    ? DateTime.SpecifyKind(entity.ClosedAt.Value, DateTimeKind.Utc)
    : null,
```

**Do NOT stamp fields that are LOCAL dates** (e.g., `CounterSession.SessionDate` which is sent from the browser as `DateTime.Today`).

### Rule 2 — Display ALWAYS goes through `DateTimeHelper`

Add `@using TenantCore.Web.Client.Helpers` and call `DateTimeHelper.ToLocalString(utcDateTime)`.  
Never call `.ToString(...)` directly on a UTC-stored datetime.

```razor
@* Wrong: *@
<td>@item.RecordedAt.ToString("dd MMM HH:mm")</td>

@* Correct: *@
<td>@DateTimeHelper.ToLocalString(item.RecordedAt, "dd MMM HH:mm")</td>
```

`DateTimeHelper` always calls `DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime()` so the result uses the browser's timezone regardless of what `Kind` the DTO carries.

### Rule 3 — Session / local dates use `DateTime.Today`, never `DateTime.UtcNow.Date`

When the client needs to record "today's date" as a local calendar date (e.g., `CounterSession.SessionDate`), always use:

```csharp
DateTime.Today  // ✓ — browser local date (IST June 22 at 1 AM IST = June 22)
DateTime.UtcNow.Date  // ✗ — UTC date (June 21 at 1 AM IST → UTC June 21)
```

### Rule 4 — Date-range filters over UTC timestamps must send `utcOffset`

When the API filters records by a user-selected date range where the underlying column stores UTC, the server must know the user's timezone to compute the correct UTC boundaries.

**Client API clients** send `utcOffset` (minutes) alongside `yyyy-MM-dd` dates:

```csharp
qs.Add($"from={from.Value:yyyy-MM-dd}");
qs.Add($"to={to.Value:yyyy-MM-dd}");
qs.Add($"utcOffset={(int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes}");
```

**Query record** carries the offset:

```csharp
public sealed record GetExpenseRecordsQuery(
    Guid ApplicationId, DateTime? From, DateTime? To, int UtcOffsetMinutes = 0)
    : IRequest<...>;
```

**Handler** converts to UTC range before in-memory filtering:

```csharp
var offsetMinutes = request.UtcOffsetMinutes;          // e.g. 330 for IST
DateTime? fromUtc = request.From.HasValue
    ? request.From.Value.Date.AddMinutes(-offsetMinutes)   // midnight IST → UTC
    : null;
DateTime? toUtc = request.To.HasValue
    ? request.To.Value.Date.AddDays(1).AddMinutes(-offsetMinutes)  // next midnight IST → UTC
    : null;

return all
    .Where(e => e.ApplicationId == request.ApplicationId
        && (fromUtc == null || e.RecordedAt >= fromUtc.Value)
        && (toUtc   == null || e.RecordedAt <  toUtc.Value))
```

**Do NOT apply this to `CounterSession` history filtering** — `SessionDate` is a local DATE (not a UTC timestamp), so `yyyy-MM-dd` direct comparison is correct and no UTC offset is needed.

---

## What NOT to Do

- Do NOT reference `TenantCore.Api`, `TenantCore.Application`, `TenantCore.Infrastructure`, or `TenantCore.Domain` from this project
- Do NOT call EF Core, MediatR, or repositories from client code
- Do NOT hardcode JWT tokens or API URLs
- Do NOT put business logic in Blazor components — keep them presentation-only
- Do NOT make API calls in `OnInitializedAsync` on print pages — always use `OnAfterRenderAsync(firstRender)` with explicit auth init first
- Do NOT use `Navigation.NavigateTo` to open print pages — use `JS.InvokeVoidAsync("window.open", url, "_blank")`
- Do NOT put a "← Back" button on print pages — use a red "✕ Close" button that calls `window.close()`
- Do NOT use `<MudTable>`, `<MudDataGrid>`, or MudBlazor card/layout components on OPD/counter pages — use `opd-table-card` + `opd-table` instead
- Do NOT return `RenderFragment` from a method to render status chips — use inline `@if`/`@else` blocks or a `static MarkupString` helper instead
- Do NOT call `.ToString(...)` directly on UTC-stored datetimes — always use `DateTimeHelper.ToLocalString()`
- Do NOT use `DateTime.UtcNow.Date` to record "today" on the client — use `DateTime.Today`
- Do NOT filter UTC-timestamp columns with raw `yyyy-MM-dd` local dates without the `utcOffset` conversion
