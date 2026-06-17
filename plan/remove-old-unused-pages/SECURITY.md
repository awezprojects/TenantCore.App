# Security Analysis: remove-old-unused-pages

**Date:** 2026-06-18
**Repo:** TenantCore.App
**Plan:** plan/remove-old-unused-pages/PLAN.md
**ADR reference:** .claude/docs/adr/ADR-010-security.md

---

## Overall Risk Level

**Low** — Pure deletion/cleanup feature. Nine files removed, four Blazor client files modified. No new backend code, no new API endpoints, no new data access. The net effect is a reduced attack surface.

---

## Findings

### CRITICAL
None

### HIGH
None

### MEDIUM
None

### LOW / Informational

- **[L1]** Pre-existing `Console.WriteLine` in `AcceptExistingInvitation.razor:129`
  Status: **Accepted (pre-existing, out of scope)**
  Note: Outputs `ex.Message` to browser DevTools console only (Blazor WASM). No server-side log risk. Message contains no sensitive data. Predates this feature — not introduced by this change.

### Code Smells
None — all modified files are Blazor `.razor` client components. Application/API layer smells (S1–S12) are not applicable.

### Architectural Violations
None

### Over-Engineering
None

---

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | No new controllers or auth policies. `App.razor` DefaultLayout → `AuthLayout` (correct — auth-state agnostic for NotFound). |
| Multi-Tenancy Isolation | N/A | No new entities, commands, queries, or repository access in modified files. |
| Input Validation | N/A | No new form inputs or validators introduced. |
| Data Access & SQL Injection | N/A | No new EF Core or repository code added. |
| Sensitive Data Handling | PASS | No new log statements with sensitive data. Pre-existing console.log (L1) noted but accepted. |
| Business Logic Security | N/A | No new business logic. Navigation changes only. |
| OWASP Top 10 | PASS | A01: Removed application pages reduce unauthenticated attack surface. A03/A04: No new vectors. |
| Code Quality | PASS | No code smells or violations in modified files. |

---

## Security Improvements Delivered

| Change | Security Effect |
|--------|----------------|
| `MainLayout.razor` removed as default layout | Eliminates risk of future pages accidentally receiving an unauthenticated layout by omitting `@layout` |
| `ApplicationCreate/List/Edit/Detail.razor` deleted | Removes 4 admin-capability routes (create/edit/delete tenants) that were reachable via direct URL navigation even though no nav menu exposed them |
| `ChangePassword.razor` deleted | Removes duplicate auth endpoint unreachable from UI, eliminating dead code surface |
| `AcceptExistingInvitation.razor` redirect → `/select-clinic` | Sends invited users into the properly auth-checked `DoctorPortalLayout` flow |

---

## Fixes Applied

None required.

---

## Approval

- [x] All CRITICAL findings resolved or accepted with documented risk
- [x] All HIGH findings resolved or have accepted risk noted
- [x] No architectural violations remaining
- [x] Ready to merge
