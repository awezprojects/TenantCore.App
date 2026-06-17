# Feature Plan: Clinic Admin Role — UI Fixes

**Repo:** TenantCore.App
**Date:** 2026-06-18
**Domain area:** Blazor Client — Role-Based Panel Navigation
**Status:** Approved — ready for execution

---

## Overview

Users with both Doctor and Clinic Admin roles can enter a clinic as either panel. Currently the active panel is inferred from the URL path, which causes the nav menu to silently switch from the Admin panel to the Doctor panel whenever an admin clicks a link that does not start with `/admin` (e.g., `/opd`, `/patients`, `/ipd`). This makes the Admin panel unstable for dual-role users.

In addition, users who have only the Clinic Admin role (no Doctor role) currently see an "Enter Clinic →" affordance on their clinic card that is unnecessary — they have no panel choice to make, so the card entry should be implicit via clicking the card itself with no footer action indicator.

This plan fixes both issues by introducing an explicit `ActivePanel` property in `ClinicContextService` (persisted to localStorage), wiring it into every clinic-entry path, and making the nav menu read from it instead of from the URL. It also cleans up the Admin panel nav by removing the doctor-specific OPD Queue item and adding Medicines as requested.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Domain | None |
| Infrastructure | None |
| Application | None |
| API | None |
| Shared | None |
| Blazor Client | 3 files modified — service, landing page, nav menu |

---

## Root Cause Analysis

### Problem 1 — Clinic card buttons for ClinicAdmin-only users

**File:** `src/TenantCore.Web.Client/Pages/Doctor/ClinicLanding.razor` lines 164–201

The `else` branch (non-owner card footer) renders an "Enter Clinic →" span for all non-owner users regardless of their role. ClinicAdmin-only users don't need to choose a panel, so this affordance is unnecessary and confusing.

The card outer div already has `@onclick="() => EnterClinic(item.Clinic.ApplicationId)"`, so card-level entry continues to work without any footer indicator.

### Problem 2 — Two buttons shown correctly for Doctor + ClinicAdmin

**File:** `src/TenantCore.Web.Client/Pages/Doctor/ClinicLanding.razor` lines 164–188

The `item.Clinic.IsOwner` flag maps exactly to the "Doctor who registered the clinic" case, which is also the "Doctor + ClinicAdmin" case described in the request. This already shows two buttons. No logic change needed here — only wire `SetActivePanelAsync()` into both entry methods.

### Problem 3 — Panel switching when navigating to shared routes

**File:** `src/TenantCore.Web.Client/Layout/NavMenu.razor` lines 8, 94–101

The admin panel condition is:
```
_isAdmin && (!_isDoctor || _currentPath.StartsWith("/admin"))
```

For a Doctor+ClinicAdmin user: admin panel only shows when on `/admin/*` routes. Clicking "OPD Queue" (`/opd`), "Patients" (`/patients`), "IPD & Beds" (`/ipd`), or "Rx Settings" (`/settings/prescription`) all navigate to non-admin routes, immediately flipping the nav into Doctor panel. The user is then locked out of the admin panel until they return to `/select-clinic` and re-enter as admin.

**Fix:** Track the active panel explicitly in `ClinicContextService`. The nav menu reads `ClinicContext.ActivePanel` instead of the URL. The panel only changes when the user explicitly enters a clinic — never because of a route change.

---

## Files to Modify

### 1. `src/TenantCore.Web.Client/Services/ClinicContextService.cs`

**What changes:**

Add `ActivePanel` tracking alongside the existing clinic-selection tracking. `ActivePanel` is a string (`"Admin"`, `"Doctor"`, `"Other"`) that is set when the user enters a clinic and cleared on logout/clinic-clear. It is persisted to localStorage under key `cc_active_panel` so it survives page refresh.

| Addition | Detail |
|----------|--------|
| `const string ActivePanelKey` | `"cc_active_panel"` |
| `public string ActivePanel` | Read-only property, default `string.Empty` |
| `SetActivePanelAsync(string panel)` | Sets `ActivePanel`, writes to localStorage, fires `OnClinicChanged` |
| Update `InitializeAsync()` | Read `cc_active_panel` from localStorage and assign `ActivePanel` |
| Update `ClearClinicAsync()` | Set `ActivePanel = string.Empty`, remove `cc_active_panel` from localStorage |

