# Feature Plan: Role Authorization Hardening

**Repo:** TenantCore.App
**Date:** 2026-06-18
**Domain area:** Security / Authorization
**Status:** Approved — ready for execution

---

## Overview

This plan closes the gap between UI-level role hiding and actual server-enforced authorization. Currently, the NavMenu conditionally shows different pages per role (Admin, Doctor, Reception panels), but neither the Blazor pages nor several API endpoints enforce role requirements independently. A user who knows a URL can paste it directly into the browser and either see restricted content or manipulate restricted data. The fix has two parts: add missing `[Authorize(Policy = ...)]` attributes to API endpoints that currently only require authentication, and add role-guard code to every Blazor page whose route should be restricted to a specific role.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| API | Add `[Authorize(Policy = ...)]` to 3 controller actions across 2 controllers |
| Blazor Client | Add role-guard helper methods to `AuthStateService`; add guard + redirect to 9 pages |
| Domain / Application / Infrastructure / Shared | No changes |

---

## Security Findings

### API Layer — Missing Role Policies

The following endpoints currently have only the class-level `[Authorize(Policy = RequireAuthenticated)]`. They expose sensitive clinical data or allow sensitive mutations to any authenticated user, including Reception staff who should not have access.

| Controller | Method | Route | Current Policy | Required Policy | Reason |
|-----------|--------|-------|---------------|----------------|--------|
| `ObstetricController` | GET | `/api/obstetric/prescriptions/{prescriptionId}/dates` | RequireAuthenticated | **RequireClinical** | Raw obstetric dates (LMP, EDD) are medical records; Reception has no need for this data |
| `ObstetricController` | GET | `/api/obstetric/patients/{patientId}/usg-chart` | RequireAuthenticated | **RequireClinical** | USG chart data is a clinical record; accessible to Reception via URL paste |
| `DoctorProfileController` | PUT | `/api/doctor-profile` | RequireAuthenticated | **RequireClinical** | Only doctors (Clinical role) should be able to create or update a doctor profile |

All other controllers were audited and are correctly guarded:
- `WardsController`, `BedsController`, `RoomsController` — GETs are `RequireAuthenticated` (correct; IPD page needs them for bed selection), mutations are `RequireClinicAdmin`
- `PatientsController`, `OpdRegistrationsController`, `IpdRegistrationsController` — mutations are `RequireReception` ✓
- `PrescriptionsController`, `DosageRemarksController`, `MedicineDosageFormsController` — mutations are `RequireClinical` ✓
- `UsgTemplatesController`, `ApplicationController` — admin mutations are `RequireClinicAdmin` ✓
- `PregnancyTenuresController` — GET endpoints intentionally `RequireAuthenticated` because the EDD Overdue tab is shown in all nav panels including Reception
- `PrescriptionConfigController` PUT — correctly `RequireClinical` (both ClinicAdmin and Doctor use this setting)
- `AuthController` — correctly `AllowAnonymous` (transparent proxy to TenantCore.Auth)

---

### Blazor Client Layer — Missing Page-Level Role Guards

`AuthorizedLayout` only checks two conditions before rendering `@Body`:
1. Is the user authenticated? (redirect to `/auth/login` if not)
2. Has the user selected a clinic? (redirect to `/select-clinic` if not)

It does **not** check the user's role. The NavMenu hides links for the wrong role, but a user who pastes a URL directly bypasses the NavMenu entirely and the page renders with full data access.

