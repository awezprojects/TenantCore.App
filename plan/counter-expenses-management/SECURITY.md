# Security Analysis: counter-expenses-management

**Date:** 2026-06-22
**Repo:** TenantCore.App
**Plan:** plan/counter-expenses-management/PLAN.md
**ADR reference:** .claude/docs/adr/ADR-010-security.md

## Overall Risk Level

Low — One privilege-escalation defect fixed (reception could delete expense records); all remaining findings were medium or lower, now resolved.

## Findings

### CRITICAL
None

### HIGH
- **[H1]** DELETE expense-records used `RequireReception` instead of `RequireClinicAdmin` — `ExpenseRecordsController.cs:57` — **Fixed**

### MEDIUM
- **[M1]** `GetAllAsync()` used without tenant filter (full cross-tenant table load into memory) — `GetExpenseRecordsHandler.cs`, `GetAmountHandoversHandler.cs` — **Fixed**
- **[M2]** `FinanceReportsController` defaulted to `DateTime.UtcNow.Date` — timezone-unsafe for non-UTC clinics — `FinanceReportsController.cs:19–36` — **Fixed**

### LOW / Informational
- **[L1]** `CreateExpenseRecordCommandValidator` missing `MaximumLength(500)` on `Notes` — **Fixed**
- **[L2]** `CreateAmountHandoverCommandValidator` missing `MaximumLength(500)` on `Notes` — **Fixed**

### Code Smells
- **[S6]** `AddOpdParticularHandler` called `SaveChangesAsync()` twice (partial-state risk) — **Fixed**
- **[S10]** `ClinicRepository<T>.GetAllAsync()` lacked `AsNoTracking()` — **Fixed**
- **[S10b]** `CounterSessionRepository.GetActiveSessionAsync` lacked `AsNoTracking()` — **Fixed**

### Architectural Violations
None

### Over-Engineering
None

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | All controllers class-level + per-action `[Authorize]`. DELETE now correctly requires `RequireClinicAdmin`. |
| Multi-Tenancy Isolation | PASS | All handlers now use repository methods that filter by `applicationId` at DB level. `GetAllAsync` + in-memory filter pattern eliminated for UTC-timestamp queries. |
| Input Validation | PASS | All required fields validated. Nullable `Notes` fields now capped at MaxLength(500). |
| Data Access & SQL Injection | PASS | All queries via EF Core LINQ. `AsNoTracking()` on all read paths. No raw SQL. |
| Sensitive Data Handling | PASS | No secrets in logs. No sensitive fields in response DTOs. Errors via `ExceptionHandlingMiddleware`. |
| Business Logic Security | PASS | All update/delete handlers verify `entity.ApplicationId == request.ApplicationId`. Status guards on AcceptOpdPayment, AcceptAmountHandover, CloseCounterSession. |
| OWASP Top 10 | PASS | A01: tenant filter on every data access. A03: parameterized EF queries only. A07: JWT config unchanged. |
| Code Quality | PASS | Double `SaveChangesAsync` eliminated. `AsNoTracking()` on all read methods. |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| H1 | `ExpenseRecordsController.cs:57` | DELETE auth changed from `RequireReception` → `RequireClinicAdmin` | Applied |
| M1 | `GetExpenseRecordsHandler.cs` | Replaced `GetAllAsync()` + in-memory filter with `GetByDateRangeAsync(fromUtc, toUtc, applicationId)` | Applied |
| M1 | `GetAmountHandoversHandler.cs` | Replaced `GetAllAsync()` + in-memory filter with `GetByDateRangeAsync(applicationId, fromUtc, toUtc)` | Applied |
| M2 | `FinanceReportsController.cs` | Added `[FromQuery] int utcOffset = 0`; default date computed as `DateTime.UtcNow.AddMinutes(utcOffset).Date` | Applied |
| L1 | `CreateExpenseRecordCommandValidator.cs` | Added `MaximumLength(500).When(Notes is not null)` | Applied |
| L2 | `CreateAmountHandoverCommandValidator.cs` | Added `MaximumLength(500).When(Notes is not null)` | Applied |
| S6 | `AddOpdParticularHandler.cs` | Removed first `SaveChangesAsync`; total computed in-memory; single atomic save at end | Applied |
| S10 | `ClinicRepository.cs:18` | Added `AsNoTracking()` to `GetAllAsync()` | Applied |
| S10b | `CounterSessionRepository.cs:11` | Added `AsNoTracking()` to `GetActiveSessionAsync` | Applied |

## Approval

- [x] All CRITICAL findings resolved or accepted with documented risk
- [x] All HIGH findings resolved or have accepted risk noted
- [x] No architectural violations remaining
- [x] Ready to merge
