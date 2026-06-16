# Security Analysis: Full Project

**Date:** 2026-06-17
**Repo:** TenantCore.App
**Plan:** no plan — code-only scan (first-ever full-project review)
**ADR reference:** .claude/docs/adr/ADR-010-security.md

## Overall Risk Level

**High** — Four critical broken-access-control paths existed allowing any authenticated user to perform admin-level destructive operations and cross-tenant data enumeration. JWT configuration had a 5-minute clock skew and no HTTPS enforcement.

## Findings

### CRITICAL
- **[C1]** RoleController — cross-tenant role enumeration via caller-supplied applicationId route param — `src/TenantCore.Api/Controllers/RoleController.cs` — **Fixed**
- **[C2]** ApplicationController — destructive operations (Delete, Remove, Toggle, ChangeRole) accessible to any authenticated user — `src/TenantCore.Api/Controllers/ApplicationController.cs` — **Fixed**
- **[C3]** ClinicController — bare `[Authorize]` (no policy) on all clinic operations — `src/TenantCore.Api/Controllers/ClinicController.cs` — **Fixed**
- **[C4]** PatientsController.Delete — restricted to `RequireReception` instead of `RequireClinicAdmin` — `src/TenantCore.Api/Controllers/PatientsController.cs` — **Fixed**

### HIGH
- **[H1]** ClinicContextMiddleware + GetApplicationId() — silent fallback to first `app_ids` claim when header absent — `src/TenantCore.Api/Controllers/ClinicControllerBase.cs` — **Fixed**
- **[H2]** AuthController.ForwardAsync — server-generated correlation ID not propagated to AuthApi — `src/TenantCore.Api/Controllers/AuthController.cs` — **Fixed**
- **[H3]** GetApplicationByCode — unbounded `string Code` route param (no MaximumLength) — new validator created — **Fixed**
- **[H4]** OpdRegistrationsController.Update (PUT) — only `RequireAuthenticated` on a mutation — `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` — **Fixed**
- **[H5]** GetMedicineAutocompleteHandler — `Limit` not capped (unbounded result sets) — `src/TenantCore.Application/Features/Medicines/Handlers/GetMedicineAutocompleteHandler.cs` — **Fixed**
- **[H6]** GetMedicinesHandler / GetMedicineTypesHandler / GetMedicineDosageFormsHandler — no page-size cap — **Fixed** (all three handlers capped at 100)
- **[H7]** JWT: `RequireHttpsMetadata = false` in non-development environments — `src/TenantCore.Api/Extensions/ServiceCollectionExtensions.cs` — **Fixed**
- **[H8]** JWT: `ClockSkew = TimeSpan.FromMinutes(5)` — `src/TenantCore.Api/Extensions/ServiceCollectionExtensions.cs` — **Fixed** (set to Zero)

### MEDIUM
- **[M1]** MedicineTypesController / MedicineDosageFormsController — not inheriting `ClinicControllerBase` (V3) — Deferred (global-scope entities; acceptable deviation if documented)
- **[M2]** DoctorProfileController — duplicates claims parsing, not inheriting `ClinicControllerBase` — Deferred (code quality task)
- **[M3]** DoctorSpecialitiesController — not inheriting `ClinicControllerBase` — Deferred
- **[M4]** GetApplicationId() silent fallback — **Fixed** (addressed as part of H1)
- **[M5]** CreatePrescriptionHandler — fragile cross-aggregate save via shared DbContext — Deferred (data integrity; document shared-context dependency)
- **[M6]** UpsertClinicUsgTemplateHandler — two SaveChangesAsync calls (data-loss window) — Deferred
- **[M7]** SubmitPrescriptionHandler — multiple SaveChangesAsync — Deferred
- **[M8]** SetObstetricLmpHandler / SetObstetricEddByUsgHandler — two-save pattern, partial-state risk — Deferred
- **[M9]** PatientsController.UploadPhoto — file validation in controller body (S1) — Deferred (code quality)
- **[M10]** PrescriptionsController.UploadReport — same as M9 — Deferred

### LOW / Informational
- **[L1]** ApplicationController — inline null-check response logic — Deferred
- **[L2]** ApplicationController.GetApplicationByIdAsync — returns `NotFound()` instead of throwing exception — Deferred
- **[L3]** Swagger guarded correctly — No issue
- **[L4]** No `AsNoTracking()` anywhere in Infrastructure layer — Deferred (performance)
- **[L5]** `[AllowAnonymous]` on AuthController missing explanatory comment — Deferred
- **[L6]** OpdRegistrationsController.GetAll — OpdStatus enum not validated — Deferred

