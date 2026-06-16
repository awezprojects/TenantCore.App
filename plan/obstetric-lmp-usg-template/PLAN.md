# Feature Plan: Obstetric LMP & USG Template Management

**Repo:** TenantCore.App
**Date:** 2026-06-16
**Domain area:** Obstetric / UsgTemplates
**Status:** Approved — ready for execution

---

## Overview

This feature enables doctors to record and update the Last Menstrual Period (LMP) and EDD-by-USG for obstetric patients directly within the prescription flow. From the LMP, the system auto-computes EDD-by-LMP (LMP + 280 days) and stores it alongside. Doctors can independently record EDD-by-USG once a scan confirms a different estimated delivery date, giving the clinic a clear view of both the LMP-derived estimate and the scan-confirmed date. Clinic admins can customize the 11-row USG appointment schedule template for their clinic; the system ships with a static default template (ported from the legacy `CalculateUSGdate` function) that clinics start from and can override. An obstetric summary section — showing LMP, both EDD values, and the full calculated appointment schedule — is always rendered on the patient detail page when the patient has an obstetric prescription with LMP set. Doctors can also print the USG chart separately via a dedicated print page with a proper clinical heading. The LMP/EDD functionality is gated to prescriptions that have `ObstetricPrescriptionData` attached, naturally restricting it to obstetric prescriptions only.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Domain | 2 new entities (ClinicUsgTemplate, UsgTemplateRow); 1 modified entity (ObstetricPrescriptionData); 1 new + 1 modified repository interface |
| Infrastructure | 2 new EF configurations; 1 new repository; 2 modified files (existing EF config + existing repository); ClinicDbContext + DI updated |
| Application | 2 new feature folders (Obstetric, UsgTemplates); 6 commands/queries; 8 handlers; 3 validators; 2 translators; 2 static helpers |
| API | 2 new controllers (ObstetricController, UsgTemplatesController) |
| Shared | 8 new DTOs/request records; 1 modified DTO |
| Blazor Client | 2 new typed clients; 4 new pages/components; patient detail page modified to embed obstetric section |

---

## Domain: New Entities

### Entity: `ClinicUsgTemplate`

**File:** `src/TenantCore.Domain/Entities/ClinicUsgTemplate.cs`
**Tenant-scoped:** Yes
**Base class:** BaseEntity

One record per clinic. Tracks whether the clinic has overridden the system defaults.

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, auto-generated |
| ApplicationId | Guid | Required — unique per clinic |
| IsCustomized | bool | Default false; set to true once clinic saves custom rows |
| CreatedAt | DateTime | Set on creation |
| UpdatedAt | DateTime | Set on update |
| RowVersion | byte[] | Optimistic concurrency |

Navigation property (loaded via `.Include()`):
- `ICollection<UsgTemplateRow> Rows` — cascade-deleted when template is deleted

---

### Entity: `UsgTemplateRow`

**File:** `src/TenantCore.Domain/Entities/UsgTemplateRow.cs`
**Tenant-scoped:** No (isolated via parent ClinicUsgTemplate)
**Base class:** BaseEntity

Each row represents one milestone in the USG appointment schedule.

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, auto-generated |
| ClinicUsgTemplateId | Guid | FK → ClinicUsgTemplate, required |
| RowOrder | int | Required — display order (1..N) |
| WeekLabel | string | Required, max 50 — e.g., "8 weeks" |
| LmpDayOffset | int | Required — days from LMP, e.g., 56 |
| Activity | string | Required, max 500 — e.g., "USG" |
| Indication | string | Required, max 500 — e.g., "Confirm Viability" |
| CreatedAt | DateTime | Set on creation |
| UpdatedAt | DateTime | Set on update |
| RowVersion | byte[] | Optimistic concurrency |

---

## Domain: Modified Entity

### `ObstetricPrescriptionData` — add 3 fields

**File:** `src/TenantCore.Domain/Entities/ObstetricPrescriptionData.cs`

Add the following properties (private setters, matching existing style):

