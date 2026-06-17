# Feature Plan: Remove Old Unused Pages

**Repo:** TenantCore.App
**Date:** 2026-06-17
**Domain area:** Blazor Client — Layout & Page Cleanup
**Status:** Approved — ready for execution

---

## Overview

The Blazor client accumulated scaffold/template pages during early development that are no longer
part of the active application. These fall into three categories: pages that explicitly use the
old `MainLayout` (basic MudBlazor scaffold theme), pages that were replaced by the new
CloudClinic-themed flows but never deleted, and stub pages that have no real implementation.
None of these pages appear in any navigation menu or are reachable from any active user flow.
Removing them eliminates confusion, dead routes, and the old `MainLayout` entirely.

This is a Blazor client-only cleanup. No backend code, no API changes, no EF migrations.

---

## Why Each Page Is Being Removed

### Old scaffold template page

| Page | Route | Reason |
|------|-------|--------|
| `Pages/Home.razor` | `/` | Scaffold template from project setup. Content references "Tenant Management" and "WebAssembly Benefits", links to `/tenants` (route does not exist). The login flow redirects to `/select-clinic` after authentication — `/` is never reached through normal navigation. The only references to `/` are "Go to Home" buttons in `Error.razor` and `App.razor` which are updated as part of this plan. |

### Old super-admin application management flow (replaced by DoctorPortalLayout)

These four pages formed the original flow for creating and managing applications (clinics, schools, HR).
That flow has been replaced by `/doctor/register-clinic` (`DoctorRegisterClinic.razor`,
`DoctorPortalLayout`) for doctors. None of these pages appear in any navigation menu.
`AcceptExistingInvitation.razor` currently redirects to `/applications` after accepting an
invitation — that redirect is fixed in this plan to go to `/select-clinic` instead.

| Page | Route | Reason |
|------|-------|--------|
| `Pages/Applications/ApplicationList.razor` | `/applications` | Not in nav. Old generic tenant list — replaced by `ClinicLanding.razor` / `DoctorPortalLayout`. |
| `Pages/Applications/ApplicationCreate.razor` | `/applications/create` | Not in nav. Old super-admin create form — replaced by `/doctor/register-clinic`. |
| `Pages/Applications/ApplicationDetail.razor` | `/applications/{Id:guid}` | Not in nav. Only linked from `ApplicationList`. |
| `Pages/Applications/ApplicationEdit.razor` | `/applications/{Id:guid}/edit` | Not in nav. Only linked from `ApplicationList` and `ApplicationDetail`. |

### Stub page with no real implementation

| Page | Route | Reason |
|------|-------|--------|
| `Pages/Settings/Preferences.razor` | `/settings/preferences` | Not in nav. The `SavePreferences()` method shows a snackbar toast only — no API call, no persistence. The `ResetDefaults()` method resets local component variables only. None of the toggle switches or selects are wired to anything. |

### Orphaned page — functionality already exists elsewhere

| Page | Route | Reason |
|------|-------|--------|
| `Pages/Auth/ChangePassword.razor` | `/auth/change-password` | Not linked from any navigation menu, account menu, or profile page. Receptionists, admins, and all non-doctor users already have "Change Password" available inside `Profile.razor` (`/auth/profile`) via the inline Security section (expandable form, lines 194–250). The standalone page at `/auth/change-password` is unreachable dead code. |
| `Pages/Medicines/MedicineTypeList.razor` | `/medicine-types` | Not in nav. Only referenced in `AuthorizedLayout._titles` (which maps URLs to page headings) but no navigation item or button links to this route. |

### Old theme layout

| File | Reason |
|------|--------|
| `Layout/MainLayout.razor` | The old scaffold layout — plain `MudAppBar` with "TenantCore" text, no auth checks, no CloudClinic branding. Used as the default in `App.razor` and explicitly in `Error.razor`. Both are updated in this plan to use `AuthLayout` instead. After those updates, nothing references `MainLayout`. |

---

## Files to Delete — 9 total

| # | File |
|---|------|
| 1 | `src/TenantCore.Web.Client/Pages/Home.razor` |
| 2 | `src/TenantCore.Web.Client/Pages/Applications/ApplicationList.razor` |
| 3 | `src/TenantCore.Web.Client/Pages/Applications/ApplicationCreate.razor` |
| 4 | `src/TenantCore.Web.Client/Pages/Applications/ApplicationDetail.razor` |
| 5 | `src/TenantCore.Web.Client/Pages/Applications/ApplicationEdit.razor` |
| 6 | `src/TenantCore.Web.Client/Pages/Settings/Preferences.razor` |
| 7 | `src/TenantCore.Web.Client/Pages/Auth/ChangePassword.razor` |
| 8 | `src/TenantCore.Web.Client/Pages/Medicines/MedicineTypeList.razor` |
| 9 | `src/TenantCore.Web.Client/Layout/MainLayout.razor` |

---

## Files to Modify — 4 total

### 1. `src/TenantCore.Web.Client/Pages/Error.razor`

| Change | Detail |
|--------|--------|
| Layout directive | Change `@layout MainLayout` → `@layout AuthLayout` |
| "Go to Home" button | Change `Href="/"` → `Href="/select-clinic"` |