| Page File | Route | Visible In | Should Require | Security Gap |
|-----------|-------|-----------|---------------|--------------|
| `Admin/AdminDashboard.razor` | `/admin/dashboard` | Admin panel | **ClinicAdmin** | Reception/Doctor can load the page and see ward/IPD overview data (API returns 200) |
| `Admin/UserManagement.razor` | `/admin/users` | Admin panel | **ClinicAdmin** | Reception/Doctor can view and interact with the user management UI (user list API returns 200) |
| `Admin/WardManagement.razor` | `/admin/wards` | Admin panel | **ClinicAdmin** | Reception/Doctor can see the ward management interface |
| `Admin/UsgTemplatePage.razor` | `/admin/usg-template` | Admin panel | **ClinicAdmin** | Reception/Doctor can view and attempt to edit USG appointment templates |
| `Settings/ClinicProfile.razor` | `/settings/clinic-profile` | Admin panel | **ClinicAdmin** | Reception/Doctor can view clinic profile settings |
| `Settings/PrescriptionSettings.razor` | `/settings/prescription` | Admin + Doctor panels | **Clinical** | Reception can load the page (save will 403 at API but page renders) |
| `Settings/DosageRemarkSettings.razor` | `/settings/dosage-remarks` | Doctor panel | **Clinical** | Reception can load the page; read API returns 200 so remark list is visible |
| `Prescriptions/PrescriptionList.razor` | `/prescriptions` | Doctor panel | **Clinical** | Reception can load the prescription list (API returns 403 on mutations but GET returns 200 for `RequireAuthenticated`) |
| `Prescriptions/PrescriptionForm.razor` | `/prescriptions/{id}` `/prescriptions/new/{id}` | Doctor panel | **Clinical** | Reception can load the prescription form; API writes will 403 but the form itself renders |

**Print pages** (`PrescriptionPrint.razor`, `PrintUsgChart.razor`) are excluded — they can only be opened from within already-guarded pages and have no independent navigable discovery vector.

**`/patients/edd-overdue`** and **`/patients/{id}/usg-chart`** via the UsgChartPage are excluded from client guards because the EDD Overdue route appears in all nav panels (including the Reception/default panel), meaning Reception access is intentional at the UI level.

---

## Implementation Approach

### API Side

Add a method-level `[Authorize(Policy = ...)]` attribute to each identified endpoint. This does not change any handler, command, or query — only the controller attribute.

### Client Side

**Step 1 — Add two helper methods to `AuthStateService`:**

Add `IsClinicAdmin(Guid applicationId)` and `IsClinical(Guid applicationId)` methods that check the user's active roles for the specified clinic. Role name strings must use `AppRoles` constants from `TenantCore.Shared.Authorization`. The methods must return `false` if the user is not authenticated or no application is selected.

| Helper | Returns true when role is |
|--------|--------------------------|
| `IsClinicAdmin(Guid)` | `AppRoles.ClinicAdmin` or `AppRoles.SystemAdmin` |
| `IsClinical(Guid)` | Any role in `AppRoles.ClinicalRoles` (Doctor, ClinicAdmin, ClinicManager, SystemAdmin) |

**Step 2 — Add role guard to each restricted page:**

Each page's `OnInitializedAsync` already calls `await AuthState.InitializeAsync()` and `await ClinicContext.InitializeAsync()` (or relies on `AuthorizedLayout` having done it). After initialization, add a role check and navigate away if the check fails. Use `NavigationManager.NavigateTo("/dashboard", replace: true)` as the redirect — replacing history prevents the back button from re-entering the forbidden page.

Guard pattern for every affected page:
```
// After initialization, before any data load:
if (!AuthState.IsClinicAdmin(ClinicContext.SelectedApplicationId))
{
    NavigationManager.NavigateTo("/dashboard", replace: true);
    return;
}
```
(swap `IsClinicAdmin` for `IsClinical` on clinical-role pages)

---

## Files to Modify

### API Layer

| File | Change |
|------|--------|
| `src/TenantCore.Api/Controllers/ObstetricController.cs` | Add `[Authorize(Policy = AuthPolicies.RequireClinical)]` to `GetObstetricDates` (GET) and `GetUsgChart` (GET) actions |
| `src/TenantCore.Api/Controllers/DoctorProfileController.cs` | Add `[Authorize(Policy = AuthPolicies.RequireClinical)]` to `UpsertMyProfile` (PUT) action |

### Blazor Client Layer

