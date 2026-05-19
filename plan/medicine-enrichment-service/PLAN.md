# Feature Plan: Medicine Dosage Form Normalization

## Repo
TenantCore.App

## Overview
Three independent concerns delivered together. (A) A new `MedicineDosageForm` lookup table and FK on `Medicine` added via EF migration to the existing Infrastructure project — no logic changes to existing code beyond what the migration and search filter require. (B) A standalone console tool (`tools/MedicineNormalizer/`) that is fully gitignored, connects to the database with its own `appsettings.json`, reads existing `BrandName` and `PackSize` columns to extract and populate dosage forms, and writes daily Markdown logs — this tool is run manually once and never committed. (C) The existing medicine search API and filters are updated to support filtering by `DosageFormId` and returning `DosageFormName` in results.

## Domain Area
`MedicineDosageForms` + `Medicines` — Domain, Infrastructure (migration only), Application (search filter update), Api (search filter update), plus new gitignored `tools/MedicineNormalizer` project

---

## Part A — Changes to Existing Projects (committed to git)

### Files to Create

| File | Purpose |
|------|---------|
| `src/TenantCore.Domain/Entities/MedicineDosageForm.cs` | Lookup entity — Tablet, Capsule, Syrup, etc. |
| `src/TenantCore.Domain/Interfaces/IMedicineDosageFormRepository.cs` | Repository interface for MedicineDosageForm |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/MedicineDosageFormConfiguration.cs` | EF config — unique index on Name, 23-form seed data |
| `src/TenantCore.Infrastructure/Repositories/MedicineDosageFormRepository.cs` | EF Core repository implementation |
| `src/TenantCore.Application/Features/MedicineDosageForms/Translators/MedicineDosageFormTranslator.cs` | Entity → DTO |
| `src/TenantCore.Application/Features/MedicineDosageForms/Commands/CreateMedicineDosageFormCommand.cs` | Create command |
| `src/TenantCore.Application/Features/MedicineDosageForms/Commands/UpdateMedicineDosageFormCommand.cs` | Update command |
| `src/TenantCore.Application/Features/MedicineDosageForms/Commands/DeleteMedicineDosageFormCommand.cs` | Soft-deactivate command |
| `src/TenantCore.Application/Features/MedicineDosageForms/Queries/GetMedicineDosageFormsQuery.cs` | Paged list query |
| `src/TenantCore.Application/Features/MedicineDosageForms/Queries/GetMedicineDosageFormByIdQuery.cs` | Single record query |
| `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/CreateMedicineDosageFormHandler.cs` | Create handler |
| `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/UpdateMedicineDosageFormHandler.cs` | Update handler |
| `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/DeleteMedicineDosageFormHandler.cs` | Delete handler — sets IsActive = false |
| `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/GetMedicineDosageFormsHandler.cs` | Paged list handler |
| `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/GetMedicineDosageFormByIdHandler.cs` | Single record handler |
| `src/TenantCore.Application/Features/MedicineDosageForms/Validators/CreateMedicineDosageFormCommandValidator.cs` | Validates Create |
| `src/TenantCore.Application/Features/MedicineDosageForms/Validators/UpdateMedicineDosageFormCommandValidator.cs` | Validates Update |
| `src/TenantCore.Shared/Dtos/MedicineDosageFormDto.cs` | MedicineDosageFormDto (read), CreateMedicineDosageFormDto, UpdateMedicineDosageFormDto |
| `src/TenantCore.Api/Controllers/MedicineDosageFormsController.cs` | CRUD endpoints for the lookup table |

### Files to Modify (existing projects)

| File | Change |
|------|--------|
| `src/TenantCore.Domain/Entities/Medicine.cs` | Add `DosageFormId` (Guid?), `MedicineDosageForm` nav property, `IsDosageFormMapped` (bool default false), `DosageFormMappedAt` (DateTime?); add `MapDosageForm(Guid dosageFormId)` and `MarkDosageFormMapped()` methods |
| `src/TenantCore.Domain/Interfaces/IMedicineRepository.cs` | Add `GetUnmappedAsync(int batchSize, CancellationToken ct)` — returns medicines where `IsDosageFormMapped == false` |
| `src/TenantCore.Infrastructure/Persistence/AppDbContext.cs` | Add `DbSet<MedicineDosageForm> MedicineDosageForms` |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/MedicineConfiguration.cs` | Add `DosageFormId` nullable FK → MedicineDosageForms, `IsDosageFormMapped` (default false, index), `DosageFormMappedAt` nullable |
| `src/TenantCore.Infrastructure/Repositories/MedicineRepository.cs` | Implement `GetUnmappedAsync`; update `GetByIdWithTypeAsync` to include DosageForm; update `GetPagedAsync` to join and filter by `DosageFormId` |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `IMedicineDosageFormRepository → MedicineDosageFormRepository` |
| `src/TenantCore.Shared/Dtos/MedicineDto.cs` | Add `DosageFormId` (Guid?), `DosageFormName` (string?) |
| `src/TenantCore.Application/Features/Medicines/Queries/GetMedicinesQuery.cs` | Add `DosageFormId` (Guid?) filter parameter |
| `src/TenantCore.Application/Features/Medicines/Handlers/GetMedicinesHandler.cs` | Apply `DosageFormId` filter when provided |
| `src/TenantCore.Application/Features/Medicines/Translators/MedicineTranslator.cs` | Map `DosageFormId` and `DosageFormName` |
| `src/TenantCore.Api/Controllers/MedicinesController.cs` | Add `dosageFormId` (Guid?) query parameter to `GetAll` |