`SetActivePanelAsync` must be called **before** `SetClinicAsync` in `ClinicLanding` so that `OnClinicChanged` subscribers (NavMenu) receive the updated panel value when re-rendering.

Since `SetActivePanelAsync` fires `OnClinicChanged`, NavMenu will re-render automatically with the new panel — no new event needed.

---

### 2. `src/TenantCore.Web.Client/Pages/Doctor/ClinicLanding.razor`

**What changes:**

#### A — Wire `SetActivePanelAsync` into every entry method

| Method | Change |
|--------|--------|
| `EnterClinicAsDoctor(Guid appId)` | Call `await ClinicContext.SetActivePanelAsync("Doctor")` before `SetClinicAsync` |
| `EnterClinicAsAdmin(Guid appId)` | Call `await ClinicContext.SetActivePanelAsync("Admin")` before `SetClinicAsync` |
| `EnterClinic(Guid appId)` | Call `await ClinicContext.SetActivePanelAsync(isClinicAdmin ? "Admin" : "Other")` before `SetClinicAsync` |

All three methods already call `ClinicContext.SetClinicAsync(...)` which fires `OnClinicChanged`. By calling `SetActivePanelAsync` first (which also fires `OnClinicChanged`), the panel value is set before navigation begins.

#### B — Fix clinic card footer for ClinicAdmin-only users (lines 190–201)

The `else` block (non-owner footer) currently shows "Enter Clinic →" for all non-owner users. Change to check whether this specific clinic's roles contain ClinicAdmin/SystemAdmin but NOT Doctor:

Compute per-card inside the `@foreach`:
```
var clinicRoles = item.Clinic.UserRoles;
var isClinicAdminOnly =
    clinicRoles.Any(r => r.Equals("Clinic Admin", StringComparison.OrdinalIgnoreCase)
                      || r.Equals("System Admin", StringComparison.OrdinalIgnoreCase))
    && !clinicRoles.Any(r => r.Equals("Doctor", StringComparison.OrdinalIgnoreCase));
```

In the `else` footer block:
- If `isClinicAdminOnly` is true: render only the join date — no "Enter Clinic →" span. The card outer `@onclick` continues to handle entry.
- If `isClinicAdminOnly` is false (reception, other roles): render join date + "Enter Clinic →" (existing behavior).

The `@onclick` on the outer card div (`EnterClinic`) already routes ClinicAdmin-only users to `/admin/dashboard` via the existing `isClinicAdmin` check inside `EnterClinic()` — no change needed there.

---

### 3. `src/TenantCore.Web.Client/Layout/NavMenu.razor`

**What changes:**

#### A — Inject `ClinicContextService` and subscribe to `OnClinicChanged`

Add `@inject ClinicContextService ClinicContext` at the top.

In `OnInitialized`: add `ClinicContext.OnClinicChanged += OnClinicContextChanged`.
In `Dispose`: add `ClinicContext.OnClinicChanged -= OnClinicContextChanged`.
Add handler:
```
private void OnClinicContextChanged()
{
    InvokeAsync(StateHasChanged);
}
```

#### B — Replace path-based panel condition with `ClinicContext.ActivePanel`

Current (line 8):
```
@if (_isAdmin && (!_isDoctor || _currentPath.StartsWith("/admin")))
```

Replace with:
```
@if (ClinicContext.ActivePanel == "Admin")
```

Current (line 27):
```
else if (_isDoctor)
```

Replace with:
```
else if (ClinicContext.ActivePanel == "Doctor")
```

The `else` block (reception) remains as-is.

Keep `_isAdmin`, `_isDoctor`, and `RefreshState()` — they are not used for panel selection anymore but may be needed elsewhere (e.g., the register-clinic card in `ClinicLanding` uses `_isDoctor`). Actually in NavMenu they were only used for panel selection, so `RefreshState()` and the role fields can be removed if not referenced elsewhere in NavMenu. Confirm during execution.