| File | Change |
|------|--------|
| `src/TenantCore.Web.Client/Services/AuthStateService.cs` | Add `IsClinicAdmin(Guid applicationId)` and `IsClinical(Guid applicationId)` public methods |
| `src/TenantCore.Web.Client/Pages/Admin/AdminDashboard.razor` | Add ClinicAdmin guard + redirect in `OnInitializedAsync`, before data load calls |
| `src/TenantCore.Web.Client/Pages/Admin/UserManagement.razor` | Add ClinicAdmin guard + redirect in `OnInitializedAsync` |
| `src/TenantCore.Web.Client/Pages/Admin/WardManagement.razor` | Add ClinicAdmin guard + redirect in `OnInitializedAsync` |
| `src/TenantCore.Web.Client/Pages/Admin/UsgTemplatePage.razor` | Add ClinicAdmin guard + redirect in `OnInitializedAsync` |
| `src/TenantCore.Web.Client/Pages/Settings/ClinicProfile.razor` | Add ClinicAdmin guard + redirect in `OnInitializedAsync` |
| `src/TenantCore.Web.Client/Pages/Settings/PrescriptionSettings.razor` | Add Clinical guard + redirect in `OnInitializedAsync` |
| `src/TenantCore.Web.Client/Pages/Settings/DosageRemarkSettings.razor` | Add Clinical guard + redirect in `OnInitializedAsync` |
| `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionList.razor` | Add Clinical guard + redirect in `OnInitializedAsync` |
| `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionForm.razor` | Add Clinical guard + redirect in `OnInitializedAsync` |

---

## API Endpoint Authorization — After Fix

| Controller | Method | Route | Policy After Fix |
|-----------|--------|-------|----------------|
| ObstetricController | GET | `/api/obstetric/prescriptions/{id}/dates` | **RequireClinical** |
| ObstetricController | GET | `/api/obstetric/patients/{patientId}/usg-chart` | **RequireClinical** |
| ObstetricController | PUT | `/api/obstetric/prescriptions/{id}/lmp` | RequireClinical (unchanged) |
| ObstetricController | PUT | `/api/obstetric/prescriptions/{id}/edd-by-usg` | RequireClinical (unchanged) |
| DoctorProfileController | GET | `/api/doctor-profile` | RequireAuthenticated (unchanged — any role can read their own profile) |
| DoctorProfileController | PUT | `/api/doctor-profile` | **RequireClinical** |

---

## Implementation Order

Execute in this sequence:

1. API controller changes — `ObstetricController.cs` and `DoctorProfileController.cs` (independent, can be done in either order)
2. `AuthStateService` — add `IsClinicAdmin` and `IsClinical` helpers (must be done before page guards)
3. Admin page guards — `AdminDashboard`, `UserManagement`, `WardManagement`, `UsgTemplatePage`, `ClinicProfile` (all use `IsClinicAdmin`; parallel order fine)
4. Clinical page guards — `PrescriptionSettings`, `DosageRemarkSettings`, `PrescriptionList`, `PrescriptionForm` (all use `IsClinical`; parallel order fine)

---

## EF Migration

**None required.** This plan modifies only authorization attributes and client-side guard code — no data model changes.

---

## Business Rules

1. `IsClinicAdmin` returns `true` for `ClinicAdmin` and `SystemAdmin` roles — matching the existing logic in `AuthorizedLayout` (`_isAdmin` flag).
2. `IsClinical` returns `true` for any role in `AppRoles.ClinicalRoles` — consistent with the existing `ClinicRoleAuthorizationHandler` for the `RequireClinical` server policy.
3. The redirect destination is `/dashboard` — the generic landing page. No toast/snackbar is shown on redirect because the guard fires before the page UI is rendered, making a message impossible to display. The UX consequence is acceptable: users who manually typed an unauthorized URL are simply returned to the dashboard silently.
4. Guards use `replace: true` on navigation so the browser back button does not loop back to the forbidden page.
5. `DoctorProfileController.GET` is intentionally left as `RequireAuthenticated` — reading your own profile is not role-restricted.

---

## Open Questions / Risks

- **`IsClinical` scope**: If a user has the Doctor role in Clinic A but only Receptionist in Clinic B, `IsClinical(clinicBAppId)` will correctly return `false`. This requires that `ClinicContext.SelectedApplicationId` is always the clinic the user is currently working in — which `AuthorizedLayout` already enforces before rendering.
- **`PrescriptionSettings` dual-panel**: This page appears in both the Admin panel and the Doctor panel nav. The guard uses `IsClinical` which covers both `Doctor` and `ClinicAdmin`, so admins retain access.
- **Print pages**: `PrescriptionPrint.razor` and `PrintUsgChart.razor` are opened in a new tab from within already-guarded pages and use `OnAfterRenderAsync` with explicit auth init. They do not need their own role guard since they can only be reached by following a link from a page the user already has access to; a direct URL paste would fail at the API level (prescription/USG endpoints require `RequireClinical`).
