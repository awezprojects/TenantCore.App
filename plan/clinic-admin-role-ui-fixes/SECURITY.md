# Security Analysis: clinic-admin-role-ui-fixes

**Date:** 2026-06-18
**Repo:** TenantCore.App
**Plan:** plan/clinic-admin-role-ui-fixes/PLAN.md
**ADR reference:** .claude/docs/adr/ADR-010-security.md

## Overall Risk Level

Low — pure Blazor WASM client change; no new API endpoints, no entity changes, no auth policy changes. The real security boundary (API-level `[Authorize]` policies) was not modified.

## Findings

### CRITICAL
None

### HIGH
None

### MEDIUM

- **[M1]** Panel separation is a UX control only, not a security boundary — `NavMenu.razor` — **Closed (False Positive)**

  Investigation: `ClinicAdmin` is explicitly listed in both `ReceptionRoles` and `ClinicalRoles` in `AuthorizationConstants.cs`. The API-level access to OPD and Prescription endpoints for ClinicAdmin users is **intentional by design**. The nav-level hiding of OPD/Prescriptions from the Admin panel is a UX improvement (prevent confusion for dual-role users), not a security requirement. No fix needed.

### LOW / Informational

- **[L1]** `SetActivePanelAsync` fired `OnClinicChanged` redundantly — `ClinicContextService.cs` — **Fixed**

  NavMenu was re-rendering twice per clinic entry (once from `SetActivePanelAsync`, once from `SetClinicAsync`). Fixed by removing the event fire from `SetActivePanelAsync` — `SetClinicAsync` (always called immediately after) fires the single notification with both values already set.

- **[L2]** `ActivePanel` is empty on first-ever session (no localStorage value) — `NavMenu.razor` — **Accepted**

  If `cc_active_panel` is not in localStorage, all panel conditions fail and the reception (`else`) menu shows briefly before `AuthorizedLayout` redirects the user to `/select-clinic`. This is pre-existing behaviour and harmless — the redirect fires before the user can interact with the wrong menu. No code fix applied.

### Code Smells
None — no handlers, translators, repositories, or validators were modified.

### Architectural Violations
None

### Over-Engineering
None

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | No new endpoints; existing policies unchanged |
| Multi-Tenancy Isolation | PASS | No new data access; `ApplicationId` scoping untouched |
| Input Validation | PASS | No new user inputs |
| Data Access & SQL Injection | PASS | No data access layer changes |
| Sensitive Data Handling | PASS | `ActivePanel` value ("Admin"/"Doctor"/"Other") is not sensitive |
| Business Logic Security | PASS | No business logic changed |
| OWASP Top 10 | PASS | No new attack surfaces introduced |
| Code Quality | PASS | Redundant event notification removed (L1) |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| L1 | `src/TenantCore.Web.Client/Services/ClinicContextService.cs` | Removed redundant `OnClinicChanged?.Invoke()` from `SetActivePanelAsync`; single notification from `SetClinicAsync` is sufficient | Applied |
| M1 | — | Closed as false positive — `ClinicAdmin` in `ReceptionRoles`/`ClinicalRoles` is intentional | No change |
| L2 | — | Accepted risk — first-session empty panel is harmless due to redirect guard | No change |

## Approval

- [x] All CRITICAL findings resolved or accepted with documented risk
- [x] All HIGH findings resolved or have accepted risk noted
- [x] No architectural violations remaining
- [x] Ready to merge
