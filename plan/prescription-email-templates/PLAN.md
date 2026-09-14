# Feature Plan: Prescription Email Templates

**Repo:** TenantCore.App
**Date:** 2026-09-14
**Domain area:** Prescriptions (email communication)
**Status:** Approved — ready for execution

---

## Overview

When a doctor submits a prescription, the system already emails the patient. This feature replaces the current hardcoded HTML email with a themed email system: **five color/formatted templates** that a clinic admin selects once per clinic. The outgoing email includes the patient's **appointment (OPD registration) details** and attaches the prescription as a **server-generated PDF** (QuestPDF). Email failures remain non-blocking (existing behavior preserved). Multi-tenant — the selected theme is per-clinic.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Domain | Extend `PrescriptionConfig` entity with `EmailTheme` |
| Infrastructure | New `PrescriptionPdfGenerator` (QuestPDF), DI registration, EF migration, add QuestPDF package |
| Application | Email theme catalog + email builder, `IPrescriptionPdfGenerator` interface, modify `SubmitPrescriptionHandler` + `PrescriptionConfig` command/query/handler/validator/translator |
| API | `PrescriptionConfigController` returns/accepts `EmailTheme` |
| Shared | `EmailTemplateTheme` enum; `PrescriptionConfigDto`/`UpdatePrescriptionConfigDto` gain `EmailTheme` |
| Web.Client | `PrescriptionSettings.razor` theme picker + live preview; extend `IPrescriptionApiClient` |

---

## Entity: PrescriptionConfig (extended — no new entity)

**Tenant-scoped:** Yes
**Base class:** BaseEntity

| Property | Type | Constraints |
|----------|------|-------------|
| ApplicationId | Guid | FK to clinic — required |
| EmailTheme | EmailTemplateTheme (enum) | default `AzureClassic` |

---

## Email Theme Catalog (5 themes)

| Theme | Header | Accent | Body bg | Table header | Layout |
|-------|--------|--------|---------|--------------|--------|
| AzureClassic (1) | `#1565C0` | `#1E88E5` | `#F5F8FC` | `#1565C0` / white | Banner + bordered table |
| EmeraldCare (2) | `#0F766E` | `#14B8A6` | `#F0FDFA` | `#0F766E` / white | Rounded cards + pill badges |
| SunsetRose (3) | `#E11D48` | `#FB7185` | `#FFF1F2` | `#E11D48` / white | Warm banner + rose table |
| MidnightIndigo (4) | `#312E81` | `#6366F1` | `#EEF2FF` | `#312E81` / white | Dark high-contrast header |
| MinimalSlate (5) | `#334155` | `#64748B` | `#F8FAFC` | `#F1F5F9` / `#334155` | Flat, thin dividers |

Shared HTML skeleton (same content, restyled per theme): header → greeting → appointment details → medicines table → next visit/notes → footer + "PDF attached" + disclaimer.

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)
| File | Purpose |
|------|---------|
| `Enums/EmailTemplateTheme.cs` | Enum: AzureClassic=1, EmeraldCare=2, SunsetRose=3, MidnightIndigo=4, MinimalSlate=5 |

### Application Layer (`src/TenantCore.Application/`)
| File | Purpose |
|------|---------|
| `Features/Prescriptions/Emails/EmailTemplateThemeCatalog.cs` | Static token catalog — color/layout tokens per theme |
| `Features/Prescriptions/Emails/PrescriptionEmailBuilder.cs` | Static builder → subject + themed HTML + attachment name |
| `Services/IPrescriptionPdfGenerator.cs` | Interface: generate prescription PDF bytes |

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)
| File | Purpose |
|------|---------|
| `Services/PrescriptionPdfGenerator.cs` | QuestPDF A4 prescription layout (mirrors PrescriptionPrint.razor `rx-*` style) |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Domain/Entities/PrescriptionConfig.cs` | Add `EmailTheme` property + factory/Update params |
| `src/TenantCore.Shared/Dtos/PrescriptionConfigDto.cs` | Add `EmailTheme` |
| `src/TenantCore.Shared/Dtos/UpdatePrescriptionConfigDto.cs` | Add `EmailTheme` |
| `src/TenantCore.Application/Features/PrescriptionConfig/Commands/UpsertPrescriptionConfigCommand.cs` | Add `EmailTheme` |
| `src/TenantCore.Application/Features/PrescriptionConfig/Handlers/UpsertPrescriptionConfigHandler.cs` | Persist `EmailTheme` |
| `src/TenantCore.Application/Features/PrescriptionConfig/Handlers/GetPrescriptionConfigHandler.cs` | Return `EmailTheme` (default AzureClassic) |
| `src/TenantCore.Application/Features/PrescriptionConfig/Validators/UpsertPrescriptionConfigCommandValidator.cs` | `IsInEnum` rule |
| `src/TenantCore.Application/Features/PrescriptionConfig/Translators/PrescriptionConfigTranslator.cs` | Map `EmailTheme` |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/SubmitPrescriptionHandler.cs` | Inject config repo + PDF generator; themed email + appointment details + PDF attachment |
| `src/TenantCore.Infrastructure/TenantCore.Infrastructure.csproj` | Add `QuestPDF` package |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `IPrescriptionPdfGenerator` |
| `src/TenantCore.Api/Controllers/PrescriptionConfigController.cs` | Accept/return `EmailTheme` (auth policy unchanged) |
| `src/TenantCore.Web.Client/Clients/IPrescriptionApiClient.cs` + impl | Update config signatures |
| `src/TenantCore.Web.Client/Pages/Settings/PrescriptionSettings.razor` | Email template theme picker + preview |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| GET | `api/prescription-config` | — | `PrescriptionConfigDto` | RequireAuthenticated |
| PUT | `api/prescription-config` | `UpdatePrescriptionConfigDto` | `PrescriptionConfigDto` | RequireClinical (unchanged — ClinicAdmin included; preserves dual-panel settings access) |