| Property | Type | Constraints |
|----------|------|-------------|
| Lmp | DateTime? | Nullable; represents date-only (time stored as midnight UTC); set by doctor |
| EddByLmp | DateTime? | Nullable; auto-computed as Lmp + 280 days whenever LMP is written; never set independently |
| EddByUsg | DateTime? | Nullable; doctor-entered after reviewing a scan; independent of Lmp |

**Update rules for the entity class:**

- Add `public void SetLmp(DateTime lmp)` — sets `Lmp = lmp` and `EddByLmp = lmp.AddDays(280)` together; calls `SetUpdatedAt()`
- Add `public void SetEddByUsg(DateTime edd)` — sets `EddByUsg = edd`; calls `SetUpdatedAt()`
- Update the `Update(UpsertObstetricPrescriptionDataDto dto)` method to also call `SetLmp` if `dto.Lmp` is provided, and `SetEddByUsg` if `dto.EddByUsg` is provided
- Update the `CreateOrUpdate(...)` factory to populate `Lmp`, `EddByLmp`, and `EddByUsg` from the DTO when set

---

## Repository Interfaces

### New: `IClinicUsgTemplateRepository`

**File:** `src/TenantCore.Domain/Interfaces/IClinicUsgTemplateRepository.cs`

Extends `IRepository<ClinicUsgTemplate>`. Additional methods:

| Method | Returns | Purpose |
|--------|---------|---------|
| `GetByApplicationIdAsync(Guid applicationId)` | `Task<ClinicUsgTemplate?>` | Lightweight existence check — no rows loaded |
| `GetByApplicationIdWithRowsAsync(Guid applicationId)` | `Task<ClinicUsgTemplate?>` | Loads template + all its rows for read/update |
| `RemoveRows(IEnumerable<UsgTemplateRow> rows)` | `void` | Marks row entities for deletion from change tracker |

---

### Modified: `IObstetricPrescriptionDataRepository`

**File:** `src/TenantCore.Domain/Interfaces/IObstetricPrescriptionDataRepository.cs`

Add one method to the existing interface:

| Method | Returns | Purpose |
|--------|---------|---------|
| `GetMostRecentWithLmpByPatientIdAsync(Guid patientId, Guid applicationId)` | `Task<ObstetricPrescriptionData?>` | Joins through Prescription to find the most recent obstetric record where Lmp is not null, for this patient in this clinic; ordered by Prescription.PrescribedDate descending |

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/Dtos/`)

| File | Purpose |
|------|---------|
| `ObstetricDatesDto.cs` | Read response — Id, PrescriptionId, Lmp, EddByLmp, EddByUsg |
| `UsgChartDto.cs` | Full chart response — PatientId, Lmp, EddByLmp, EddByUsg, list of UsgChartRowDto |
| `UsgChartRowDto.cs` | One calculated row — RowOrder, WeekLabel, Date (calculated), DayOfWeek (string), Activity, Indication |
| `ClinicUsgTemplateDto.cs` | Template read response — ApplicationId, IsCustomized, list of UsgTemplateRowDto |
| `UsgTemplateRowDto.cs` | One template row — RowOrder, WeekLabel, LmpDayOffset, Activity, Indication |
| `SetLmpRequest.cs` | PUT body — Lmp (DateTime) |
| `SetEddByUsgRequest.cs` | PUT body — EddByUsg (DateTime) |
| `UpsertClinicUsgTemplateRequest.cs` | PUT body — Rows (List of UsgTemplateRowDto) |

---

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Entities/ClinicUsgTemplate.cs` | New entity — clinic-level USG template container |
| `Entities/UsgTemplateRow.cs` | New entity — one row of the USG appointment schedule |
| `Interfaces/IClinicUsgTemplateRepository.cs` | Repository contract with 3 specialized methods |

