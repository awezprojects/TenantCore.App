# Security Analysis: Action & Audit Logging

**Date:** 2026-09-14
**Repo:** TenantCore.App
**Plan:** plan/action-audit-logging/PLAN.md
**ADR reference:** .claude/docs/adr/ADR-010-security.md

## Overall Risk Level

Low — this is a cross-cutting audit-logging feature with no new API surface, no new tenant-scoped data, and no changes to authentication, authorization, or entity mutation paths. It only adds a MediatR pipeline behavior and a write-only Azure Table Storage sink that mirrors the already-shipped `centralized-logging-service` pattern.

## Findings

### CRITICAL
None

### HIGH
None

### MEDIUM
None

### LOW / Informational
- **[L1]** Exception messages flow into a cross-tenant, unencrypted audit table without redaction — `src/TenantCore.Infrastructure/Services/ActionLoggingService.cs` (line 70) / `src/TenantCore.Application/Common/Behaviors/ActionLoggingBehavior.cs` (line 53) — **Accepted** (mirrors the existing, already-accepted `ErrorLoggingService` pattern, which stores strictly more detail — the full stack trace — in the same class of store; no new exposure introduced)

### Code Smells
None

### Architectural Violations
None

### Over-Engineering
None

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | No new endpoints or controllers added by this feature |
| Multi-Tenancy Isolation | PASS | `ApplicationId` is read via reflection from the existing command property (already validated/populated by the controller via `GetApplicationId()`); no new intake path, no `[FromBody]` acceptance |
| Input Validation | PASS | No new user-facing inputs; `ActionName` is a compile-time constant on each command |
| Data Access & SQL Injection | PASS | No EF Core or SQL changes |
| Sensitive Data Handling | PARTIAL | See L1 — accepted, consistent with existing precedent |
| Business Logic Security | PASS | No entity mutation logic touched; behavior only wraps existing handlers and always rethrows the original exception unchanged |
| OWASP Top 10 | PASS | No new resource, injection, or auth surface introduced |
| Code Quality | PASS | All new types `sealed`; correct layering (`IBusinessAction`/`ICurrentUserContext` in `Application/Common`, `IActionLogger` in `Application/Services`, matching the actual location of `IApplicationAccessValidator`/`IErrorLogger` in this codebase) |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| L1 | — | — | Skipped (user chose `skip` — no code changes applied) |

## Approval

- [x] All CRITICAL findings resolved or accepted with documented risk
- [x] All HIGH findings resolved or have accepted risk noted
- [x] No architectural violations remaining
- [x] Ready to merge
