# Security Analysis: Medicine Dosage Form Normalization

**Date**: 2026-05-19
**Analyst**: Claude
**Plan**: plan/medicine-enrichment-service/PLAN.md

## Summary

Overall risk level: **Low**. The API-side changes follow the existing controller/CQRS patterns with proper authorization applied. The standalone normalizer tool is gitignored and runs locally only against a direct DB connection — it has no network-exposed surface. The main risk areas are the DB connection string in `appsettings.json` (mitigated by gitignore) and the lack of a page-size cap on the dosage forms endpoint.

## Findings

### CRITICAL (must fix before merge)

None.

### HIGH (should fix before merge)

None.

### MEDIUM (fix in follow-up)

- **[M1]** No page-size cap on `GET /api/medicine-dosage-forms`
  - **Location**: `src/TenantCore.Api/Controllers/MedicineDosageFormsController.cs` — `GetAll`
  - **Risk**: A caller can pass `pageSize=100000` and fetch all rows in one request, causing high memory usage.
  - **Fix**: Add a validator or clamp in the handler: `request.PageSize = Math.Min(request.PageSize, 100)`.

- **[M2]** `appsettings.json` in the tool contains a real connection string template
  - **Location**: `tools/MedicineNormalizer/appsettings.json`
  - **Risk**: If a developer fills in real credentials and accidentally tracks the file outside the gitignore scope (e.g., copies it to a tracked path), credentials could be committed.
  - **Fix**: Already mitigated by `.gitignore`. Ensure the connection string is stored in environment variables or a secrets manager on any shared/dev server rather than this file.

### LOW / Informational

- **[L1]** `MedicineDosageFormExtractor` accepts raw DB strings from `BrandName`/`PackSize`
  - These strings go through regex normalization only, not HTML/SQL encoding. Since extraction output is used as a name for a new `MedicineDosageForm` record (stored via EF Core parameterized query), there is no injection risk. No issue.

- **[L2]** `NormalizationWorker` re-uses a single `DbContextFactory` and calls `SaveChangesAsync` inside `FindOrCreateDosageFormAsync` mid-batch
  - This is safe because the tool is single-threaded and sequential. Not a concurrency issue, but worth noting if parallel processing is ever added.

- **[L3]** `MedicineDosageForm.CreateForSeed` is `public`
  - Needed for EF Core `HasData` across assembly boundaries. Not a security concern — it just creates in-memory entity instances.

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | All new endpoints decorated with `[Authorize]`. GET uses `RequireAuthenticated`, POST/PUT use `RequireClinical`, DELETE uses `RequireClinicAdmin` |
| Input Validation | PARTIAL | FluentValidation in place for commands. No page-size cap on list endpoint (M1) |
| Data Access & Injection | PASS | All queries via EF Core (parameterized). No raw SQL. Standalone tool also uses EF Core |
| Sensitive Data Handling | PASS | Connection string gitignored. No PII logged. No tokens in responses |
| Business Logic Security | PASS | Soft delete protects FK references. Duplicate name guarded by unique index + handler check |
| OWASP Top 10 | PASS | A01: Authorization policies applied. A03: EF Core parameterization. No other top-10 concerns identified |

## Recommended Actions

1. **(MEDIUM — follow-up)** [M1] Add page-size cap (max 100) to `GetMedicineDosageFormsHandler` or the query validator.
2. **(MEDIUM — follow-up)** [M2] When running the tool on a shared dev/staging environment, load the connection string from environment variable (`ConnectionStrings__ClinicConnection`) instead of `appsettings.json`.

## Approval

- [x] All CRITICAL findings resolved
- [x] All HIGH findings resolved or have accepted risk noted
- [ ] Ready to merge (pending M1 page-size cap)