---

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Persistence/Configurations/Clinic/ClinicUsgTemplateConfiguration.cs` | Fluent API — table `clinic.ClinicUsgTemplates`; unique index on ApplicationId; HasMany Rows with cascade delete |
| `Persistence/Configurations/Clinic/UsgTemplateRowConfiguration.cs` | Fluent API — table `clinic.UsgTemplateRows`; FK to ClinicUsgTemplateId; max-length constraints on WeekLabel, Activity, Indication |
| `Repositories/ClinicUsgTemplateRepository.cs` | Implements IClinicUsgTemplateRepository — uses `.Include(t => t.Rows)` for row-loading methods; `RemoveRows` calls `_dbSet` equivalent via `_context` |

---

### Application Layer — Feature: `Obstetric`

**Base path:** `src/TenantCore.Application/Features/Obstetric/`

| File | Purpose |
|------|---------|
| `Commands/SetObstetricDatesCommand.cs` | Command: PrescriptionId (Guid), Lmp (DateTime), ApplicationId (Guid) → returns ObstetricDatesDto |
| `Commands/UpdateEddByUsgCommand.cs` | Command: PrescriptionId (Guid), EddByUsg (DateTime), ApplicationId (Guid) → returns ObstetricDatesDto |
| `Queries/GetObstetricDatesQuery.cs` | Query: PrescriptionId (Guid), ApplicationId (Guid) → returns ObstetricDatesDto? |
| `Queries/GetUsgChartForPatientQuery.cs` | Query: PatientId (Guid), ApplicationId (Guid) → returns UsgChartDto? (null when no LMP on record) |
| `Handlers/SetObstetricDatesHandler.cs` | Gets ObstetricPrescriptionData by PrescriptionId; throws EntityNotFoundException if not found (obstetric gate); calls entity.SetLmp(lmp); saves; returns mapped dto |
| `Handlers/UpdateEddByUsgHandler.cs` | Gets ObstetricPrescriptionData; throws EntityNotFoundException if not found; throws InvalidOperationException if Lmp is null (LMP must be set first); calls entity.SetEddByUsg; saves; returns dto |
| `Handlers/GetObstetricDatesHandler.cs` | Gets ObstetricPrescriptionData by PrescriptionId; returns null (not 404) if not found |
| `Handlers/GetUsgChartForPatientHandler.cs` | Calls GetMostRecentWithLmpByPatientIdAsync; returns null if none found; loads clinic template (or defaults); calls UsgDateCalculator.CalculateSchedule; maps and returns UsgChartDto |
| `Validators/SetObstetricDatesCommandValidator.cs` | PrescriptionId NotEmpty; Lmp NotEmpty; Lmp must be ≤ today; ApplicationId NotEmpty |
| `Validators/UpdateEddByUsgCommandValidator.cs` | PrescriptionId NotEmpty; EddByUsg NotEmpty; ApplicationId NotEmpty |
| `Translators/ObstetricTranslator.cs` | Static class — ToObstetricDatesDto(ObstetricPrescriptionData), ToUsgChartDto(ObstetricPrescriptionData, IEnumerable of calculated rows), ToUsgChartRowDto |
| `Helpers/UsgDateCalculator.cs` | Static class — CalculateSchedule(DateTime lmp, IEnumerable<UsgTemplateRow> rows) → IEnumerable of (rowOrder, weekLabel, calculatedDate, dayOfWeekName, activity, indication); for each row: rawDate = lmp.AddDays(offset); if rawDate.DayOfWeek == Sunday → rawDate = rawDate.AddDays(1) |

---

### Application Layer — Feature: `UsgTemplates`

**Base path:** `src/TenantCore.Application/Features/UsgTemplates/`

| File | Purpose |
|------|---------|
| `Commands/UpsertClinicUsgTemplateCommand.cs` | Command: ApplicationId (Guid), Rows (list of row data) → returns Guid (template Id) |
| `Commands/ResetClinicUsgTemplateCommand.cs` | Command: ApplicationId (Guid) → IRequest (void) |
| `Queries/GetClinicUsgTemplateQuery.cs` | Query: ApplicationId (Guid) → ClinicUsgTemplateDto; returns clinic custom rows when IsCustomized, else system defaults |
| `Queries/GetDefaultUsgTemplateQuery.cs` | Query: no parameters → ClinicUsgTemplateDto; always returns DefaultUsgTemplateDefinition rows |
| `Handlers/UpsertClinicUsgTemplateHandler.cs` | Gets or creates ClinicUsgTemplate for clinic; calls RemoveRows on existing rows; inserts new rows from command; sets IsCustomized = true; saves; returns template Id |
| `Handlers/ResetClinicUsgTemplateHandler.cs` | Gets template for clinic (null is not an error — nothing to reset); if found, removes all rows; sets IsCustomized = false; saves |
| `Handlers/GetClinicUsgTemplateHandler.cs` | Gets template with rows; if not found or not IsCustomized, maps DefaultUsgTemplateDefinition.Rows; else maps stored rows; returns ClinicUsgTemplateDto |
| `Handlers/GetDefaultUsgTemplateHandler.cs` | Maps DefaultUsgTemplateDefinition.Rows to ClinicUsgTemplateDto (IsCustomized = false, ApplicationId = empty Guid) |
| `Validators/UpsertClinicUsgTemplateCommandValidator.cs` | ApplicationId NotEmpty; Rows not empty; each row: WeekLabel NotEmpty + MaxLength(50), LmpDayOffset >= 0, Activity NotEmpty + MaxLength(500), Indication NotEmpty + MaxLength(500) |
| `Translators/UsgTemplateTranslator.cs` | Static class — ToClinicUsgTemplateDto(ClinicUsgTemplate, IEnumerable<UsgTemplateRow>); ToUsgTemplateRowDto(UsgTemplateRow); ToUsgTemplateRow(UsgTemplateRowDto, Guid templateId) |
| `Helpers/DefaultUsgTemplateDefinition.cs` | Static class with a static readonly list of 11 system-default rows (see Default Row Table below) |

---

### API Layer (`src/TenantCore.Api/Controllers/`)

| File | Purpose |
|------|---------|
| `ObstetricController.cs` | Inherits ClinicControllerBase — 4 endpoints for LMP, EDD-by-USG, and chart retrieval |
| `UsgTemplatesController.cs` | Inherits ClinicControllerBase — 4 endpoints for clinic template CRUD |

---

### Blazor Client (`src/TenantCore.Web.Client/`)

| File | Purpose |
|------|---------|
| `Clients/ObstetricClient.cs` | Typed HTTP client — SetLmp, SetEddByUsg, GetObstetricDates, GetUsgChartForPatient |
| `Clients/UsgTemplateClient.cs` | Typed HTTP client — GetClinicTemplate, GetDefaultTemplate, UpsertTemplate, ResetTemplate |
| `Pages/Obstetric/UsgChartPage.razor` | Routable page at `/obstetric/patient/{PatientId:guid}/usg-chart` — shows patient name, LMP, EddByLmp, EddByUsg as a header strip; full 11-row table (Week / Date / Day / Things To Do / Indication); "Print" button navigates to the print page |
| `Pages/Obstetric/PrintUsgChart.razor` | Routable print page at `/print/usg-chart/{PatientId:guid}` — uses a minimal layout (no nav menu); renders clinic name as header, "USG Pregnancy Monitoring Chart" as title, patient name + MR number, LMP / EDD by LMP / EDD by USG summary row, full schedule table; CSS `@media print` hides everything except the chart |
| `Pages/Admin/UsgTemplatePage.razor` | Routable page at `/admin/usg-template` — ClinicAdmin only; shows current template rows in an editable table; "Save Custom Template" and "Reset to Default" buttons |
| `Components/Obstetric/ObstetricSummarySection.razor` | Parameter: `PatientId` (Guid) — on init, calls GetUsgChartForPatient; renders nothing if response is null; otherwise renders a summary strip (LMP, EddByLmp, EddByUsg) and the appointment schedule table inline; "View Full Chart" link navigates to UsgChartPage |
| `Components/Obstetric/PrescriptionObstetricDatesSection.razor` | Parameter: `PrescriptionId` (Guid) — on init, calls GetObstetricDates; renders nothing if response is null (non-obstetric prescription); if present, renders a labelled section showing LMP, EDD by LMP, and EDD by USG (displayed as "Not yet recorded" when null); embedded directly in the prescription detail page |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Domain/Entities/ObstetricPrescriptionData.cs` | Add Lmp, EddByLmp, EddByUsg (DateTime?) with private setters; add SetLmp() and SetEddByUsg() methods; update CreateOrUpdate factory and Update method |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/ObstetricPrescriptionDataConfiguration.cs` | Add Fluent API for Lmp, EddByLmp, EddByUsg (all nullable DateTime columns) |
| `src/TenantCore.Shared/Dtos/UpsertObstetricPrescriptionDataDto.cs` | Add `DateTime? Lmp` and `DateTime? EddByUsg` fields |
| `src/TenantCore.Domain/Interfaces/IObstetricPrescriptionDataRepository.cs` | Add GetMostRecentWithLmpByPatientIdAsync method signature |
| `src/TenantCore.Infrastructure/Repositories/ObstetricPrescriptionDataRepository.cs` | Implement GetMostRecentWithLmpByPatientIdAsync — join to `_context.Prescriptions` on PrescriptionId, filter by patientId and applicationId, where Lmp != null, order by PrescribedDate descending, return first |
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add `DbSet<ClinicUsgTemplate> ClinicUsgTemplates` and `DbSet<UsgTemplateRow> UsgTemplateRows` |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `IClinicUsgTemplateRepository → ClinicUsgTemplateRepository` as Scoped |
| Patient detail Blazor page (path to be confirmed during execute — check `src/TenantCore.Web.Client/Pages/Patients/`) | Embed `<ObstetricSummarySection PatientId="@patientId" />` in the patient detail layout, below existing patient data sections |
| Prescription detail Blazor page (path to be confirmed during execute — check `src/TenantCore.Web.Client/Pages/Prescriptions/`) | Embed `<PrescriptionObstetricDatesSection PrescriptionId="@prescriptionId" />` inside the prescription detail layout, above the medicines/items section; component self-hides when not an obstetric prescription |
| Prescription print/PDF (path to be confirmed during execute — check for Razor print template or HTML template used by IPdfConversionService) | Add an obstetric dates block (LMP / EDD by LMP / EDD by USG) to the prescription print output; only rendered when ObstetricDatesDto is non-null for the prescription |

---

## API Endpoints

### `ObstetricController` — route prefix: `api/obstetric`

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| GET | `api/obstetric/prescription/{prescriptionId:guid}` | — | `ObstetricDatesDto?` (200 or null body) | RequireAuthenticated |
| PUT | `api/obstetric/prescription/{prescriptionId:guid}/lmp` | `SetLmpRequest` | `ObstetricDatesDto` (200) | RequireClinical |
| PUT | `api/obstetric/prescription/{prescriptionId:guid}/edd-by-usg` | `SetEddByUsgRequest` | `ObstetricDatesDto` (200) | RequireClinical |
| GET | `api/obstetric/patient/{patientId:guid}/usg-chart` | — | `UsgChartDto?` (200 or null body) | RequireAuthenticated |

### `UsgTemplatesController` — route prefix: `api/usgtemplates`

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| GET | `api/usgtemplates` | — | `ClinicUsgTemplateDto` (200) | RequireAuthenticated |
| GET | `api/usgtemplates/default` | — | `ClinicUsgTemplateDto` (200) | RequireAuthenticated |
| PUT | `api/usgtemplates` | `UpsertClinicUsgTemplateRequest` | `Guid` (200) | RequireClinicAdmin |
| DELETE | `api/usgtemplates` | — | 204 No Content | RequireClinicAdmin |

---

## Validation Rules

### `SetObstetricDatesCommandValidator`

| Field | Rules |
|-------|-------|
| PrescriptionId | NotEmpty |
| Lmp | NotEmpty; must be ≤ DateTime.UtcNow.Date (LMP cannot be in the future) |
| ApplicationId | NotEmpty |

### `UpdateEddByUsgCommandValidator`

| Field | Rules |
|-------|-------|
| PrescriptionId | NotEmpty |
| EddByUsg | NotEmpty |
| ApplicationId | NotEmpty |

### `UpsertClinicUsgTemplateCommandValidator`

| Field | Rules |
|-------|-------|
| ApplicationId | NotEmpty |
| Rows | NotEmpty (must have at least 1 row) |
| Rows[].WeekLabel | NotEmpty; MaxLength(50) |
| Rows[].LmpDayOffset | GreaterThanOrEqualTo(0) |
| Rows[].Activity | NotEmpty; MaxLength(500) |
| Rows[].Indication | NotEmpty; MaxLength(500) |

---

## Business Rules

Enforced in handlers — throw domain exceptions on violation:

1. **Obstetric gate:** `SetObstetricDatesHandler` — if `ObstetricPrescriptionData` is not found for the given `prescriptionId`, throw `EntityNotFoundException("ObstetricPrescriptionData", prescriptionId)`. This prevents LMP from being set on non-obstetric prescriptions.

2. **EDD-by-USG requires LMP first:** `UpdateEddByUsgHandler` — if `ObstetricPrescriptionData.Lmp` is null when trying to set EddByUsg, throw `InvalidOperationException("Cannot set EDD by USG before LMP is recorded.")`.

3. **EddByLmp is always auto-computed:** `SetObstetricDatesHandler` calls `entity.SetLmp(lmp)` which internally sets `EddByLmp = lmp.AddDays(280)`. EddByLmp is never set directly via any command.

4. **Chart returns null gracefully:** `GetUsgChartForPatientHandler` — returns `null` (not an error) when no ObstetricPrescriptionData with LMP exists for the patient. The UI hides the obstetric section when the response is null.

5. **Template upsert replaces all rows:** `UpsertClinicUsgTemplateHandler` calls `repository.RemoveRows(existingRows)` then inserts the full new row set. No partial row updates.

6. **Reset clears customization flag:** `ResetClinicUsgTemplateHandler` — removes all `UsgTemplateRow` records and sets `IsCustomized = false`. After reset, `GetClinicUsgTemplateHandler` will serve the static defaults from code.

7. **Sunday shift rule:** `UsgDateCalculator.CalculateSchedule` — for each row, `rawDate = lmp.AddDays(offset)`; if `rawDate.DayOfWeek == DayOfWeek.Sunday` then `rawDate = rawDate.AddDays(1)`.

---

## Default USG Template — System Rows

**File:** `src/TenantCore.Application/Features/UsgTemplates/Helpers/DefaultUsgTemplateDefinition.cs`

Static readonly list of 11 rows. This is the authoritative source for the default schedule; it is never stored in the database.

| RowOrder | WeekLabel | LmpDayOffset | Activity | Indication |
|:--------:|-----------|:------------:|----------|------------|
| 1 | 8 weeks | 56 | USG | Confirm Viability |
| 2 | 11 weeks | 77 | Blood Test — B-HCG & PAPP-A | Screening for Trisomies |
| 3 | 12 weeks 3 days | 87 | USG | NT Scan |
| 4 | 19 weeks | 133 | USG | Anomaly Scan & Colour Doppler |
| 5 | 23 weeks | 161 | USG | Fetal Echo (If Indicated) |
| 6 | 23 weeks | 161 | GTT | 75g glucose test for GDM |
| 7 | 26 weeks | 182 | Influenza Vaccination | Protection of mother & child up to 6 months |
| 8 | 30 weeks | 210 | USG | Growth Scan & Colour Doppler |
| 9 | 32 weeks | 224 | GTT | 75g glucose test for GDM (repeat) |
| 10 | 32 weeks | 224 | Inj Tdap | Protect newborn from whooping cough & tetanus |
| 11 | 35 weeks | 245 | USG | Growth Scan, Colour Doppler & Liquor Assessment |

---

## Multi-Tenancy Checklist

- [ ] `ClinicUsgTemplate.ApplicationId` present — one template record per clinic, unique index enforced
- [ ] `UsgTemplateRow` isolated via parent `ClinicUsgTemplate.ApplicationId` (no own ApplicationId needed)
- [ ] `ObstetricPrescriptionData` tenant isolation maintained through `PrescriptionId → Prescription.ApplicationId`
- [ ] `GetMostRecentWithLmpByPatientIdAsync` filters by both `patientId` and `applicationId` via join to `Prescriptions`
- [ ] All new commands carry `ApplicationId`; handlers pass it to repository methods
- [ ] Both new controllers use `GetApplicationId()` from `ClinicControllerBase`
- [ ] `ClinicUsgTemplateConfiguration` enforces unique index on `ApplicationId`

---

## EF Migration

**Migration name:** `AddObstetricLmpAndUsgTemplate`

Run after all infrastructure changes are in place (entities configured, DbContext updated):

```
dotnet ef migrations add AddObstetricLmpAndUsgTemplate --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

