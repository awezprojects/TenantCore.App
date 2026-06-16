# Feature Plan: LMP / EDD Pregnancy Tenure Behaviour

**Repo:** TenantCore.App
**Date:** 2026-06-16
**Domain area:** Obstetrics / Pregnancy Tenure
**Status:** Approved — ready for execution

---

## Overview

This feature introduces a `PregnancyTenure` lifecycle entity that is created automatically when a doctor first sets an LMP date on an obstetric prescription and is closed by the doctor once the pregnancy outcome is known. Each tenure tracks the patient's LMP, the calculated EDD (by LMP and optionally by USG), the active/closed status, and the final outcome (Delivered, Abortion, or NotKnown). When a tenure's effective EDD date passes without being closed, it surfaces in a dedicated "EDD Overdue Patients" tab accessible to both doctors and receptionists. The system blocks doctors from setting a new LMP on a patient who has an open overdue tenure, enforcing a close-first workflow via an inline popup that updates state immediately without a full page refresh.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Domain | New `PregnancyTenure` entity + `IPregnancyTenureRepository` interface |
| Infrastructure | EF Fluent config, repository implementation, EF migration |
| Application | 3 commands/queries, 3 handlers, 1 validator, 1 translator; 2 existing handlers modified |
| API | New `PregnancyTenuresController` with 3 endpoints |
| Shared | 2 DTOs, 1 request model, 2 enums |
| Blazor Client | 1 typed client (interface + impl), 1 new page, 1 new modal component; 3 existing files modified |

---

## Entity: `PregnancyTenure`

**Tenant-scoped:** Yes
**Base class:** `AuditableEntity` (knowing who opened and closed a pregnancy tenure is clinically significant)

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, auto-generated |
| PatientId | Guid | FK → Patient, required |
| ApplicationId | Guid | FK → clinic (tenant scope), required |
| Lmp | DateOnly | required — the LMP date that opened this tenure |
| EddByLmp | DateOnly | required — computed as Lmp + 280 days at creation and on LMP update |
| EddByUsg | DateOnly? | nullable — synced from ObstetricPrescriptionData when doctor sets USG EDD |
| Status | PregnancyTenureStatus | required — Active = 1 (default), Closed = 2 |
| Outcome | PregnancyOutcome? | nullable — set only on close: Delivered = 1, Abortion = 2, NotKnown = 3 |
| ClosedAt | DateTime? | nullable — UTC timestamp set when closed |
| Notes | string? | nullable — doctor's notes entered at close, max 1000 chars |
| CreatedAt | DateTime | set by EF |
| UpdatedAt | DateTime | set by EF |
| Patient | Patient | navigation property — no loading hint per ADR-002, populated via Include in repo queries |

