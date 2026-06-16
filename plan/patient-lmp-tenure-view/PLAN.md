# Feature Plan: Patient LMP Tenure View

**Repo:** TenantCore.App
**Date:** 2026-06-16
**Domain area:** Patients / Pregnancy Tenures
**Status:** Approved — ready for execution

---

## Overview

This feature adds a "View LMP" action button to the doctor's patient list. The button appears only for patients who have at least one `PregnancyTenure` record. When the patient has an active (running) tenure, an "Active" status badge is shown alongside the button; when all tenures are closed and no active tenure exists, no status badge is shown — the cell is left visually empty per rule. Clicking the button opens a read-only popup that lists all of the patient's tenures (current and historical) ordered newest-first, showing LMP date, EDD by LMP, EDD by USG, effective EDD, status, outcome, and closure date.

No new domain entities are required. `PregnancyTenure` was created in the previously executed `lmp-edd-pregnancy-tenure` feature. This feature is purely additive: a new query, a new API endpoint, two new repository methods, and Blazor UI changes.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Shared | Modify `PatientDto` — add two boolean tenure-status fields |
| Domain | Extend `IPregnancyTenureRepository` — add two new query method contracts |
| Infrastructure | Implement two new methods in `PregnancyTenureRepository` |
| Application | New query + handler for patient tenure history; modify `GetPatientsHandler` to enrich patient DTOs |
| API | Add one new endpoint to existing `PregnancyTenuresController` |
| Blazor Client | Extend API client interface + implementation; new popup component; modify patient list page |

---

## No New Entity

This feature introduces **no new domain entities and no EF migration**. All data comes from the existing `PregnancyTenure` entity and `PregnancyTenures` DbSet.

---

## Data Model Changes

### PatientDto (modified)

Two fields are added to `TenantCore.Shared/Dtos/PatientDto.cs`:

| New Property | Type | Purpose |
|---|---|---|
| `HasLmpRecord` | `bool` | `true` when the patient has at least one `PregnancyTenure` row — controls "View LMP" button visibility |
| `HasActiveTenure` | `bool` | `true` when the patient has a tenure with `Status == Active` — controls the status badge in the list row |

Both default to `false`. When `HasLmpRecord` is `false`, `HasActiveTenure` is always `false`.

---

## Repository Changes

### IPregnancyTenureRepository (modified)

Two new method contracts added to `TenantCore.Domain/Interfaces/IPregnancyTenureRepository.cs`:

| Method | Signature | Purpose |
|---|---|---|
| `GetTenureInfoForPatientsAsync` | `Task<Dictionary<Guid, bool>> GetTenureInfoForPatientsAsync(IEnumerable<Guid> patientIds, Guid applicationId, CancellationToken ct = default)` | Returns a dictionary keyed by patientId. A patient appears in the dictionary only if they have at least one tenure record. The value is `true` when the patient has an `Active` tenure, `false` when all tenures are `Closed`. Used by `GetPatientsHandler` to enrich the patient list in a single DB round-trip. |
| `GetAllForPatientAsync` | `Task<IEnumerable<PregnancyTenure>> GetAllForPatientAsync(Guid patientId, Guid applicationId, CancellationToken ct = default)` | Returns all tenure records for a single patient ordered by `CreatedAt` descending (newest first). Used by the new popup query handler. |

### PregnancyTenureRepository (modified)

`TenantCore.Infrastructure/Repositories/PregnancyTenureRepository.cs` implements both new methods:

- `GetTenureInfoForPatientsAsync` — uses a `GroupBy` on `PatientId` filtered to the given `patientIds` and `applicationId`, projecting `PatientId` and whether any tenure in the group is `Active`. Materialised with `ToDictionaryAsync`.
- `GetAllForPatientAsync` — filters by `patientId` and `applicationId`, orders by `CreatedAt` descending, returns `AsNoTracking`.

---

## Files to Create

### Application Layer (`src/TenantCore.Application/Features/PregnancyTenures/`)