This migration adds:
- Columns `Lmp`, `EddByLmp`, `EddByUsg` (nullable datetime2) to `clinic.ObstetricPrescriptionData`
- Table `clinic.ClinicUsgTemplates` (Id, ApplicationId, IsCustomized, CreatedAt, UpdatedAt, RowVersion)
- Table `clinic.UsgTemplateRows` (Id, ClinicUsgTemplateId, RowOrder, WeekLabel, LmpDayOffset, Activity, Indication, CreatedAt, UpdatedAt, RowVersion)
- Unique index on `clinic.ClinicUsgTemplates.ApplicationId`
- FK from `clinic.UsgTemplateRows.ClinicUsgTemplateId` → `clinic.ClinicUsgTemplates.Id` (cascade delete)

---

## Implementation Order

Execute in this exact sequence to avoid compile errors:

1. Shared DTOs — ObstetricDatesDto, UsgChartDto, UsgChartRowDto, ClinicUsgTemplateDto, UsgTemplateRowDto, SetLmpRequest, SetEddByUsgRequest, UpsertClinicUsgTemplateRequest
2. Modify `UpsertObstetricPrescriptionDataDto` — add Lmp and EddByUsg fields
3. Domain entity — `ClinicUsgTemplate.cs`
4. Domain entity — `UsgTemplateRow.cs`
5. Modify `ObstetricPrescriptionData` — add Lmp, EddByLmp, EddByUsg + SetLmp(), SetEddByUsg() methods; update CreateOrUpdate factory and Update method
6. Domain interface — `IClinicUsgTemplateRepository.cs`
7. Modify `IObstetricPrescriptionDataRepository` — add GetMostRecentWithLmpByPatientIdAsync signature
8. Infrastructure EF config — `ClinicUsgTemplateConfiguration.cs`
9. Infrastructure EF config — `UsgTemplateRowConfiguration.cs`
10. Modify `ObstetricPrescriptionDataConfiguration` — add Lmp, EddByLmp, EddByUsg column mappings
11. Infrastructure repository — `ClinicUsgTemplateRepository.cs`
12. Modify `ObstetricPrescriptionDataRepository` — implement GetMostRecentWithLmpByPatientIdAsync
13. Modify `ClinicDbContext` — add DbSet<ClinicUsgTemplate> and DbSet<UsgTemplateRow>
14. Modify Infrastructure `DependencyInjection.cs` — register IClinicUsgTemplateRepository
15. Application helpers — `DefaultUsgTemplateDefinition.cs`, `UsgDateCalculator.cs`
16. Application commands — SetObstetricDatesCommand, UpdateEddByUsgCommand, UpsertClinicUsgTemplateCommand, ResetClinicUsgTemplateCommand
17. Application queries — GetObstetricDatesQuery, GetUsgChartForPatientQuery, GetClinicUsgTemplateQuery, GetDefaultUsgTemplateQuery
18. Application validators — SetObstetricDatesCommandValidator, UpdateEddByUsgCommandValidator, UpsertClinicUsgTemplateCommandValidator
19. Application translators — ObstetricTranslator, UsgTemplateTranslator
20. Application handlers — all 8 (write handlers before read handlers within each feature)
21. API controllers — ObstetricController, UsgTemplatesController
22. Run EF migration
23. Blazor typed clients — ObstetricClient, UsgTemplateClient
24. Blazor component — `Components/Obstetric/ObstetricSummarySection.razor`
25. Blazor component — `Components/Obstetric/PrescriptionObstetricDatesSection.razor`
26. Blazor pages — UsgChartPage, PrintUsgChart, UsgTemplatePage
27. Modify patient detail Blazor page — embed ObstetricSummarySection
28. Modify prescription detail Blazor page — embed PrescriptionObstetricDatesSection
29. Modify prescription print/PDF template — add obstetric dates block (guarded by null check)