**Effective EDD rule (used throughout for overdue detection):** `EddByUsg ?? EddByLmp`. The USG date takes precedence when set.

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Enums/PregnancyTenureStatus.cs` | `Active = 1`, `Closed = 2` — status of the tenure lifecycle |
| `Enums/PregnancyOutcome.cs` | `Delivered = 1`, `Abortion = 2`, `NotKnown = 3` — outcome recorded on closure |
| `Dtos/PregnancyTenureDto.cs` | Full read DTO — all tenure fields plus a computed `EffectiveEdd` (EddByUsg ?? EddByLmp). Returned by get-active and close endpoints. |
| `Dtos/PregnancyTenureSummaryDto.cs` | Lean list DTO for the overdue tab — includes `TenureId`, `PatientId`, `PatientMrNumber`, `PatientFullName`, `Lmp`, `EddByLmp`, `EddByUsg`, `EffectiveEdd`, `DaysOverdue` (int, computed by translator). Used only for the overdue list response. |
| `Dtos/ClosePregnancyTenureRequest.cs` | POST body for the close endpoint — `Outcome` (PregnancyOutcome, required), `Notes` (string?, optional, max 1000) |

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Entities/PregnancyTenure.cs` | Domain entity as described in the entity table above. Inherits `AuditableEntity`. No EF Data Annotations. |
| `Interfaces/IPregnancyTenureRepository.cs` | Extends `IRepository<PregnancyTenure>`. Adds: `GetActiveForPatientAsync(Guid patientId, Guid applicationId, CancellationToken ct)` → `Task<PregnancyTenure?>` and `GetAllOverdueAsync(Guid applicationId, DateOnly today, CancellationToken ct)` → `Task<IEnumerable<PregnancyTenure>>` (includes Patient via EF Include). |

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Persistence/Configurations/PregnancyTenureConfiguration.cs` | Fluent API config — PK, required fields, max-length for Notes (1000), index on `(ApplicationId, PatientId, Status)` for fast active-tenure lookups, index on `(ApplicationId, Status)` for overdue queries, FK to Patient. Configures `EddByUsg` and `Outcome` as nullable. Configures `Status` with a default value of `1` (Active). Configures `Patient` navigation for Include queries. |
| `Repositories/PregnancyTenureRepository.cs` | Implements `IPregnancyTenureRepository` — extends `ClinicRepository<PregnancyTenure>`. `GetActiveForPatientAsync` filters by `patientId`, `applicationId`, and `Status == Active`. `GetAllOverdueAsync` filters by `applicationId`, `Status == Active`, and effective EDD < `today` (checks `EddByUsg < today` when not null, else `EddByLmp < today`), then orders by effective EDD ascending; uses `Include(t => t.Patient)`. All queries use `AsNoTracking()`. |

### Application Layer (`src/TenantCore.Application/Features/PregnancyTenures/`)

| File | Purpose |
|------|---------|
| `Commands/ClosePregnancyTenureCommand.cs` | Sealed record — carries `TenureId` (Guid), `Request` (ClosePregnancyTenureRequest), `ApplicationId` (Guid). Returns `PregnancyTenureDto`. |
| `Queries/GetOverdueEddPatientsQuery.cs` | Sealed record — carries `ApplicationId` (Guid). Returns `IEnumerable<PregnancyTenureSummaryDto>`. |
| `Queries/GetActivePregnancyTenureForPatientQuery.cs` | Sealed record — carries `PatientId` (Guid), `ApplicationId` (Guid). Returns `PregnancyTenureDto?`. |
| `Handlers/ClosePregnancyTenureHandler.cs` | Loads tenure by Id, verifies `tenure.ApplicationId == request.ApplicationId` (throws `NotFoundException` otherwise), verifies `tenure.Status == Active` (throws `InvalidOperationException` if already closed), sets `Status = Closed`, `Outcome`, `ClosedAt = DateTime.UtcNow`, `Notes`, calls `Update` + `SaveChangesAsync`, returns mapped `PregnancyTenureDto`. |
| `Handlers/GetOverdueEddPatientsHandler.cs` | Calls `GetAllOverdueAsync(applicationId, DateOnly.FromDateTime(DateTime.UtcNow))`, maps to `IEnumerable<PregnancyTenureSummaryDto>` via translator. |
| `Handlers/GetActivePregnancyTenureForPatientHandler.cs` | Calls `GetActiveForPatientAsync(patientId, applicationId)`, maps to `PregnancyTenureDto?` via translator (returns null if no active tenure). |
| `Validators/ClosePregnancyTenureCommandValidator.cs` | `TenureId` NotEmpty; `ApplicationId` NotEmpty; `Request.Outcome` must be a valid `PregnancyOutcome` enum value; `Request.Notes` MaximumLength(1000) when provided. |
| `Translators/PregnancyTenureTranslator.cs` | Static class. `ToDto(PregnancyTenure)` → `PregnancyTenureDto` (computes `EffectiveEdd = EddByUsg ?? EddByLmp`). `ToSummaryDto(PregnancyTenure)` → `PregnancyTenureSummaryDto` (maps `Patient.MrNumber` + `Patient.FirstName + Patient.LastName`; computes `DaysOverdue = (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - effectiveEdd.DayNumber)`, min 0). |

### API Layer (`src/TenantCore.Api/Controllers/`)

| File | Purpose |
|------|---------|
| `PregnancyTenuresController.cs` | Inherits `ClinicControllerBase`. Class-level: `[Authorize(Policy = AuthPolicies.RequireAuthenticated)]`. Three endpoints as detailed below. |

### Blazor Client (`src/TenantCore.Web.Client/`)

| File | Purpose |
|------|---------|
| `Clients/IPregnancyTenureApiClient.cs` | Interface — declares `GetActiveForPatientAsync(Guid patientId)`, `GetOverdueListAsync()`, `CloseTenureAsync(Guid tenureId, ClosePregnancyTenureRequest request)`. |
| `Clients/PregnancyTenureApiClient.cs` | Implementation of `IPregnancyTenureApiClient`. Follows same pattern as `ObstetricApiClient` — `SetAuth()` helper, `Ok<T>()` helper, `Fail<T>()` helper. Calls `api/pregnancy-tenures/*` endpoints. |
| `Pages/Patients/OverdueEddPatients.razor` | Routable page (`@page "/patients/edd-overdue"`). Loads and displays the overdue list in a styled table (MR Number, Patient Name, LMP, EDD by LMP, EDD by USG, Effective EDD, Days Overdue). Accessible to both doctors and receptionists. Uses `OnInitializedAsync` (standard `AuthorizedLayout`). |
| `Components/CloseTenureModal.razor` | Inline modal dialog component. Parameters: `TenureId` (Guid), `IsVisible` (bool), `OnClosed` (EventCallback<PregnancyTenureDto>) — fires after successful close. Contains a dropdown for `Outcome` (Delivered / Abortion / Not Known) and a textarea for `Notes`. On submit, calls `IPregnancyTenureApiClient.CloseTenureAsync` and fires `OnClosed`. Shows validation error inline if the API call fails. |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add `public DbSet<PregnancyTenure> PregnancyTenures => Set<PregnancyTenure>();` |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `services.AddScoped<IPregnancyTenureRepository, PregnancyTenureRepository>();` |
| `src/TenantCore.Application/Features/Obstetrics/Handlers/SetObstetricLmpHandler.cs` | Inject `IPregnancyTenureRepository`. After setting LMP on obstetric data: (1) load patient ID from `prescription.PatientId`, (2) load active tenure via `GetActiveForPatientAsync`, (3) if active tenure exists and effective EDD < today → throw `InvalidOperationException("Patient has an active pregnancy tenure past its EDD. Close the previous tenure before setting a new LMP.")`, (4) if active tenure exists and EDD not yet passed → update `tenure.Lmp`, `tenure.EddByLmp`, call `Update` + `SaveChangesAsync`, (5) if no active tenure → create new `PregnancyTenure` with `Status = Active` and `SaveChangesAsync`. |
| `src/TenantCore.Application/Features/Obstetrics/Handlers/SetObstetricEddByUsgHandler.cs` | Inject `IPregnancyTenureRepository`. After updating `obstetricData.EddByUsg`: load `prescription.PatientId`, get active tenure via `GetActiveForPatientAsync`; if active tenure exists, set `tenure.EddByUsg = request.Request.EddByUsg`, call `Update` (tenure is updated in the same `SaveChangesAsync` call already at the end of the handler). |
| `src/TenantCore.Web.Client/Components/PrescriptionObstetricDatesSection.razor` | Add parameter `PatientId` (Guid). Inject `IPregnancyTenureApiClient`. On `OnInitializedAsync` (or `OnParametersSetAsync`), load `_activeTenure` via `GetActiveForPatientAsync(PatientId)`. Compute `_hasOverdueTenure = _activeTenure != null && _activeTenure.EffectiveEdd < DateOnly.FromDateTime(DateTime.Today)`. Show "Close Tenure" button only when `_hasOverdueTenure`. Disable "Save LMP" button and show explanation message when `_hasOverdueTenure`. Embed `<CloseTenureModal>` with `IsVisible=_showCloseModal`, `TenureId=_activeTenure.Id`, `OnClosed=HandleTenureClosed`. `HandleTenureClosed` sets `_activeTenure` to the returned closed DTO, recalculates `_hasOverdueTenure = false`, hides the modal — no page reload. |
| `src/TenantCore.Web.Client/Program.cs` | Register `builder.Services.AddScoped<IPregnancyTenureApiClient, PregnancyTenureApiClient>();` with the appropriate `HttpClient` factory entry. |
| `src/TenantCore.Web.Client/Layout/NavMenu.razor` | Add navigation link "EDD Overdue" pointing to `/patients/edd-overdue`. |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| GET | `api/pregnancy-tenures/overdue` | — | `IEnumerable<PregnancyTenureSummaryDto>` | `RequireAuthenticated` (doctors + receptionists) |
| GET | `api/pregnancy-tenures/patient/{patientId:guid}/active` | — | `PregnancyTenureDto` (200) or 404 | `RequireClinical` (doctors only) |
| POST | `api/pregnancy-tenures/{id:guid}/close` | `ClosePregnancyTenureRequest` | `PregnancyTenureDto` (200) | `RequireClinical` (doctors only) |

---

## Validation Rules

| Field | Rules |
|-------|-------|
| `TenureId` | NotEmpty |
| `ApplicationId` | NotEmpty — always required |
| `Request.Outcome` | Must be a valid `PregnancyOutcome` enum value (1, 2, or 3) |
| `Request.Notes` | Optional; MaximumLength(1000) when provided |

---

## Business Rules

Enforced in handlers — throw named domain exceptions on violation:

1. **Block new LMP when overdue tenure exists** — In `SetObstetricLmpHandler`, if the patient has an active `PregnancyTenure` whose effective EDD is in the past, throw `InvalidOperationException`. The middleware maps this to HTTP 409. The Blazor UI also guards against this by disabling the Save LMP button, but the server-side check is the authoritative enforcement.

2. **Cannot close an already-closed tenure** — In `ClosePregnancyTenureHandler`, if `tenure.Status == Closed`, throw `InvalidOperationException("This pregnancy tenure is already closed.")`.

3. **Tenant isolation on close** — In `ClosePregnancyTenureHandler`, if `tenure.ApplicationId != request.ApplicationId`, throw `NotFoundException` (same as the "not found" case — do not leak existence of other clinics' records).

4. **EddByUsg takes precedence for overdue detection** — Everywhere an effective EDD is compared to today (`GetAllOverdueAsync`, overdue check in `SetObstetricLmpHandler`, Blazor UI display), use `EddByUsg ?? EddByLmp`.

5. **LMP update on active non-overdue tenure** — If a doctor sets a new LMP on a prescription and the patient already has an active tenure whose EDD has not yet passed (e.g., correcting an earlier LMP entry), update the existing tenure rather than creating a second one. This prevents duplicate active tenures per patient.

6. **Only one active tenure per patient per clinic** — Enforced implicitly by rule 1 (you cannot set a new LMP, which is the only way to create a tenure, while an overdue active tenure exists) and rule 5 (if EDD not passed, the existing tenure is updated, not duplicated).

---

## Multi-Tenancy Checklist

- [x] `ApplicationId` property present on `PregnancyTenure` entity
- [x] `ApplicationId` passed in all commands and queries
- [x] Repository queries filter by `applicationId`
- [x] Controller uses `GetApplicationId()` from `ClinicControllerBase`
- [x] Blazor client sends `X-Application-Id` header via the `ClinicContextService` already wired into `HttpClient` handlers

---

## EF Migration

**Migration name:** `AddPregnancyTenure`

Run after all infrastructure files are created:
```
dotnet ef migrations add AddPregnancyTenure --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

---

## Implementation Order

Execute in this sequence to avoid compile errors:

1. `Shared/Enums/PregnancyTenureStatus.cs`
2. `Shared/Enums/PregnancyOutcome.cs`
3. `Shared/Dtos/PregnancyTenureDto.cs`
4. `Shared/Dtos/PregnancyTenureSummaryDto.cs`
5. `Shared/Dtos/ClosePregnancyTenureRequest.cs`
6. `Domain/Entities/PregnancyTenure.cs`
7. `Domain/Interfaces/IPregnancyTenureRepository.cs`
8. `Infrastructure/Persistence/Configurations/PregnancyTenureConfiguration.cs`
9. `Infrastructure/Repositories/PregnancyTenureRepository.cs`
10. `Infrastructure/Persistence/ClinicDbContext.cs` — add DbSet
11. `Infrastructure/DependencyInjection.cs` — register repository
12. `Application/Features/PregnancyTenures/Commands/ClosePregnancyTenureCommand.cs`
13. `Application/Features/PregnancyTenures/Queries/GetOverdueEddPatientsQuery.cs`
14. `Application/Features/PregnancyTenures/Queries/GetActivePregnancyTenureForPatientQuery.cs`
15. `Application/Features/PregnancyTenures/Validators/ClosePregnancyTenureCommandValidator.cs`
16. `Application/Features/PregnancyTenures/Translators/PregnancyTenureTranslator.cs`
17. `Application/Features/PregnancyTenures/Handlers/ClosePregnancyTenureHandler.cs`
18. `Application/Features/PregnancyTenures/Handlers/GetOverdueEddPatientsHandler.cs`
19. `Application/Features/PregnancyTenures/Handlers/GetActivePregnancyTenureForPatientHandler.cs`
20. `Application/Features/Obstetrics/Handlers/SetObstetricLmpHandler.cs` — add tenure logic (inject `IPregnancyTenureRepository`, block/create/update)
21. `Application/Features/Obstetrics/Handlers/SetObstetricEddByUsgHandler.cs` — add tenure EDD sync
22. `Api/Controllers/PregnancyTenuresController.cs`
23. Run EF migration (`AddPregnancyTenure`)
24. `Web.Client/Clients/IPregnancyTenureApiClient.cs`
25. `Web.Client/Clients/PregnancyTenureApiClient.cs`
26. `Web.Client/Components/CloseTenureModal.razor`
27. `Web.Client/Pages/Patients/OverdueEddPatients.razor`
28. `Web.Client/Components/PrescriptionObstetricDatesSection.razor` — add PatientId param, tenure loading, close button
29. `Web.Client/Program.cs` — register `PregnancyTenureApiClient`
30. `Web.Client/Layout/NavMenu.razor` — add "EDD Overdue" link

---

## Test Coverage Required

| Test class | What it tests |
|-----------|--------------|
| `ClosePregnancyTenureHandlerTests` | Happy path: sets Status=Closed, Outcome, ClosedAt, Notes, returns DTO; throws InvalidOperationException when already closed; throws NotFoundException when wrong ApplicationId |
| `GetOverdueEddPatientsHandlerTests` | Calls repo with today's date; maps results to summary DTOs; returns empty list when none overdue |
| `GetActivePregnancyTenureForPatientHandlerTests` | Returns null when no active tenure; returns mapped DTO when active tenure found |
| `SetObstetricLmpHandlerTests` | Blocks with InvalidOperationException when active tenure has passed EDD; creates new tenure when none exists; updates existing tenure when EDD not yet passed |
| `ClosePregnancyTenureCommandValidatorTests` | TenureId empty fails; ApplicationId empty fails; invalid Outcome enum value fails; Notes over 1000 chars fails; valid command passes |
| `PregnancyTenureTranslatorTests` | ToDto: EffectiveEdd uses EddByUsg when set; falls back to EddByLmp when EddByUsg is null; ToSummaryDto: DaysOverdue computed correctly |

---

## Open Questions / Risks

- **Prescription.PatientId assumption** — `SetObstetricLmpHandler` and `SetObstetricEddByUsgHandler` need `prescription.PatientId` to look up the tenure. This is assumed to exist on the `Prescription` entity (it is passed into `Prescription.Create(...)` in `CreatePrescriptionHandler`). The executor must verify this property is accessible on the loaded `Prescription` entity.

- **Existing obstetric data without a tenure** — Patients who already have LMP set on historical prescriptions (from the `obstetric-lmp-usg-template` feature) will have no corresponding `PregnancyTenure`. These patients will silently miss the overdue tab. A data backfill migration could be considered but is out of scope for this plan — the feature applies to LMP set from this point forward.

- **`PrescriptionObstetricDatesSection` parent callers** — Adding `PatientId` as a required `[Parameter]` will cause a compile error in any parent component that does not pass it. The executor must locate all callers of this component and add the parameter.
