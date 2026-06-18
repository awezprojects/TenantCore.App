# Security & Quality Analysis — role-authorization-hardening

**Date:** 2026-06-18
**Repo:** TenantCore.App
**Feature:** Role Authorization Hardening (UI + API)
**Risk level:** LOW

---

## Files Reviewed

| File | Layer |
|------|-------|
| `src/TenantCore.Api/Controllers/ObstetricController.cs` | API |
| `src/TenantCore.Api/Controllers/DoctorProfileController.cs` | API |
| `src/TenantCore.Web.Client/Services/AuthStateService.cs` | Client |
| `src/TenantCore.Web.Client/Pages/Admin/AdminDashboard.razor` | Client |
| `src/TenantCore.Web.Client/Pages/Admin/UserManagement.razor` | Client |
| `src/TenantCore.Web.Client/Pages/Admin/WardManagement.razor` | Client |
| `src/TenantCore.Web.Client/Pages/Admin/UsgTemplatePage.razor` | Client |
| `src/TenantCore.Web.Client/Pages/Settings/ClinicProfile.razor` | Client |
| `src/TenantCore.Web.Client/Pages/Settings/PrescriptionSettings.razor` | Client |
| `src/TenantCore.Web.Client/Pages/Settings/DosageRemarkSettings.razor` | Client |
| `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionList.razor` | Client |
| `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionForm.razor` | Client |

---

## Security Checklist Results

| Section | Result |
|---------|--------|
| S1 — Auth & Authorization | ✓ All new `[Authorize]` attributes correctly placed; no missing policy |
| S2 — Multi-Tenancy | ✓ N/A — no repository or data-access changes |
| S3 — Input Validation | ✓ N/A — no new inputs |
| S4 — Data Access / SQL Injection | ✓ N/A — no EF or raw SQL changes |
| S5 — Sensitive Data | ✓ N/A — no logging or DTO changes |
| S6 — Business Logic Security | ✓ N/A — no handler changes |
| S7 — OWASP Top 10 | ✓ A01 Broken Access Control addressed; others N/A |
| S8 — Code Smells | ✓ No Application-layer code added |
| S9 — Architectural Violations | ✓ Both controllers inherit `ClinicControllerBase`; no violations |
| S10 — Over-Engineering | ✓ No unnecessary abstractions |

---

## Findings

### L1 — PrescriptionForm: `OnParametersSetAsync` fires API calls after unauthorized redirect

**Severity:** LOW
**Status:** FIXED
**File:** `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionForm.razor`

**Root cause:** Blazor's component lifecycle always calls `OnParametersSetAsync` after `OnInitializedAsync` completes, regardless of early exit. The original guard in `OnInitializedAsync` called `NavigateTo` but did not `return`, and `OnParametersSetAsync` had no guard of its own. This caused 4–5 API GET calls to fire for unauthorized users before the navigation replaced the component.

Note: prescription GET endpoints are `RequireAuthenticated` by intentional design (established in PLAN.md), so no unauthorized data was returned by the API. The calls were wasted and the guard's intent was partially undermined.

**Fix applied:**
1. Added `return` after `NavigateTo` in `OnInitializedAsync`
2. Added `if (!AuthState.IsClinical(ClinicContext.SelectedApplicationId)) return;` at the top of `OnParametersSetAsync`

---

## Summary

| Severity | Count | Fixed |
|----------|-------|-------|
| CRITICAL | 0 | — |
| HIGH | 0 | — |
| MEDIUM | 0 | — |
| LOW | 1 | 1 |

**Fixes applied: 1**