---

## Test Coverage Required

| Test class | What it tests |
|-----------|--------------|
| `SetObstetricDatesHandlerTests` | Sets LMP + auto-computes EddByLmp correctly; throws EntityNotFoundException when no ObstetricPrescriptionData found; EddByLmp = LMP + 280 days |
| `UpdateEddByUsgHandlerTests` | Sets EddByUsg; throws InvalidOperationException when Lmp is null; throws EntityNotFoundException when record not found |
| `GetUsgChartForPatientHandlerTests` | Returns null when no LMP on record; uses clinic custom template when IsCustomized; falls back to defaults when not customized; chart has correct row count |
| `UpsertClinicUsgTemplateHandlerTests` | Creates template on first save; replaces all rows on re-save; sets IsCustomized = true |
| `ResetClinicUsgTemplateHandlerTests` | Clears rows; sets IsCustomized = false; is a no-op when no template exists |
| `UsgDateCalculatorTests` | Correct computed date for each of the 11 offsets; Sunday shifts forward by 1 day to Monday; non-Sunday dates are not shifted; EDD offset (280) is correct |
| `SetObstetricDatesCommandValidatorTests` | Future LMP fails validation; past/today LMP passes; empty ApplicationId fails |
| `UpsertClinicUsgTemplateCommandValidatorTests` | Empty Rows list fails; missing WeekLabel fails; negative offset fails; valid command passes |
| `ObstetricTranslatorTests` | ToObstetricDatesDto maps all three date fields correctly; null EddByUsg is preserved as null |
| `UsgTemplateTranslatorTests` | ToClinicUsgTemplateDto maps all row fields; row count matches source |