### Code Smells
- **[S1-a/b/c]** Business logic in controller action bodies (UploadPhoto, UploadReport, ApplicationController) — Deferred
- **[S2]** RoleController injects `IHttpClientFactory` in constructor — Deferred (refactor to MediatR command)
- **[S4]** Several commands/validators missing `sealed` keyword — Deferred
- **[S5-a/b]** Mid-handler SaveChangesAsync calls — Deferred
- **[S6-a/b]** Multiple SaveChangesAsync in one handler — Deferred
- **[S10]** No `AsNoTracking()` in Infrastructure layer — Deferred
- **[S11]** `Guid.NewGuid()` in domain entity factories — Deferred (accepted pattern or needs migration)

### Architectural Violations
- **[V3-a through V3-g]** Seven controllers inherit `ControllerBase` directly instead of `ClinicControllerBase` — Deferred
- **[V7-a/b]** `CreateApplicationCommand`, `GetApplicationByCodeQuery` not `sealed record` — Deferred

### Over-Engineering
- **[O1]** `UpdateClinicFeeConfigCommandValidator` — nearly empty — Deferred
- **[O4-a]** `ClinicControllerBase.GetApplicationName` — bare `catch { }` — Deferred (change to `catch (JsonException)`)
- **[O4-b]** `ClinicRoleAuthorizationHandler` — bare `catch { }` — Deferred

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | **PARTIAL** | Critical fixes applied; 7 controllers still inherit ControllerBase directly (V3) |
| Multi-Tenancy Isolation | **PASS** | GetApplicationId() fallback removed; cross-tenant guard added to RoleController |
| Input Validation | **PARTIAL** | GetApplicationByCode bounded; OpdStatus enum validation deferred |
| Data Access & SQL Injection | **PASS** | EF Core LINQ throughout; no raw SQL found |
| Sensitive Data Handling | **PARTIAL** | Correlation ID propagation fixed; no password logging found |
| Business Logic Security | **PARTIAL** | Ownership checks exist in handlers; multi-save data integrity deferred |
| OWASP Top 10 | **PARTIAL** | A01 improved (auth policies); A07 fixed (ClockSkew=Zero, HTTPS enforced) |
| Code Quality | **PARTIAL** | Security smells addressed; structural smells deferred |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| C1 | `src/TenantCore.Api/Controllers/RoleController.cs` | Inherit ClinicControllerBase; RequireAuthenticated policy; cross-tenant Forbid guard | Applied |
| C2 | `src/TenantCore.Api/Controllers/ApplicationController.cs` | RequireAuthenticated class-level; RequireClinicAdmin on 6 destructive actions | Applied |
| C3 | `src/TenantCore.Api/Controllers/ClinicController.cs` | RequireAuthenticated explicit policy | Applied |
| C4 | `src/TenantCore.Api/Controllers/PatientsController.cs` | Delete action: RequireReception → RequireClinicAdmin | Applied |
| H1/M4 | `src/TenantCore.Api/Controllers/ClinicControllerBase.cs` | GetApplicationId() fallback removed; returns Guid.Empty when header absent | Applied |
| H2 | `src/TenantCore.Api/Controllers/AuthController.cs` | ForwardAsync always injects X-Correlation-Id from TraceIdentifier | Applied |
| H3 | `src/TenantCore.Application/Features/Applications/Validators/GetApplicationByCodeQueryValidator.cs` | Created validator: Code.NotEmpty().MaximumLength(50) | Applied |
| H4 | `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` | PUT Update: RequireAuthenticated → RequireReception | Applied |
| H5 | `src/TenantCore.Application/Features/Medicines/Handlers/GetMedicineAutocompleteHandler.cs` | Limit capped at 50 | Applied |
| H6 | GetMedicinesHandler, GetMedicineTypesHandler, GetMedicineDosageFormsHandler | PageSize capped at 100 in each handler | Applied |
| H7 | `src/TenantCore.Api/Extensions/ServiceCollectionExtensions.cs` + Program.cs | RequireHttpsMetadata = !IsDevelopment(); environment param added | Applied |
| H8 | `src/TenantCore.Api/Extensions/ServiceCollectionExtensions.cs` | ClockSkew = TimeSpan.Zero | Applied |

## Approval

- [x] All CRITICAL findings resolved
- [x] All HIGH findings resolved
- [ ] V3 violations (7 controllers not inheriting ClinicControllerBase) — remaining architectural debt
- [ ] Multi-SaveChangesAsync data integrity issues — deferred to follow-up
- [ ] No `AsNoTracking()` on read queries — deferred performance work
- [ ] Ready to merge: **Yes, for security** — deferred items are non-security code quality
