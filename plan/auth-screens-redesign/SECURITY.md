# Security Analysis: Auth Screens UI Redesign

**Date:** 2026-06-18
**Repo:** TenantCore.App
**Plan:** plan/auth-screens-redesign/PLAN.md
**ADR reference:** .claude/docs/adr/ADR-010-security.md

## Overall Risk Level

Low — Pure Blazor WebAssembly UI redesign. No new backend endpoints, entities, repositories, or validators. Two pre-existing vulnerabilities were fixed during review.

## Findings

### CRITICAL
None

### HIGH
None

### MEDIUM
- **[M1]** Open Redirect via `ReturnUrl` — `Pages/Auth/Login.razor` + `Pages/Auth/TwoFactor.razor` — **Fixed**

  The `ReturnUrl` query parameter was decoded and passed directly to `NavigationManager.NavigateTo()` with only a guard against looping back to `/auth/login`. An attacker could craft a login link with `?returnUrl=https://external-phishing-site.com` to silently redirect authenticated users to an external site.

  Fix applied in both files: the decoded URL must now start with `/` and must not start with `//` (protocol-relative trick) before navigation is permitted. URLs that fail this check fall through to the `/select-clinic` default.

### LOW / Informational
- **[L1]** `Console.WriteLine` leaking exception context to browser DevTools — all 7 auth `@code` blocks — **Fixed**

  Every `catch (Exception ex)` block called `Console.WriteLine($"... error: {ex.Message}")`. In production, any user who opens DevTools could see diagnostic information including HTTP status codes or Auth API error strings.

  Fix applied: removed all `Console.WriteLine` calls across Login, ForgotPassword, VerifyEmail, TwoFactor, Register, ResendVerification, and ResetPassword. User-facing error messages remain generic strings unchanged.

### Code Smells
None — Smell checks S1–S12 apply to Application-layer code only (handlers, validators, translators). This feature contains no Application-layer changes.

### Architectural Violations
None — All changes are within `TenantCore.Web.Client`. No cross-layer dependencies were introduced.

### Over-Engineering
None

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | N/A | No controllers modified |
| Multi-Tenancy Isolation | N/A | No tenant-scoped operations; auth pages are public |
| Input Validation | PASS | Forms use `DataAnnotationsValidator`; server-side validation handled by TenantCore.Auth API |
| Data Access & SQL Injection | N/A | No repositories or EF Core changes |
| Sensitive Data Handling | PASS | `Console.WriteLine` calls removed (L1 fixed); no sensitive fields in DTOs |
| Business Logic Security | PASS | Open redirect vulnerability fixed (M1); email enumeration prevention preserved |
| OWASP Top 10 | PASS | A01 N/A (no resources); A03 N/A (no queries); open redirect (A01/A10) fixed |
| Code Quality | PASS | No smells or architectural violations |

## Fixes Applied

| ID | File(s) | Change | Status |
|----|---------|--------|--------|
| M1a | `Pages/Auth/Login.razor` | ReturnUrl guard: require `StartsWith("/")` and `!StartsWith("//")` before navigating | Applied |
| M1b | `Pages/Auth/TwoFactor.razor` | Same relative-path guard on post-2FA redirect | Applied |
| L1 | Login, ForgotPassword, VerifyEmail, TwoFactor, Register, ResendVerification, ResetPassword | Removed `Console.WriteLine(ex.Message)` from all catch blocks | Applied |

## Approval

- [x] All CRITICAL findings resolved or accepted with documented risk
- [x] All HIGH findings resolved or have accepted risk noted
- [x] No architectural violations remaining
- [x] Ready to merge