---

## Part B — Standalone Gitignored Tool (never committed)

### New Project

`tools/MedicineNormalizer/` — .NET Console App (Worker Service pattern). Added to `.gitignore`. Has its own `appsettings.json` with DB connection string. References `TenantCore.Infrastructure` and `TenantCore.Domain` to reuse `AppDbContext` and entities directly — no duplication.

### Files to Create (all gitignored via folder)

| File | Purpose |
|------|---------|
| `tools/MedicineNormalizer/MedicineNormalizer.csproj` | Console app — references TenantCore.Infrastructure + TenantCore.Domain |
| `tools/MedicineNormalizer/appsettings.json` | DB connection string (gitignored, never committed) |
| `tools/MedicineNormalizer/Program.cs` | Host setup — DI, logging, runs NormalizationWorker then exits |
| `tools/MedicineNormalizer/NormalizationWorker.cs` | Main loop — batches unmapped medicines, calls extractor, updates DB, writes log |
| `tools/MedicineNormalizer/MedicineDosageFormExtractor.cs` | Static utility — parses BrandName and PackSize to extract canonical form name |
| `tools/MedicineNormalizer/MedicineEnrichmentLogger.cs` | Appends daily run results to `medicine-logs/YYYY-MM-DD.md` at repo root |

### .gitignore Additions

```
# One-time medicine normalizer tool
tools/MedicineNormalizer/

# Medicine normalization run logs
medicine-logs/
```

---

## Part C — .gitignore Update

| File | Change |
|------|--------|
| `.gitignore` | Append `tools/MedicineNormalizer/` and `medicine-logs/` |

---

## API Endpoints

### MedicineDosageForm CRUD (new)

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|--------------|----------|-------------|
| GET | /api/medicine-dosage-forms | — | `PagedResult<MedicineDosageFormDto>` | RequireAuthenticated |
| GET | /api/medicine-dosage-forms/{id} | — | `MedicineDosageFormDto` | RequireAuthenticated |
| POST | /api/medicine-dosage-forms | `CreateMedicineDosageFormDto` | `MedicineDosageFormDto` (201) | RequireManagement |
| PUT | /api/medicine-dosage-forms/{id} | `UpdateMedicineDosageFormDto` | `MedicineDosageFormDto` | RequireManagement |
| DELETE | /api/medicine-dosage-forms/{id} | — | 204 | RequireAdmin |

### Medicine Search (updated — new filter only)

| Method | Route | New Parameter | Notes |
|--------|-------|---------------|-------|
| GET | /api/medicines | `dosageFormId` (Guid?, optional) | Filters results to medicines linked to this dosage form |

