# Feature Plan: Remove MudBlazor

**Repo:** TenantCore.App
**Date:** 2026-06-22
**Domain area:** Blazor Client — UI Theme Refactor
**Status:** Approved — ready for execution

---

## Overview

This refactor removes MudBlazor v7.15.0 entirely from `TenantCore.Web.Client` and replaces every MudBlazor component reference with the project's existing custom CSS theme (`opd-*` classes, `cc-*` classes, `auth-*` classes, and inline styles). A lightweight `IToastService` replaces `ISnackbar` across all 34 files that currently inject it. `OpdDiscountDialog` and `OpdParticularsPopup` are converted from `MudDialog` to the same custom modal pattern already used by `CloseTenureModal` and `PatientTenureHistoryDialog`. `AuthorizedLayout` is rebuilt without `MudLayout`/`MudDrawer`/`MudAppBar`, using the same pure-CSS collapsible sidebar pattern already in `DoctorPortalLayout`. All action buttons across the entire app are standardised as text-labelled `<button>` elements — no icon-only buttons. ADR-007 is updated to reflect the removal of the "MudBlazor for dialogs" rule and the elimination of Theme 2.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Blazor Client — Services | New IToastService + ToastService |
| Blazor Client — Components | New ToastContainer; rebuild OpdDiscountDialog, OpdParticularsPopup |
| Blazor Client — Layouts | Rebuild AuthorizedLayout, DoctorPortalLayout, AuthLayout, PrintLayout, AuthInvitationLayout |
| Blazor Client — Pages | 30+ pages — remove MudBlazor, apply opd-*/auth-* CSS, swap ISnackbar |
| Blazor Client — wwwroot | Remove MudBlazor CSS/JS from index.html; add toast + auth-form CSS to app.css |
| Project file | Remove MudBlazor NuGet package |
| Program.cs | Remove AddMudServices; register IToastService |
| _Imports.razor | Remove @using MudBlazor |
| ADR-007 | Update UI theme section; remove MudBlazor dialog rule |
| Backend | No changes (zero impact on API, Application, Infrastructure, Domain) |

---

## No EF Migration Required

This refactor is purely client-side. No entities, DbSets, repositories, or API endpoints change.

---

## New Files to Create