---

## Open Questions / Risks

1. **DateOnly vs DateTime** — All existing entities use `DateTime`. The LMP and EDD fields are date-only values. For consistency with the codebase, use `DateTime?` (time stored as midnight UTC). If `DateOnly` is found elsewhere in the project during execute, switch to `DateOnly?` instead. Check for `DateOnly` usage with a grep before writing the entity.

2. **Patient detail page exact path** — Confirm the exact `.razor` file path during execute by checking `src/TenantCore.Web.Client/Pages/Patients/`. The `ObstetricSummarySection` component must be embedded below the existing patient data, and the patient ID must be available as a route parameter or component parameter on that page.

3. **Print layout** — `PrintUsgChart.razor` requires a minimal layout (no nav, no header). Check `src/TenantCore.Web.Client/Layout/` for an existing print layout before creating one. If none exists, create `PrintLayout.razor` and reference it via `@layout PrintLayout` in the print page.

4. **ObstetricPrescriptionData existing handler impact** — The existing `UpsertObstetricPrescriptionDataDto` change (adding Lmp/EddByUsg) will affect any existing handler that calls `ObstetricPrescriptionData.CreateOrUpdate` or `.Update`. During execute, check all callers of these methods and ensure they still compile after the signature change. The fields are nullable, so existing callers may pass `null` without breaking.

5. **UsgTemplateRow lacks its own `IRepository` interface** — UsgTemplateRow is managed entirely through `IClinicUsgTemplateRepository`. Do not create a separate `IUsgTemplateRowRepository`. The `ClinicDbContext.UsgTemplateRows` DbSet is needed only for EF to track the entity; direct access is via the parent template repository.
