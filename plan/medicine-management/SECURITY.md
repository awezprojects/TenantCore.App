# Security Analysis: Medicine Management

**Date**: 2026-04-25
**Analyst**: Claude
**Plan**: plan/medicine-management/PLAN.md

## Summary

Overall risk level: **Low**. All controller endpoints are protected by authorization policies using the existing `AuthPolicies` constants. Input validation is fully covered by FluentValidation. All database access is via EF Core parameterized queries with no raw SQL. The primary concern is a medium-risk item around the fuzzy-match duplicate check potentially being bypassable in high-concurrency scenarios, and a low-risk note about EF `ToLower()` translation behavior.

## Findings

### CRITICAL (must fix before merge)

None.

### HIGH (should fix before merge)

None.

### MEDIUM (fix in follow-up)

- **[M1]** Fuzzy-match race condition on concurrent medicine creation
  - **Location**: `src/TenantCore.Application/Features/Medicines/Handlers/CreateMedicineHandler.cs` — `Handle` method
  - **Risk**: Two concurrent requests for the same medicine name can both pass the `FindSimilarAsync` check simultaneously before either persists, resulting in duplicate records in the global catalogue.
  - **Fix**: Add a unique index on `Name` in `MedicineConfiguration.cs` or wrap the check+insert in a serializable transaction. A unique DB index is the simplest and most reliable guard.

- **[M2]** EF Core `ToLower()` in `FindSimilarAsync` may not translate to SQL on all providers
  - **Location**: `src/TenantCore.Infrastructure/Repositories/MedicineRepository.cs` — `FindSimilarAsync`
  - **Risk**: `m.Name.ToLower().Contains(name.ToLower())` is evaluated client-side if EF cannot translate it, causing a full table scan. On SQL Server this is not a correctness issue but is a performance risk as the catalogue grows.
  - **Fix**: Replace with `EF.Functions.Like(m.Name, $"%{name}%")` for case-insensitive server-side evaluation, or use `m.Name.Contains(name)` which EF translates to SQL `LIKE` (case-insensitivity depends on DB collation, which is typically case-insensitive by default on SQL Server).

- **[M3]** Page-size cap not enforced at the API layer
  - **Location**: `src/TenantCore.Api/Controllers/MedicinesController.cs` and `MedicineTypesController.cs` — `GetAll` actions
  - **Risk**: A caller can request `pageSize=10000`, causing a large unbounded result set that strains memory and DB.
  - **Fix**: Add `pageSize = Math.Min(pageSize, 100)` at the start of each `GetAll` action, or add a validator on the query that rejects `PageSize > 100`.

### LOW / Informational

- **[L1]** Medicine entity `Update()` method accepts `IsActive = false` from any management-role user, which is the intended soft-delete. Since there is no DELETE endpoint and deactivation is the only removal path, this is by design. No action required.

- **[L2]** `MedicineType.GetByNameAsync` uses `t.Name.ToLower() == name.ToLower()` which may evaluate client-side on large tables. Acceptable for a typically-small lookup table, but consider `EF.Functions.Like` if the table grows.

- **[L3]** The `MedicineList.razor` page builds query string URLs with `Uri.EscapeDataString`, which correctly prevents open redirect and injection via query params. No issue.

- **[L4]** Audit fields (`CreatedBy`, `UpdatedBy`) are declared but not populated by the handlers — they are `null` after insert. This is consistent with the existing pattern in the codebase (Patient, OPD). Not a security issue but worth tracking as a data quality concern.

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | All GET endpoints use `RequireAuthenticated`; all POST/PUT use `RequireClinical`. No DELETE endpoints exposed. |
| Input Validation | PARTIAL | FluentValidation covers all fields. Page-size cap is missing (M3). |
| Data Access & Injection | PASS | All queries via EF Core. No raw SQL. No string interpolation in queries. |
| Sensitive Data Handling | PASS | No PII in medicine records. No secrets in response bodies. |
| Business Logic Security | PARTIAL | Duplicate guard is present but has a race-condition window (M1). Global catalogue correctly enforced — no ApplicationId injection possible. |
| OWASP Top 10 | PASS | A01 (access control): policies applied. A03 (injection): parameterized queries only. A07 (auth): JWT-protected endpoints. |

## Recommended Actions

1. **(MEDIUM — follow-up)** [M1] Add a unique index on `Medicines.Name` in `MedicineConfiguration.cs` to enforce deduplication at the DB level as a safety net against race conditions.
2. **(MEDIUM — follow-up)** [M2] Replace `ToLower().Contains()` in `MedicineRepository.FindSimilarAsync` with `EF.Functions.Like` or plain `.Contains()` to ensure server-side evaluation.
3. **(MEDIUM — follow-up)** [M3] Add `pageSize = Math.Min(pageSize, 100)` guard in `MedicinesController.GetAll` and `MedicineTypesController.GetAll`.

## Approval

- [ ] All CRITICAL findings resolved
- [ ] All HIGH findings resolved or have accepted risk noted
- [ ] Ready to merge