**Why `AuthLayout`:** Error pages can appear when the user is not authenticated (e.g., after a failed initial load). `AuthorizedLayout` would redirect an unauthenticated user to login before they can see the error. `AuthLayout` renders without auth checks — the same pattern used for login, register, and reset-password pages.

---

### 2. `src/TenantCore.Web.Client/App.razor`

| Change | Detail |
|--------|--------|
| `DefaultLayout` | Change `typeof(MainLayout)` → `typeof(AuthLayout)` |
| `NotFound` layout | Change `Layout="typeof(MainLayout)"` → `Layout="typeof(AuthLayout)"` |
| "Go to Home" button | Change `Href="/"` → `Href="/select-clinic"` |

**Why `AuthLayout` as default:** The default layout is only used by pages that do not declare their own `@layout`. After this cleanup every active page already declares an explicit layout. The default is effectively the fallback for the `NotFound` view — `AuthLayout` is appropriate there for the same reason as `Error.razor`.

---

### 3. `src/TenantCore.Web.Client/Pages/Auth/AcceptExistingInvitation.razor`

| Change | Detail |
|--------|--------|
| 3 action buttons | Change all `Href="/applications"` → `Href="/select-clinic"` |

There are three buttons in this page (success state, already-accepted state, and error state) that all redirect to `/applications` after an invitation is accepted. After accepting, the user should land on the clinic selection screen — which is exactly what `/select-clinic` provides. The deleted `ApplicationList` page is no longer a valid destination.

---

### 4. `src/TenantCore.Web.Client/Layout/AuthorizedLayout.razor`

| Change | Detail |
|--------|--------|
| `_titles` dictionary | Remove entry `["/medicine-types"] = "Medicine Types"` |
| `_titles` dictionary | Remove entry `["/settings/preferences"] = "Preferences"` |

These two entries map URL paths to page header titles. Removing them prevents the layout from attempting to look up headings for routes that no longer exist.

---

## Implementation Order

Execute in this exact sequence. The dependency is: fix all references to deleted pages first,
then delete the pages, then delete the old layout last.

| Step | Action | File |
|------|--------|------|
| 1 | Modify — fix invitation redirect | `Pages/Auth/AcceptExistingInvitation.razor` — 3× `Href="/applications"` → `Href="/select-clinic"` |
| 2 | Modify — clean stale title entries | `Layout/AuthorizedLayout.razor` — remove `/medicine-types` and `/settings/preferences` from `_titles` |
| 3 | Delete | `Pages/Applications/ApplicationList.razor` |
| 4 | Delete | `Pages/Applications/ApplicationCreate.razor` |
| 5 | Delete | `Pages/Applications/ApplicationDetail.razor` |
| 6 | Delete | `Pages/Applications/ApplicationEdit.razor` |
| 7 | Delete | `Pages/Home.razor` |
| 8 | Delete | `Pages/Settings/Preferences.razor` |
| 9 | Delete | `Pages/Auth/ChangePassword.razor` |
| 10 | Delete | `Pages/Medicines/MedicineTypeList.razor` |
| 11 | Modify — fix layout + redirect | `Pages/Error.razor` — `@layout MainLayout` → `@layout AuthLayout`; `Href="/"` → `Href="/select-clinic"` |
| 12 | Modify — fix layout + redirect | `App.razor` — `DefaultLayout` + `NotFound` layout to `AuthLayout`; `Href="/"` → `Href="/select-clinic"` |
| 13 | Delete | `Layout/MainLayout.razor` — must be last; steps 11 and 12 must complete first |

---

## Verification Checklist

After all steps are executed, verify:

- [ ] `dotnet build` in `src/TenantCore.Web.Client` produces zero errors
- [ ] No remaining references to `MainLayout` anywhere in the client project
- [ ] No remaining references to `/applications`, `/medicine-types`, `/settings/preferences`, `/auth/change-password`, or `/` (root) in any Razor file (except as anchored links that navigate away from login, which are fine)
- [ ] `Error.razor` renders under `AuthLayout`
- [ ] `App.razor` `NotFound` view renders under `AuthLayout`
- [ ] `AcceptExistingInvitation.razor` buttons point to `/select-clinic`

---

## No Migration Required

No database schema changes. No new entities. No new EF DbSets. No DI registrations added or removed.

---

## No Test Changes Required

No application logic is being changed. The deleted pages contain only Blazor UI code — no handlers, validators, translators, or domain services. Existing test coverage is unaffected.

---

## Open Questions / Risks

- **`/Applications` folder becomes empty after deletions.** The folder `Pages/Applications/` will contain no files. Verify the folder is removed by the executor or confirm the build system handles empty directories gracefully (Blazor does not require explicit folder cleanup, but keeping empty folders is noise).
- **Future: "Change Password" for non-doctor account menu.** The account menu in `AuthorizedLayout` currently shows no "Change Password" option for non-doctor users (receptionists, admins). The functionality lives inside `/auth/profile`. If a dedicated menu shortcut is later desired, a `MudMenuItem` pointing to `/auth/profile` (scrolling to the Security section) or a new inline drawer panel would be the right approach. That is out of scope for this plan.