---

## Validation Rules

| Field | Rules |
|-------|-------|
| EmailTheme | `IsInEnum()` |
| ApplicationId | NotEmpty — always required |

---

## Business Rules

1. On prescription submission, email is sent only when `Patient.Email` is present.
2. Theme resolved from `PrescriptionConfig`; unconfigured clinics default to `AzureClassic`.
3. Appointment details = linked `OpdRegistration` (registration no., date, doctor, fee, vitals).
4. Prescription PDF is generated server-side and attached as `Prescription-<Number>.pdf`.
5. Email failure is non-blocking — log the error, set `IsEmailSent=false`, continue (unchanged).

---

## Multi-Tenancy Checklist

- [x] `ApplicationId` present on `PrescriptionConfig` (theme is per-clinic)
- [x] Config query filters by `applicationId` (`GetByApplicationIdAsync`)
- [x] Controller uses `GetApplicationId()` from `ClinicControllerBase`
- [x] PDF/email content derived from the tenant-scoped prescription/patient/OPD

---

## EF Migration

**Migration name:** `AddPrescriptionEmailTheme`

Run after all infrastructure files are created:
```
dotnet ef migrations add AddPrescriptionEmailTheme --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

---

## Implementation Order

1. Shared enum `EmailTemplateTheme`
2. Domain `PrescriptionConfig` entity (+ `EmailTheme`)
3. Shared DTOs
4. Application email catalog + email builder + `IPrescriptionPdfGenerator`
5. Infrastructure `PrescriptionPdfGenerator` (QuestPDF) + DI registration + csproj package
6. Application PrescriptionConfig command/handler/validator/translator updates
7. `SubmitPrescriptionHandler` update (themed email + appointment details + PDF)
8. API `PrescriptionConfigController` update
9. Web.Client settings page + API client
10. Unit tests
11. EF migration (print command, do not auto-run)

---

## Test Files to Create

All test files live under `tests/TenantCore.Application.Tests/Features/Prescriptions/`.

| File | What it covers |
|------|---------------|
| `Emails/EmailTemplateThemeCatalogTests.cs` | All 5 themes defined with distinct tokens; every enum value maps to a catalog entry |
| `Emails/PrescriptionEmailBuilderTests.cs` | Subject + HTML include patient name, appointment details, medicines, next visit; HTML-encodes inputs; returns attachment name |
| `Handlers/SubmitPrescriptionHandlerTests.cs` | PDF attachment bytes passed to IEmailService; theme resolved; appointment details rendered; no email when Patient.Email empty; failure sets IsEmailSent=false |
| `PrescriptionConfig/Validators/UpsertPrescriptionConfigCommandValidatorTests.cs` | Invalid enum fails; valid enum passes; empty ApplicationId fails |
| `PrescriptionConfig/Handlers/UpsertPrescriptionConfigHandlerTests.cs` | EmailTheme persisted on create + update |
| `PrescriptionConfig/Translators/PrescriptionConfigTranslatorTests.cs` | EmailTheme mapped to/from entity |

---

## Open Questions / Risks

- QuestPDF is a **new dependency** (not currently in any project) — confirmed by user 2026-09-14.
- QuestPDF licensing: free under `Community` license for companies with < $1M USD annual revenue.
- Clinic name/branding in email header is not currently on `PrescriptionConfig`; defaults to a generic "Your Clinic" label.