---

## Entity Properties

### MedicineDosageForm (new)

| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required, MaxLength(100), Unique index |
| `Description` | `string?` | MaxLength(500) |
| `IsActive` | `bool` | Default true |
| `CreatedAt` | `DateTime` | Set on create |
| `UpdatedAt` | `DateTime?` | Set on update |

### Medicine (new columns only)

| Property | Type | Constraints |
|----------|------|-------------|
| `DosageFormId` | `Guid?` | Nullable FK → MedicineDosageForms.Id, no cascade delete |
| `IsDosageFormMapped` | `bool` | Default false, index |
| `DosageFormMappedAt` | `DateTime?` | Nullable |

### MedicineDosageForm Seed Data (23 forms)

| Canonical Name | Matches in data |
|---|---|
| Tablet | "tablet", "tablets" |
| Tablet SR | "tablet sr" |
| Tablet XR | "tablet xr", "tablet er" |
| Capsule | "capsule", "capsules" |
| Syrup | "syrup" |
| Dry Syrup | "dry syrup" |
| Cream | "cream" |
| Eye Drop | "eye drop", "eye drops" |
| Drop | "drop", "drops" |
| Injection | "injection" |
| Infusion | "infusion" |
| Inhaler | "inhaler" |
| Patch | "patch" |
| Powder | "powder" |
| Gel | "gel" |
| Ointment | "ointment" |
| Lotion | "lotion" |
| Ophthalmic Suspension | "opthalmic suspension", "ophthalmic suspension" |
| Oral Suspension | "oral suspension", "suspension" |
| Ophthalmic Solution | "ophthalmic solution", "opthalmic solution" |
| Solution | "solution" |
| Spray | "spray", "nasal spray" |
| Oral Solution | "oral solution" |

---

## Validation Rules

| Field | Rule |
|-------|------|
| `CreateMedicineDosageFormDto.Name` | NotEmpty, MaxLength(100) |
| `UpdateMedicineDosageFormDto.Name` | NotEmpty, MaxLength(100) |

---

## Extraction Algorithm (MedicineDosageFormExtractor in standalone tool)

### Phase 0 — Typo + Variant Normalization (applied first, before any matching)

A static normalization dictionary is applied to the raw input text **before** any form matching or DB lookup. This guarantees the extractor always returns a canonical name — never a typo — so no duplicate `MedicineDosageForm` records can be created regardless of how many spelling variants exist in the data.

| Raw variant (case-insensitive) | Canonical output |
|---|---|
| opthalmic, ophtamic, opthamic | Ophthalmic |
| opthalmic suspension | Ophthalmic Suspension |
| opthalmic solution | Ophthalmic Solution |
| opthalimc | Ophthalmic |
| suspenion, suspention, supsension | Suspension |
| capsules | Capsule |
| tablets | Tablet |
| drops | Drop |
| eye drops | Eye Drop |
| injections | Injection |
| inhalor, inhailer | Inhaler |
| syp, syrp | Syrup |
| tab, tabs | Tablet |
| cap, caps | Capsule |
| crm | Cream |
| oint | Ointment |
| susp | Suspension |
| soln, sol | Solution |
| inj | Injection |
| inf | Infusion |

The dictionary is applied via whole-word, case-insensitive regex replacement on the extracted token — not on the full original string — so "Opthalmic Suspension" → "Ophthalmic Suspension" as a unit.

---

**Step 1 — Try BrandName:**
- Apply Phase 0 normalization to the full BrandName string
- Match trailing 1–3 words against known canonical forms, longest match first, case-insensitive
- Example: `"Bactol 250mg Capsule"` → normalize → match "Capsule" ✓
- Example: `"Moxtif LP Opthalmic Suspension"` → normalize → "Moxtif LP Ophthalmic Suspension" → match "Ophthalmic Suspension" ✓
- Example: `"Eyefen Eye Drop"` → match "Eye Drop" ✓