| File | Purpose |
|------|---------|
| `Queries/GetPatientPregnancyTenuresQuery.cs` | Read query — carries `PatientId` and `ApplicationId`; returns `IEnumerable<PregnancyTenureDto>` |
| `Handlers/GetPatientPregnancyTenuresHandler.cs` | Calls `IPregnancyTenureRepository.GetAllForPatientAsync`, maps results via existing `PregnancyTenureTranslator.ToDto`, returns list ordered newest-first |

### Blazor Client (`src/TenantCore.Web.Client/`)

| File | Purpose |
|------|---------|
| `Components/Patients/PatientTenureHistoryDialog.razor` | Read-only popup component. Accepts a `PatientId` parameter. On open, calls `IPregnancyTenureApiClient.GetAllForPatientAsync`. Renders a table of all tenures with columns: LMP, EDD (LMP), EDD (USG), Effective EDD, Status badge, Outcome, Closed On. No edit or delete actions. |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Shared/Dtos/PatientDto.cs` | Add `bool HasLmpRecord` and `bool HasActiveTenure` properties |
| `src/TenantCore.Domain/Interfaces/IPregnancyTenureRepository.cs` | Add `GetTenureInfoForPatientsAsync` and `GetAllForPatientAsync` method signatures |
| `src/TenantCore.Infrastructure/Repositories/PregnancyTenureRepository.cs` | Implement both new methods |
| `src/TenantCore.Application/Features/Patients/Handlers/GetPatientsHandler.cs` | Inject `IPregnancyTenureRepository`; after fetching the current page of patients, extract their IDs, call `GetTenureInfoForPatientsAsync`, and set `HasLmpRecord` / `HasActiveTenure` on each mapped `PatientDto` using the dictionary result |
| `src/TenantCore.Api/Controllers/PregnancyTenuresController.cs` | Add `GET patient/{patientId:guid}` endpoint that dispatches `GetPatientPregnancyTenuresQuery` |
| `src/TenantCore.Web.Client/Clients/IPregnancyTenureApiClient.cs` | Add `Task<ApiResponse<IEnumerable<PregnancyTenureDto>>> GetAllForPatientAsync(Guid patientId)` |
| `src/TenantCore.Web.Client/Clients/PregnancyTenureApiClient.cs` | Implement the new `GetAllForPatientAsync` method — `GET api/pregnancytenures/patient/{patientId}` |
| `src/TenantCore.Web.Client/Pages/Patients/PatientList.razor` | In the Actions column: add "View LMP" button (visible only when `p.HasLmpRecord`), add "Active" badge next to it (visible only when `p.HasActiveTenure`); add popup trigger that sets `_lmpPatientId` and `_lmpDialogOpen = true`; render `<PatientTenureHistoryDialog>` below the table wired to `_lmpDialogOpen` and `_lmpPatientId` |

---

## API Endpoint

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| GET | `api/pregnancytenures/patient/{patientId}` | — | `IEnumerable<PregnancyTenureDto>` | `RequireAuthenticated` |

This endpoint is read-only and does not require `RequireClinical` — any authenticated clinic user (doctor, reception) may view a patient's tenure history.

---

## Business Rules

Rules enforced in UI and/or handler — no writes occur in this feature:

1. **Button visibility** — "View LMP" button renders only when `PatientDto.HasLmpRecord == true`. Evaluated during patient list render; no extra API call needed per row.
2. **Status badge visibility** — The "Active" badge beside the button renders only when `PatientDto.HasActiveTenure == true`. When all of a patient's tenures are `Closed`, the button appears but the status cell next to it is left empty.
3. **Popup data** — The popup shows ALL tenures for the patient (Active and Closed), ordered newest-first. There is no filtering or pagination in the popup — all records are shown.
4. **No writes from popup** — The popup is read-only. Close-tenure and add-LMP actions remain on the existing prescription and tenure workflows; this popup is an informational view only.

---

## Multi-Tenancy Checklist

- [x] `ApplicationId` passed in `GetPatientPregnancyTenuresQuery`
- [x] `GetAllForPatientAsync` filters by `applicationId`
- [x] `GetTenureInfoForPatientsAsync` filters by `applicationId`
- [x] New controller endpoint uses `GetApplicationId()` from `ClinicControllerBase`
- [x] No new Blazor client-side tenancy wiring required — existing `X-Application-Id` header is already set by the registered `HttpClient`

---

## EF Migration

**Not required.** This feature adds no new entities, columns, or indexes. The `PregnancyTenures` table and all required columns already exist.

---

## Implementation Order

Execute in this sequence to avoid compile errors:

1. Modify `PatientDto.cs` — add `HasLmpRecord` and `HasActiveTenure` fields (Shared)
2. Modify `IPregnancyTenureRepository.cs` — add `GetTenureInfoForPatientsAsync` and `GetAllForPatientAsync` (Domain)
3. Modify `PregnancyTenureRepository.cs` — implement both new methods (Infrastructure)
4. Modify `GetPatientsHandler.cs` — inject `IPregnancyTenureRepository`, enrich DTOs with tenure status (Application)
5. Create `GetPatientPregnancyTenuresQuery.cs` (Application)
6. Create `GetPatientPregnancyTenuresHandler.cs` (Application)
7. Modify `PregnancyTenuresController.cs` — add new GET endpoint (API)
8. Modify `IPregnancyTenureApiClient.cs` — add method signature (Blazor)
9. Modify `PregnancyTenureApiClient.cs` — implement method (Blazor)
10. Create `PatientTenureHistoryDialog.razor` (Blazor)
11. Modify `PatientList.razor` — add button, badge, and popup wiring (Blazor)

---

## PatientTenureHistoryDialog — UI Specification

The dialog is a `MudDialog` rendered inside `PatientList.razor` with `@bind-Visible="_lmpDialogOpen"`.

### Trigger (in the Actions column of the patient list table)

When `p.HasLmpRecord == true`, the actions cell renders:
- A "View LMP" button (styled `opd-btn` in teal/purple — distinct from the existing View/Edit/Delete buttons)
- Immediately after the button: if `p.HasActiveTenure == true`, render an "Active" badge using the existing `opd-badge done` CSS class; otherwise render nothing in that slot

### Dialog content

The dialog title shows the patient's name, e.g. "Pregnancy History — [Patient Name]".

The body contains a table with these columns:

| Column | Source field | Notes |
|--------|-------------|-------|
| # | Row index | Newest = 1 |
| LMP | `Lmp` | Formatted `dd MMM yyyy` |
| EDD (by LMP) | `EddByLmp` | Formatted `dd MMM yyyy` |
| EDD (by USG) | `EddByUsg` | Formatted `dd MMM yyyy`; show `—` if null |
| Effective EDD | `EffectiveEdd` | Formatted `dd MMM yyyy`; bold |
| Status | `Status` | Badge: `opd-badge done` for Active, `opd-badge cancelled` for Closed |
| Outcome | `Outcome` | Show enum display name; `—` if null |
| Closed On | `ClosedAt` | Formatted `dd MMM yyyy`; `—` if null |

The dialog has a single "Close" button in the footer.

### Loading state

On open (`_lmpDialogOpen` set to true), the dialog immediately shows a `MudProgressLinear` spinner while `IPregnancyTenureApiClient.GetAllForPatientAsync` is called. On success the table is displayed. On error, a brief error message is shown inside the dialog body.

---

## Test Coverage Required

| Test class | What it tests |
|-----------|--------------|
| `GetPatientPregnancyTenuresHandlerTests` | Returns all tenures for patient ordered newest-first; returns empty list when no tenures exist; passes correct `applicationId` to repository |
| `GetPatientsHandlerEnrichmentTests` | `HasLmpRecord` and `HasActiveTenure` correctly set based on `GetTenureInfoForPatientsAsync` result; patients with no tenure records have both fields as `false` |

---

## Open Questions / Risks

- **Performance** — `GetTenureInfoForPatientsAsync` runs once per patient list page load (e.g. 20 patients per page). This is a single `GROUP BY` query against `PregnancyTenures` scoped to at most 20 IDs — acceptable performance. No index change needed; the existing `(PatientId, ApplicationId)` filter pattern aligns with existing queries.
- **No Auth dependency** — this feature is entirely within TenantCore.App; no TenantCore.Auth changes required.