| File | Purpose |
|------|---------|
| `src/TenantCore.Web.Client/Services/ToastService.cs` | Defines `IToastService` interface with `ShowSuccess`, `ShowError`, `ShowWarning` methods, plus `ToastService` implementation and `ToastLevel` enum. Exposes an `OnToast` event that `ToastContainer` subscribes to. Registered as `Singleton` in `Program.cs`. |
| `src/TenantCore.Web.Client/Components/ToastContainer.razor` | Subscribes to `IToastService.OnToast`. Maintains a timed list of active toast messages and renders them as fixed-position overlay items at bottom-right. Each toast auto-dismisses after 3 seconds. Colour-coded by level: success = green (#DCFCE7 / #166534), error = red (#FEE2E2 / #991B1B), warning = amber (#FEF3C7 / #92400E). Must be placed once inside every layout that wraps authenticated or interactive pages (AuthorizedLayout, DoctorPortalLayout, AuthLayout). No MudBlazor dependency. |

---

## CSS Additions to `wwwroot/css/app.css`

Two new class groups must be appended to `app.css`. No MudBlazor override blocks should remain.

### Toast notification styles

Classes: `toast-container`, `toast-item`, `toast-item.toast-success`, `toast-item.toast-error`, `toast-item.toast-warning`. The container is `position:fixed; bottom:24px; right:24px; z-index:9999; display:flex; flex-direction:column; gap:8px`. Each item has `padding:12px 16px; border-radius:10px; font-size:13px; font-weight:600; min-width:260px; max-width:380px; box-shadow:0 4px 16px rgba(0,0,0,0.12); animation: toast-slide-in 0.2s ease`. Add `@keyframes toast-slide-in` for a subtle slide-up entry.

### Auth form input and button styles

Classes needed by auth pages to replace MudTextField/MudButton without inline styles everywhere:

| Class | Purpose |
|-------|---------|
| `auth-input` | Full-width text input: border 1.5px #E2E8F0, border-radius 8px, padding 11px 14px, font-size 14px, width 100%, outline:none. Focus border: #1a56db. |
| `auth-input-wrap` | Label + input wrapper: display flex, flex-direction column, gap 6px, margin-bottom 16px |
| `auth-label` | Input label: font-size 13px, font-weight 600, color #374151 |
| `auth-btn-primary` | Full-width submit button: background #1a56db, color #fff, border-radius 8px, padding 13px, font-size 15px, font-weight 700, border none, cursor pointer, width 100%, height 48px |
| `auth-btn-secondary` | Outline/secondary button: background #fff, color #1a56db, border 1.5px #1a56db, same sizing as primary |
| `auth-error` | Error alert box: background #FEF2F2, border 1.5px solid #FECACA, border-radius 8px, padding 10px 14px, color #B91C1C, font-size 13px, margin-bottom 16px |
| `auth-info` | Info alert box: same structure, background #EFF6FF, border #BFDBFE, color #1565C0 |
| `auth-success` | Success alert box: background #F0FDF4, border #BBF7D0, color #166534 |
| `auth-link` | Inline link: color #1a56db, font-weight 600, text-decoration none, cursor pointer |
| `auth-divider` | Horizontal rule: border-top 1px #E5E7EB, margin 20px 0 |
| `auth-spinner` | Small loading spinner via border + border-radius animation: 20px circle, border 2px solid #E2E8F0, border-top-color #1a56db, border-radius 50%, animation spin 0.7s linear infinite |

Also add `@keyframes spin` for the spinner.

Remove the existing `.mud-drawer-content` override (line ~561-567 in app.css) as it has no effect once MudBlazor is gone.

---

## Files to Modify — Grouped by Migration Complexity

---

### GROUP A — Infrastructure Removal (do last, after all components migrated)

| File | Change |
|------|--------|
| `TenantCore.Web.Client.csproj` | Remove `<PackageReference Include="MudBlazor" Version="7.15.0" />` |
| `Program.cs` | Remove `builder.Services.AddMudServices()`. Add `builder.Services.AddSingleton<IToastService, ToastService>()`. |
| `_Imports.razor` | Remove `@using MudBlazor` line |
| `wwwroot/index.html` | Remove `<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />` (line ~10). Remove `<script src="_content/MudBlazor/MudBlazor.min.js"></script>` (line ~39). |

---

### GROUP B — Layout Files

#### `Layout/PrintLayout.razor`

Current: 4 MudBlazor provider components + `@Body`.
Change: Remove all 4 providers (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`). Keep only `@Body`. Result: 1-line file.

#### `Layout/AuthLayout.razor`

Current: 4 providers at top. 3 `MudIcon` instances in the decorative right panel. One `_isDarkMode` field with `@bind-IsDarkMode`.
Changes:
- Remove all 4 providers.
- Replace the 3 `MudIcon` components in the `.auth-hub`, `.auth-orbit-1`, `.auth-orbit-2`, `.auth-orbit-3` divs with emoji: `🏥`, `👥`, `💊`, `📅` respectively. Use a `<span style="font-size:1.4rem;">` wrapper to match sizing.
- Remove the `_isDarkMode` field and the `@code` block entirely (it only held that field).
- Place `<ToastContainer />` inside the layout shell, below `@Body`.
- Add `@using TenantCore.Web.Client.Services` and `@inject IToastService _toast` if needed by ToastContainer injection pattern; otherwise ToastContainer self-injects.

#### `Layout/DoctorPortalLayout.razor`

Current: 4 providers at top + `MudThemeProvider Theme="_theme"`. `MudProgressCircular` for loading state. Private `MudTheme _theme = new() { ... }` field in `@code`.
Changes:
- Remove all 4 provider lines and `Theme="_theme"` binding.
- Remove the `private readonly MudTheme _theme = new() { ... }` field.
- Replace the loading `MudProgressCircular` with: `<div style="display:flex;align-items:center;justify-content:center;min-height:100vh;background:#F0F4F9;"><div class="auth-spinner"></div></div>`.
- Place `<ToastContainer />` after the outer layout div.

#### `Layout/AuthorizedLayout.razor`

Current: Uses `MudLayout`, `MudDrawer`, `MudAppBar`, `MudMainContent`, `MudContainer`, `MudProgressCircular`, `MudIconButton` (dark mode), `MudMenu`, `MudMenuItem`, `MudDivider`, `MudTheme`.
This is the most complex layout change. The sidebar *content* is already custom HTML/CSS; only the MudBlazor shell wrapping it needs to be replaced.

Target structure (follow DoctorPortalLayout pattern exactly):

- Remove all 4 MudBlazor provider lines.
- Remove `MudTheme _theme` field from `@code`.
- Remove `_isDarkMode` bool and `ToggleDarkMode` method.
- Loading state: replace `MudProgressCircular` with `<div class="auth-spinner">` inside a centering div.
- Outer layout: replace `<MudLayout>` with `<div style="display:flex;min-height:100vh;background:#F4F6FA;">`.
- Sidebar: replace `<MudDrawer @bind-Open="_drawerOpen" ...>` with `<div style="width:@(_drawerOpen ? "230px" : "68px");flex-shrink:0;background:#0A1628;display:flex;flex-direction:column;transition:width 0.2s ease;overflow:hidden;">`. Keep all existing sidebar inner content (logo, user info, NavMenu, bottom buttons) unchanged.
- Top AppBar: replace `<MudAppBar ...>` + `</MudAppBar>` with `<div style="background:#ffffff;border-bottom:1px solid #E2E8F0;padding:14px 24px;display:flex;align-items:center;justify-content:space-between;position:sticky;top:0;z-index:50;box-shadow:0 1px 4px rgba(0,0,0,0.04);">`.
- Inside the top bar: keep screen title and date/time div. Replace the right-side items:
  - Keep the clinic name badge and Switch Clinic button (already custom HTML).
  - Keep the "🟢 Online" badge.
  - Remove `MudIconButton` (dark mode toggle) entirely — no replacement needed.
  - Replace `MudMenu` / `MudMenuItem` / `MudDivider` user dropdown with a custom dropdown: a `<button>` showing `👤 Account ▾` that toggles `_userMenuOpen` bool, and a conditionally rendered `<div style="position:absolute;right:24px;top:58px;background:#fff;border:1px solid #E2E8F0;border-radius:10px;box-shadow:0 8px 24px rgba(0,0,0,0.1);min-width:180px;z-index:200;">` containing plain `<a>` links for "My Profile" and `<button>` for "Sign Out". Add `_userMenuOpen = false` default. Add click-outside close by resetting on any other interaction.
- Main content: replace `<MudMainContent>` + `<MudContainer MaxWidth="MaxWidth.ExtraExtraLarge" Class="pt-5 pb-6">` with `<div style="flex:1;padding:28px 32px;overflow-y:auto;">`.
- Place `<ToastContainer />` as the last element before the closing outer div.
- Add `_userMenuOpen` bool field to `@code`.

#### `Pages/Auth/AuthInvitationLayout.razor`

Current: `MudThemeProvider` + `MudContainer MaxWidth="MaxWidth.Small"` centring wrapper.
Change: Remove `MudThemeProvider`. Replace `MudContainer` with `<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:100vh;background:#F9FAFB;padding:24px;">`. Keep `@Body` inside.

---

### GROUP C — MudDialog Components → Custom Modals

Both components must follow the `CloseTenureModal.razor` pattern exactly: fixed backdrop div (`position:fixed;inset:0;background:rgba(0,0,0,0.45);z-index:900`) + fixed centred dialog div (`position:fixed;top:50%;left:50%;transform:translate(-50%,-50%);background:#fff;border-radius:14px;z-index:901`).

#### `Components/OpdDiscountDialog.razor`

Current: `MudDialog Visible="Visible"`, `MudText`, `MudNumericField @bind-Value="_discount"`, `MudButton` (Cancel + Apply).
Changes:
- Remove `MudDialog` wrapper. Replace with custom modal using `@if (Visible)` guard.
- Title: `<div style="font-size:16px;font-weight:800;color:#0F172A;margin-bottom:6px;">Apply Discount</div>`
- Payment summary: two custom `<div>` lines showing Total and Current Discount (conditional on `_payment is not null`).
- Replace `MudNumericField` with `<input type="number" @bind="_discount" min="0" step="0.01" style="width:100%;padding:9px 12px;border:1.5px solid #E2E8F0;border-radius:8px;font-size:13px;color:#0F172A;outline:none;" />` with a label.
- Error display: inline error div (same pattern as CloseTenureModal).
- Buttons: Cancel (`opd-btn` class or inline style matching CloseTenureModal) + Apply (`opd-btn-primary` style, disabled when `_applying`).
- Replace `ISnackbar Snackbar` injection with `IToastService Toast`. Replace `Snackbar.Add(...)` with `Toast.ShowSuccess(...)` / `Toast.ShowError(...)`.

#### `Components/OpdParticularsPopup.razor`

Current: `MudDialog Visible="Visible"`, `MudProgressLinear`, `MudAlert` (discount warning), complex content with multiple MudButton rows.
Changes:
- Remove `MudDialog` wrapper. Replace with `@if (Visible)` + backdrop + centred dialog div (use `MaxWidth.Medium` equivalent: `width:680px;max-width:96vw`).
- Title: `<div style="font-size:16px;font-weight:800;...">OPD Service Items</div>` + close button `×` top-right.
- Loading: custom spinner div instead of `MudProgressLinear`.
- Discount warning: replace `MudAlert` with a custom amber warning div (background:#FEF3C7, border:#FDE68A, color:#92400E, border-radius 8px, padding 10px 14px, margin-bottom 12px).
- Keep all functional logic unchanged — only markup changes.
- All existing `<button>` / styled buttons inside the popup content may remain if already custom; only MudButton and MudAlert instances need replacement.
- Replace `ISnackbar` with `IToastService`. Replace all `Snackbar.Add(...)` calls.

---

### GROUP D — Auth Pages

All 9 auth pages and the Profile page use `AuthLayout` or `AuthorizedLayout`. The `auth-*` CSS (already in AuthLayout's `<style>` block) plus the new `auth-input`, `auth-btn-primary`, `auth-error`, etc. classes (added to app.css) cover all replacements. The `@code` block logic is unchanged in every case — only markup changes.

Replacement mapping for every auth page:

| MudBlazor | Replace with |
|-----------|-------------|
| `<MudAvatar>` + `<MudIcon>` in brand row | `<div class="auth-brand"><span style="font-size:22px;">🏥</span> <span style="font-weight:700;color:#1a56db;">ClinicCore</span></div>` |
| `<MudText Typo="Typo.h5">` | `<h2 style="font-size:22px;font-weight:700;color:#111827;margin-bottom:4px;">` |
| `<MudText Typo="Typo.body2">` | `<p style="color:#6B7280;margin-bottom:1.5rem;font-size:14px;">` |
| `<MudTextField @bind-Value="..." Label="..." Variant="Variant.Outlined">` | `<div class="auth-input-wrap"><label class="auth-label">Label</label><input class="auth-input" @bind="..." type="..." /></div>` |
| `<MudStack Row="true" Justify="Justify.FlexEnd">` (forgot password link row) | `<div style="text-align:right;margin-bottom:16px;">` |
| `<MudLink Href="..." Color="...">` | `<a href="..." class="auth-link">` |
| `<MudAlert Severity="Severity.Error">` | `<div class="auth-error">@_errorMessage</div>` |
| `<MudAlert Severity="Severity.Success">` | `<div class="auth-success">...message...</div>` |
| `<MudAlert Severity="Severity.Info">` | `<div class="auth-info">...message...</div>` |
| `<MudButton ButtonType="ButtonType.Submit" Variant="Filled" Color="Primary" FullWidth>` | `<button type="submit" class="auth-btn-primary" disabled="@_loading">` |
| `<MudButton ... OnClick="...">` (non-submit) | `<button class="auth-btn-primary" @onclick="..." disabled="@_loading">` |
| `<MudProgressCircular>` inside button | `<span class="auth-spinner" style="width:18px;height:18px;margin-right:8px;"></span>` |
| `<MudDivider>` | `<hr class="auth-divider" />` |
| `<MudStack AlignItems.Center>` | `<div style="text-align:center;">` |
| `<MudPaper>` (AcceptInvitation card) | `<div style="background:#fff;border-radius:12px;padding:32px;border:1px solid #E2E8F0;width:400px;max-width:100%;">` |
| `<MudForm @ref="_form">` | Remove MudForm; keep `<EditForm>` or use Blazor's built-in `DataAnnotationsValidator` |
| `<MudGrid>` / `<MudItem>` (Register 2-col) | `<div style="display:grid;grid-template-columns:1fr 1fr;gap:14px;">` |
| `<MudPaper Class="mt-3">` (TwoFactor QR card) | `<div style="background:#F8FAFC;border:1px solid #E2E8F0;border-radius:10px;padding:20px;text-align:center;">` |
| `<MudContainer>` (AcceptExistingInvitation) | Remove; the AuthInvitationLayout already centres the body |
| `<MudProgressLinear>` (profile password strength) | Custom div `<div style="height:4px;border-radius:2px;background:#E2E8F0;margin-top:4px;"><div style="height:100%;background:...(colour by strength);border-radius:2px;transition:width 0.3s;width:@(_pwdStrength)%;"></div></div>` |

**Password show/hide toggle:** The current `MudTextField` with `AdornmentIcon` handles the eye icon. Replacement: wrap the `<input>` in a `<div style="position:relative;">`, add a `<button type="button" @onclick="TogglePassword" style="position:absolute;right:12px;top:50%;transform:translateY(-50%);background:none;border:none;cursor:pointer;font-size:13px;color:#6B7280;">` showing "Show" / "Hide" text (not icon).

#### Files in Group D

| File | Notes |
|------|-------|
| `Pages/Auth/Login.razor` | Password show/hide toggle; MudAvatar brand; error alert; spinner in button |
| `Pages/Auth/Register.razor` | 2-col grid for name fields; MudGrid → CSS grid |
| `Pages/Auth/ForgotPassword.razor` | Success state; conditional show/hide sections |
| `Pages/Auth/ResetPassword.razor` | Password strength progress; 3 password fields |
| `Pages/Auth/TwoFactor.razor` | QR code card (MudPaper → custom div); MudAvatar |
| `Pages/Auth/VerifyEmail.razor` | Verifying / verified / error states via @if |
| `Pages/Auth/ResendVerification.razor` | Success state replace |
| `Pages/Auth/AcceptInvitation.razor` | MudPaper card → custom card div; MudForm → EditForm or bare inputs |
| `Pages/Auth/AcceptExistingInvitation.razor` | MudContainer → remove (AuthInvitationLayout centres); MudPaper → custom div |

#### `Pages/Auth/Profile.razor`

Profile is on `AuthorizedLayout` and uses MudTextField for editing personal info, plus inline MudDialog-like markup for the password change and 2FA sections. It already uses mostly custom CSS for the card structure.
Changes:
- Replace `MudProgressCircular` loading state with custom spinner div.
- Replace `MudTextField` instances in the edit form with native `<input>` elements using opd-form input styles (same pattern as Settings pages — see Group G).
- For the "Change Password" section: if it currently opens a MudDialog inline, convert to the CloseTenureModal custom modal pattern.
- For the "2FA" section: same — if using MudDialog, convert to custom modal.
- Replace `ISnackbar` with `IToastService`.
- The existing card structure (custom HTML divs, badges, Edit Profile button) is unchanged.

---

### GROUP E — Admin Pages Still on MudBlazor Layout

These two pages use `MudTable`, `MudStack`, `MudText`, `MudButton`, `MudProgressLinear` for their entire layout. They also contain inline MudDialog for the Add/Edit forms.

#### `Pages/Admin/Particulars.razor`

Changes:
- Replace header: `<MudStack Row Justify.SpaceBetween>` + `<MudText Typo.h4>` + `<MudButton StartIcon="...">` → `<div class="opd-header"><div><div class="opd-title">Particulars (Service Items)</div><div class="opd-subtitle">Settings · Service billing items</div></div><button class="opd-btn-primary" @onclick="OpenCreateDialog">+ Add Particular</button></div>`.
- Replace loading state `<MudProgressLinear>` with `<div class="opd-empty">Loading...</div>` inside the table card.
- Replace `<MudTable Items="_items" ...>` with `<div class="opd-table-card"><table class="opd-table"><thead>...</thead><tbody>...</tbody></table></div>`.
- Table header `<MudTh>` → `<th>`. Body template `<MudTd>` → `<td>`.
- Row action buttons: any icon-only buttons (e.g., Edit pencil icon, Delete trash icon) → text buttons: `<button class="opd-btn" @onclick="...">Edit</button>` `<button style="...red..." @onclick="...">Delete</button>`.
- Inline `<MudDialog>` (Add/Edit form) → custom modal following CloseTenureModal pattern. Fields inside use native `<input>` / `<select>` with inline-style inputs.
- Replace `ISnackbar` with `IToastService`.
- Wrap page in `<div class="opd-page">`.

#### `Pages/Admin/ExpenseCategories.razor`

Same migration as Particulars:
- Replace `<MudStack>` header → `opd-header` pattern with `+ Add Category` text button.
- Replace `<MudProgressLinear>` → loading empty state.
- Replace `<MudTable>` → `opd-table-card` + `opd-table`. Actions: text buttons (Edit, Activate/Deactivate) — not icons.
- Convert inline MudDialog to custom modal.
- Replace `ISnackbar` with `IToastService`.
- Wrap in `<div class="opd-page">`.

---

### GROUP F — Settings Pages

These pages use `MudGrid`, `MudItem`, `MudPaper`, `MudTextField`, `MudSelect`, `MudProgressLinear`, `MudAlert`. They are form pages, not table/list pages.

Standard migration approach for settings pages:
- Replace `<MudText Typo.h5>` page title → `<div class="opd-title" style="margin-bottom:4px;">...</div>`.
- Replace `<MudText Typo.body2>` subtitle → `<div class="opd-subtitle" style="margin-bottom:20px;">...</div>`.
- Replace `<MudProgressLinear>` → `<div style="height:4px;background:#E2E8F0;border-radius:2px;overflow:hidden;margin-bottom:16px;"><div style="height:100%;background:#1565C0;width:100%;animation:progress-indeterminate 1.2s ease infinite;"></div></div>`. Add `@keyframes progress-indeterminate` to app.css (or use the auth-spinner approach).
- Replace `<MudGrid Spacing="4">` → `<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:20px;">`.
- Replace `<MudItem xs="12" md="8">` → `<div>` (CSS grid handles sizing).
- Replace `<MudPaper Elevation="1">` → `<div style="background:#fff;border-radius:12px;border:1px solid #E2E8F0;padding:24px;">`.
- Replace `<MudTextField @bind-Value="..." Label="..." Variant="Variant.Outlined">` → custom labelled input (same as auth-input but using opd-page context styles).
- Replace `<MudSelect T="..." @bind-Value="...">` → native `<select @bind="...">` with inline styles.
- Replace `<MudAlert Severity.Warning>` → `<div style="background:#FEF3C7;border:1.5px solid #FDE68A;border-radius:8px;padding:10px 14px;color:#92400E;font-size:13px;margin-bottom:16px;">`.
- Replace `ISnackbar` with `IToastService`.
- Save/cancel buttons: replace `<MudButton>` with `<button class="opd-btn-primary">` / `<button class="opd-btn">`.

#### Files in Group F

| File | Notes |
|------|-------|
| `Pages/Settings/ClinicProfile.razor` | MudGrid 2-col form; MudTextField x8; Save button; warning alert for no clinic |
| `Pages/Settings/DosageRemarkSettings.razor` | MudGrid split panel (form left, list right); MudSelect for medicine form type; MudTextField x3 (multi-lang remarks); MudTable for remark list → opd-table-card |
| `Pages/Settings/PrescriptionSettings.razor` | MudGrid multi-section; MudSelect for language/print settings; save button per section |

---

### GROUP G — Pages with Light MudBlazor Use (ISnackbar + Spinner Only)

For each file in this group, the only changes are:
1. Replace `@inject ISnackbar Snackbar` with `@inject IToastService Toast`
2. Replace `Snackbar.Add("msg", Severity.Success)` → `Toast.ShowSuccess("msg")`
3. Replace `Snackbar.Add("msg", Severity.Error)` / `Snackbar.Add(response.Message, Severity.Error)` → `Toast.ShowError("msg")`
4. Replace `Snackbar.Add("msg", Severity.Warning)` → `Toast.ShowWarning("msg")`
5. Replace `Snackbar.Add("msg")` (no severity) → `Toast.ShowSuccess("msg")`
6. Replace any `<MudProgressCircular ... />` with `<div style="display:flex;justify-content:center;align-items:center;padding:80px 0;"><div class="auth-spinner" style="width:40px;height:40px;border-width:3px;"></div></div>`
7. Replace any `<MudProgressLinear ... />` with `<div style="height:4px;background:#BFDBFE;border-radius:2px;overflow:hidden;margin-bottom:12px;"><div style="height:100%;background:#1565C0;width:100%;animation:progress-indeterminate 1.2s ease infinite;"></div></div>`
8. Replace any remaining `<MudButton>` / `<MudChip>` / `<MudIconButton>` with styled `<button>` elements (text-labelled, no icon-only)

#### Files in Group G

| File | Mud components beyond ISnackbar |
|------|--------------------------------|
| `Pages/Dashboard.razor` | `MudProgressCircular` (loading); 1× `MudButton` with `StartIcon` — replace with `<button class="opd-btn-primary">+ New Prescription</button>` |
| `Pages/Admin/AdminDashboard.razor` | `MudProgressCircular` (loading) only |
| `Pages/Admin/UserManagement.razor` | `MudProgressCircular` (loading) |
| `Pages/Admin/WardManagement.razor` | `MudProgressCircular` (loading) |
| `Pages/Admin/DoctorFeeConfigs.razor` | ISnackbar only (already opd-theme) |
| `Pages/Admin/FinanceDashboard.razor` | ISnackbar only (already opd-theme) |
| `Pages/Opd/OpdRegistrationList.razor` | `MudProgressLinear` (loading) |
| `Pages/Prescriptions/PrescriptionForm.razor` | `MudProgressCircular` (loading); 1× `MudButton StartIcon=Print` → `<button class="opd-btn">Print</button>`; `MudChip` (medicine tags) → `<span style="display:inline-flex;align-items:center;gap:4px;background:#EFF6FF;color:#1565C0;border-radius:20px;padding:3px 10px;font-size:12px;font-weight:600;">` |
| `Pages/Prescriptions/PrescriptionList.razor` | ISnackbar only |
| `Pages/Counter/CounterSession.razor` | ISnackbar only |
| `Pages/Counter/ExpenseRecords.razor` | ISnackbar only |
| `Pages/Counter/AmountHandovers.razor` | ISnackbar only |
| `Pages/Patients/PatientList.razor` | ISnackbar only |
| `Pages/Patients/OverdueEddPatients.razor` | ISnackbar only |
| `Pages/Ipd/IpdRegistrationList.razor` | ISnackbar only |
| `Pages/Medicines/MedicineList.razor` | ISnackbar only |
| `Pages/Doctor/DoctorProfile.razor` | `MudProgressCircular` (loading) |
| `Pages/Doctor/DoctorRegisterClinic.razor` | ISnackbar + possibly MudTextField (check during execution) |
| `Pages/Doctor/DoctorChangePassword.razor` | ISnackbar + possibly MudTextField (check during execution) |
| `Components/CloseTenureModal.razor` | ISnackbar only (modal itself is already custom HTML) |
| `Components/PatientTenureHistoryDialog.razor` | `MudProgressLinear` (loading) only |

---

### GROUP H — App Root and Error Page

#### `App.razor`

The `<NotFound>` block uses `MudAvatar`, `MudIcon`, `MudText`, `MudStack`, `MudButton`.
Replace the entire `<NotFound>` block content with a simple custom 404 layout:
- Brand: `<div class="auth-brand"><span style="font-size:22px;">🏥</span> <span style="font-weight:700;color:#1a56db;">ClinicCore</span></div>`
- Icon: `<div style="font-size:80px;text-align:center;margin-bottom:16px;">🔍</div>`
- Heading: `<h2 style="font-size:22px;font-weight:700;color:#111827;margin-bottom:4px;text-align:center;">Page Not Found</h2>`
- Body text: `<p style="color:#6B7280;margin-bottom:2rem;text-align:center;">The page you're looking for doesn't exist or has been moved.</p>`
- Button: `<a href="/select-clinic" class="auth-btn-primary" style="display:block;text-align:center;text-decoration:none;">Go to Home</a>`

#### `Pages/Error.razor`

Same approach: replace MudAvatar, MudIcon, MudText, MudStack, MudButton with plain HTML using auth-* classes for the 500 error state.

---

### GROUP I — ADR Update

#### `.claude/docs/adr/ADR-007-blazor-client.md`

Update the **UI Theme System** section (currently lines ~343–593):

1. Remove the "Dialogs within OPD pages" sub-section (lines ~454–476 that say "Forms and dialogs on OPD pages still use MudBlazor dialog components... that is intentional").

2. Replace with: All dialogs and popups use the custom modal pattern. The standard is: conditional `@if (Visible)` guard → fixed backdrop div → fixed centred dialog div → custom `<input>` / `<select>` / `<button>` elements → error div for validation messages. The `CloseTenureModal.razor` is the canonical reference for this pattern.

3. Remove the entire "Theme 2 — MudBlazor Material Design" sub-section (lines ~479–483 that say "Use for: Auth pages, dashboard/home pages...").

4. Replace the Theme decision rule with: All pages use the OPD theme (`opd-*` classes) for management/list pages, `cc-*` classes for dashboard widgets, `auth-*` classes for authentication pages. MudBlazor is not used anywhere in the project.

5. Update the "What NOT to Do" list: Replace `Do NOT use <MudTable>, <MudDataGrid>, or MudBlazor card/layout components on OPD/counter pages` with `Do NOT use any MudBlazor components. The package is removed.`

6. Update the PrintLayout description: `PrintLayout is intentionally minimal — it renders only @Body with no providers or dependencies. Print pages load their own auth state in OnAfterRenderAsync.`

---

## Button Consistency Rule

The rule for this entire migration: **every action button must be a `<button>` element with visible text**. No icon-only buttons.

Specific replacements:
- `<MudIconButton Icon="..." OnClick="...">` → `<button class="opd-btn" @onclick="...">Label</button>` where "Label" is the action name in plain English (e.g., "Edit", "Delete", "Refresh", "Print")
- `<MudButton StartIcon="@Icons.Material.Filled.Add" ...>Add X</MudButton>` → `<button class="opd-btn-primary" @onclick="...">+ Add X</button>` (+ prefix replaces icon)
- `<MudButton StartIcon="@Icons.Material.Filled.Print">Print</MudButton>` → `<button class="opd-btn">Print</button>` (text is already there, just drop icon)
- `<MudButton StartIcon="@Icons.Material.Filled.Delete" Color="Color.Error">Delete</MudButton>` → `<button style="background:#FEE2E2;color:#991B1B;border:none;border-radius:8px;padding:6px 14px;font-weight:600;cursor:pointer;font-size:12px;" @onclick="...">Delete</button>`
- The dark mode `MudIconButton` in `AuthorizedLayout` is removed entirely (no replacement).
- The `MudMenu` account dropdown `<MudMenu Icon="...">` → a custom text button `<button ...>👤 Account ▾</button>` with a dropdown div.

---

## Implementation Order

Execute exactly in this sequence to avoid compile errors at each stage. Removing the NuGet package is always last.

| Step | Action | Files |
|------|--------|-------|
| 1 | Create IToastService + ToastService | `Services/ToastService.cs` (new) |
| 2 | Create ToastContainer component | `Components/ToastContainer.razor` (new) |
| 3 | Add toast + auth-form CSS to app.css | `wwwroot/css/app.css` (modify) |
| 4 | Rebuild PrintLayout | `Layout/PrintLayout.razor` |
| 5 | Rebuild AuthInvitationLayout | `Pages/Auth/AuthInvitationLayout.razor` |
| 6 | Rebuild AuthLayout | `Layout/AuthLayout.razor` |
| 7 | Rebuild DoctorPortalLayout | `Layout/DoctorPortalLayout.razor` |
| 8 | Rebuild AuthorizedLayout | `Layout/AuthorizedLayout.razor` |
| 9 | Register ToastService in Program.cs (keep AddMudServices for now) | `Program.cs` |
| 10 | Convert OpdDiscountDialog to custom modal | `Components/OpdDiscountDialog.razor` |
| 11 | Convert OpdParticularsPopup to custom modal | `Components/OpdParticularsPopup.razor` |
| 12 | Migrate Login.razor | `Pages/Auth/Login.razor` |
| 13 | Migrate Register.razor | `Pages/Auth/Register.razor` |
| 14 | Migrate ForgotPassword.razor | `Pages/Auth/ForgotPassword.razor` |
| 15 | Migrate ResetPassword.razor | `Pages/Auth/ResetPassword.razor` |
| 16 | Migrate TwoFactor.razor | `Pages/Auth/TwoFactor.razor` |
| 17 | Migrate VerifyEmail.razor | `Pages/Auth/VerifyEmail.razor` |
| 18 | Migrate ResendVerification.razor | `Pages/Auth/ResendVerification.razor` |
| 19 | Migrate AcceptInvitation.razor | `Pages/Auth/AcceptInvitation.razor` |
| 20 | Migrate AcceptExistingInvitation.razor | `Pages/Auth/AcceptExistingInvitation.razor` |
| 21 | Migrate Profile.razor | `Pages/Auth/Profile.razor` |
| 22 | Migrate Particulars.razor | `Pages/Admin/Particulars.razor` |
| 23 | Migrate ExpenseCategories.razor | `Pages/Admin/ExpenseCategories.razor` |
| 24 | Migrate ClinicProfile.razor | `Pages/Settings/ClinicProfile.razor` |
| 25 | Migrate DosageRemarkSettings.razor | `Pages/Settings/DosageRemarkSettings.razor` |
| 26 | Migrate PrescriptionSettings.razor | `Pages/Settings/PrescriptionSettings.razor` |
| 27 | Group G — all light-touch ISnackbar/spinner pages (can be done in any sub-order) | 21 files listed in Group G above |
| 28 | Fix App.razor NotFound section | `App.razor` |
| 29 | Fix Error.razor | `Pages/Error.razor` |
| 30 | Remove MudBlazor infrastructure | `_Imports.razor`, `wwwroot/index.html` |
| 31 | Remove AddMudServices from Program.cs | `Program.cs` |
| 32 | Remove MudBlazor package from .csproj | `TenantCore.Web.Client.csproj` |
| 33 | Update ADR-007 | `.claude/docs/adr/ADR-007-blazor-client.md` |

---

## Invariants — Must Not Break

| Invariant | Risk | Mitigation |
|-----------|------|-----------|
| Auth flow (login → select-clinic → dashboard) must still work | AuthorizedLayout rebuild may break redirect logic | Keep all `@code` logic in AuthorizedLayout identical; only replace markup |
| ISnackbar → IToastService replacement must not miss any call site | 34 files; any missed file causes build error once package removed | Remove package last; build after step 31 catches any missed injection |
| OpdDiscountDialog and OpdParticularsPopup `Visible`/`VisibleChanged` parameters must remain identical | Parent pages bind `@bind-Visible` to these components | Keep parameter names and EventCallback signatures exactly the same |
| Print pages must not be broken | PrintLayout change is minimal | PrintLayout becomes 1-line (@Body only) — zero risk |
| No backend / API change | Risk of accidentally touching server-side code | This plan is Blazor Client only; no server-side files are listed |
| All action-triggering event handlers preserved | Markup rewrites may accidentally remove @onclick | All @onclick, @onchange, @bind remain on their elements during markup replacement |

---

## Open Questions / Risks

1. **DoctorRegisterClinic.razor and DoctorChangePassword.razor** — these were identified as having ISnackbar but their full markup was not read. During execution, scan these files for any MudTextField/MudButton/MudGrid usage beyond ISnackbar and apply the auth-* / opd-* replacement accordingly.

2. **Profile.razor dialogs** — the password change and 2FA sections in Profile.razor may use inline MudDialog markup or a separate component. During execution, read the full file and convert any MudDialog to a custom modal.

3. **DosageRemarkSettings MudSelect** — this uses a `<MudSelect T="MedicineFormType">` with `<MudSelectItem>` children. The native `<select @bind="...">` replacement works for simple enum binding but loses the MudBlazor styled dropdown. The plain `<select>` is acceptable and consistent with other form pages.

4. **AcceptInvitation MudForm validation** — `MudForm @ref="_form" @bind-IsValid="_formValid"` drives the submit button's disabled state. Replace with Blazor `<EditForm>` + `<DataAnnotationsValidator>` and ensure the `_formValid` equivalent is managed via an `@code` bool that responds to input change events, or simply enable the button and validate on submit.

5. **PrescriptionForm MudChip** — medicine dosage chips use MudChip for display. The `<span>` replacement keeps the visual, but MudChip may have had a delete/close callback. Check if chips are removable and wire the `×` button into the replacement span accordingly.