**Step 2 — Fall back to PackSize:**
- Apply Phase 0 normalization to the full PackSize string
- Split on ` of ` → take part after "of"
- Strip leading number + optional unit (ml, gm, mg, mcg, IU)
- Match remaining text against canonical forms (longest match first)
- Example: `"strip of 10 tablet sr"` → normalize → strip "10" → "tablet sr" → "Tablet SR" ✓
- Example: `"bottle of 5 ml Opthalmic Suspension"` → normalize → strip "5 ml" → "Ophthalmic Suspension" ✓
- Example: `"strip of 15 capsules"` → normalize → strip "15" → "capsule" → "Capsule" ✓

**Step 3 — Return null:**
- `DosageFormId` stays null, medicine still marked `IsDosageFormMapped = true`
- Logged as "Not Found"

---

## Business Rules

- `MedicineDosageForm.Name` is unique — enforced at DB via unique index (case-insensitive collation on the column).
- The extractor **always returns a canonical name or null** — it never returns raw text from the DB column. Typo normalization (Phase 0) runs before any form matching, so a misspelling in `BrandName` or `PackSize` can never create a duplicate `MedicineDosageForm` record.
- `NormalizationWorker` does a case-insensitive `GetByNameAsync` lookup using the canonical name before inserting — if a record already exists (even if seeded with a slightly different casing), it reuses the existing record's Id.
- `GetByNameAsync` on `IMedicineDosageFormRepository` uses `EF.Functions.Like` or `.ToLower()` comparison — never an exact string equality — so "tablet" and "Tablet" both resolve to the same record.
- If two medicines independently yield the same canonical form name in the same batch, the second one reuses the `MedicineDosageForm` already created by the first — no race-condition duplicates because processing is sequential within a batch.
- A medicine with `IsDosageFormMapped = true` is never returned by `GetUnmappedAsync` — permanently skipped even when `DosageFormId` is null (attempt already made, no form found).
- `MarkDosageFormMapped()` is called only after `SaveChanges` succeeds.
- The tool processes medicines in batches of 100 (configurable in `appsettings.json`), loops until no unmapped medicines remain, then exits.
- If a single medicine throws during processing, it is skipped for this run (not marked mapped) and the exception is logged; the batch continues.
- Delete on `MedicineDosageForm` is soft — sets `IsActive = false` to protect existing FK references.

---

## Log File Format (written by standalone tool)

Path: `medicine-logs/YYYY-MM-DD.md` at repo root (appended per run, file created if absent, folder gitignored)

```
## Run: 2026-05-19 10:30 UTC  |  Processed: 48  |  Mapped: 45  |  Not Found: 2  |  Failed: 1

| Medicine Name | Brand Name | Extracted From | Dosage Form | Status | Mapped At |
|---|---|---|---|---|---|
| Paracetamol | Panadol Tablet | BrandName | Tablet | Mapped | 2026-05-19 10:30 |
| Bactol 250mg Capsule | Bactol 250mg Capsule | BrandName | Capsule | Mapped | 2026-05-19 10:30 |
| Moxtif LP | Moxtif LP Opthalmic Suspension | BrandName | Ophthalmic Suspension | Mapped | 2026-05-19 10:30 |
| Tuss BD | Tuss BD Syrup | BrandName | Syrup | Mapped | 2026-05-19 10:30 |
| Unknown | — | PackSize | Tablet SR | Mapped | 2026-05-19 10:30 |
| XYZ Generic | XYZ Generic | None | — | Not Found | 2026-05-19 10:30 |
```

---

## Implementation Order

### Existing projects (committed to git)