Keep `_currentPath` and `UpdatePath()` — the `NavItem` component uses `CurrentPath` for active-link highlighting. This is unrelated to panel selection.

#### C — Remove OPD Queue from admin panel nav (line 19)

Remove:
```
<NavItem Href="/opd" Icon="📋" Label="OPD Queue" DrawerOpen="DrawerOpen" CurrentPath="_currentPath" />
```

OPD is a doctor workflow. Clinic Admins should not access it. The doctor panel already has "My OPD Queue" under the Doctor panel, which is correct.

#### D — Add Medicines to admin panel nav

Add after the Patients nav item in the admin panel section:
```
<NavItem Href="/medicines" Icon="💊" Label="Medicines" DrawerOpen="DrawerOpen" CurrentPath="_currentPath" />
```

The `/medicines` page already exists (it is in the Doctor panel nav). Admin users may manage the clinic medicine inventory, so it belongs in the Admin panel too.

**Final admin panel nav items (in order):**
1. Dashboard → `/admin/dashboard`
2. _(separator)_
3. Wards & Rooms → `/admin/wards`
4. User Management → `/admin/users`
5. IPD & Beds → `/ipd`
6. Patients → `/patients`
7. Medicines → `/medicines` _(new)_
8. _(separator)_
9. USG Template → `/admin/usg-template`
10. Clinic Profile → `/settings/clinic-profile`
11. Rx Settings → `/settings/prescription`
12. My Profile → `/auth/profile`

---

## Implementation Order

Execute in this sequence — each step compiles cleanly before moving to the next:

1. **`ClinicContextService.cs`** — add `ActivePanel`, `SetActivePanelAsync`, update `InitializeAsync` and `ClearClinicAsync`
2. **`ClinicLanding.razor` — entry methods** — wire `SetActivePanelAsync` into `EnterClinicAsDoctor`, `EnterClinicAsAdmin`, `EnterClinic`
3. **`ClinicLanding.razor` — card footer** — add `isClinicAdminOnly` computation and conditional footer rendering
4. **`NavMenu.razor`** — inject service, subscribe to event, replace panel condition, remove OPD Queue, add Medicines

---

## Behaviour After Fix

| Scenario | Before | After |
|----------|--------|-------|
| ClinicAdmin-only user on clinic card | Sees "Enter Clinic →" arrow | Sees join date only; card click enters admin panel |
| Doctor+ClinicAdmin on clinic card | Sees two buttons | Sees two buttons (no change) |
| Doctor-only on clinic card | Sees "Enter Clinic →" | Sees "Enter Clinic →" (no change) |
| Admin+Doctor enters as admin, clicks Patients `/patients` | Nav switches to Doctor panel | Nav stays on Admin panel |
| Admin+Doctor enters as admin, clicks IPD & Beds `/ipd` | Nav switches to Doctor panel | Nav stays on Admin panel |
| Admin+Doctor enters as admin — OPD Queue | OPD Queue in admin nav (wrong) | OPD Queue removed from admin nav |
| Admin panel — Medicines | Not available | Medicines nav item added |
| Page refresh while in admin panel | Panel lost if on non-admin route | Panel restored from localStorage |

---

## Open Questions / Risks

- **`_isAdmin` / `_isDoctor` in NavMenu after removal of panel-selection usage:** Confirm during execution that these fields are not referenced elsewhere in `NavMenu.razor` before removing `RefreshState()` and the event subscription to `AuthState.OnAuthStateChanged`. If unused, remove them to keep the component clean. If still needed (e.g., for some inline conditional), keep them.

- **`ActivePanel` fallback when empty:** If `ActivePanel` is `string.Empty` (user navigates directly to a clinic page without going through `ClinicLanding`), all three panel conditions will be false and the `else` (reception) menu will show. This is acceptable as a safe default. A more robust fallback could derive the panel from the user's roles at that point, but that is out of scope for this fix.

- **`/medicines` in admin panel while it's also in doctor panel:** The Medicines page at `/medicines` is currently doctor-scoped. Confirm that admin users can access it (no `[Authorize(Policy = AuthPolicies.RequireClinical)]` guard on the Medicines API that would block admin). If blocked, either use `RequireAuthenticated` or skip adding Medicines to admin nav.