1. `src/TenantCore.Domain/Entities/MedicineDosageForm.cs` — create entity
2. `src/TenantCore.Domain/Interfaces/IMedicineDosageFormRepository.cs` — create interface
3. `src/TenantCore.Domain/Entities/Medicine.cs` — add DosageFormId, IsDosageFormMapped, DosageFormMappedAt, nav property, MapDosageForm(), MarkDosageFormMapped()
4. `src/TenantCore.Domain/Interfaces/IMedicineRepository.cs` — add GetUnmappedAsync
5. `src/TenantCore.Shared/Dtos/MedicineDosageFormDto.cs` — create three DTOs
6. `src/TenantCore.Shared/Dtos/MedicineDto.cs` — add DosageFormId, DosageFormName
7. `src/TenantCore.Infrastructure/Persistence/AppDbContext.cs` — add DbSet<MedicineDosageForm>
8. `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/MedicineDosageFormConfiguration.cs` — EF config + 23-form seed data
9. `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/MedicineConfiguration.cs` — add FK + new column config
10. `src/TenantCore.Infrastructure/Repositories/MedicineDosageFormRepository.cs` — implement
11. `src/TenantCore.Infrastructure/Repositories/MedicineRepository.cs` — implement GetUnmappedAsync; update GetPagedAsync and GetByIdWithTypeAsync to include DosageForm
12. `src/TenantCore.Infrastructure/DependencyInjection.cs` — register IMedicineDosageFormRepository
13. `src/TenantCore.Application/Features/MedicineDosageForms/Translators/MedicineDosageFormTranslator.cs`
14. `src/TenantCore.Application/Features/MedicineDosageForms/Commands/CreateMedicineDosageFormCommand.cs`
15. `src/TenantCore.Application/Features/MedicineDosageForms/Commands/UpdateMedicineDosageFormCommand.cs`
16. `src/TenantCore.Application/Features/MedicineDosageForms/Commands/DeleteMedicineDosageFormCommand.cs`
17. `src/TenantCore.Application/Features/MedicineDosageForms/Queries/GetMedicineDosageFormsQuery.cs`
18. `src/TenantCore.Application/Features/MedicineDosageForms/Queries/GetMedicineDosageFormByIdQuery.cs`
19. `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/CreateMedicineDosageFormHandler.cs`
20. `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/UpdateMedicineDosageFormHandler.cs`
21. `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/DeleteMedicineDosageFormHandler.cs`
22. `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/GetMedicineDosageFormsHandler.cs`
23. `src/TenantCore.Application/Features/MedicineDosageForms/Handlers/GetMedicineDosageFormByIdHandler.cs`
24. `src/TenantCore.Application/Features/MedicineDosageForms/Validators/CreateMedicineDosageFormCommandValidator.cs`
25. `src/TenantCore.Application/Features/MedicineDosageForms/Validators/UpdateMedicineDosageFormCommandValidator.cs`
26. `src/TenantCore.Application/Features/Medicines/Queries/GetMedicinesQuery.cs` — add DosageFormId filter
27. `src/TenantCore.Application/Features/Medicines/Handlers/GetMedicinesHandler.cs` — apply DosageFormId filter
28. `src/TenantCore.Application/Features/Medicines/Translators/MedicineTranslator.cs` — map DosageFormId + DosageFormName
29. `src/TenantCore.Api/Controllers/MedicineDosageFormsController.cs` — CRUD controller
30. `src/TenantCore.Api/Controllers/MedicinesController.cs` — add dosageFormId query param
31. Run migration: `dotnet ef migrations add AddMedicineDosageFormAndNormalizationTracking --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api`

### Standalone gitignored tool (not committed)

32. `.gitignore` — append `tools/MedicineNormalizer/` and `medicine-logs/`
33. `tools/MedicineNormalizer/MedicineNormalizer.csproj` — console app, project refs to Infrastructure + Domain
34. `tools/MedicineNormalizer/appsettings.json` — connection string (matches AppDbContext key)
35. `tools/MedicineNormalizer/MedicineDosageFormExtractor.cs` — static extraction + normalization logic
36. `tools/MedicineNormalizer/MedicineEnrichmentLogger.cs` — daily md log writer
37. `tools/MedicineNormalizer/NormalizationWorker.cs` — batch loop, calls extractor, updates DB, calls logger
38. `tools/MedicineNormalizer/Program.cs` — host builder, DI wiring, runs worker then exits

---

## Migration Name

`AddMedicineDosageFormAndNormalizationTracking`

---

## Execution Status

- **Status**: Plan fully executed and completed
- **Started**: 2026-05-19
- **Development completed**: 2026-05-19
- **Security check completed**: 2026-05-19
- **Completed**: 2026-05-19
