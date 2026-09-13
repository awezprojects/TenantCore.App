# Feature Plan: Action & Audit Logging

**Repo:** TenantCore.App
**Date:** 2026-09-13
**Domain area:** Cross-cutting / Observability (extends Centralized Logging Service)
**Status:** Approved — ready for execution

---

## Overview

Today TenantCore.App only logs unhandled exceptions (via the existing `IErrorLogger` / `TenantCore.Logging` Azure Table Storage pipeline). There is no durable record of *business actions* — who registered a patient, who created an OPD registration, who submitted a prescription, who accepted or disputed a cash handover, who processed a refund or applied a discount, or who deleted something. This feature adds a lightweight, opt-in audit trail: each instrumented action writes one "Started" row and one "Completed" or "Failed" row to a new `ActionLogs` Azure Table, tied together by a correlation id. Instrumentation is added by marking a command with a small interface and a human-readable action name — no handler body is touched, and no per-file logging code is required in commands, handlers, services, or repositories, per your instruction. Login (Admin/Doctor/Reception) is out of scope here because it is implemented entirely in the separate TenantCore.Auth repository; "flag actions" are also out of scope because no such feature exists anywhere in the current codebase.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| `TenantCore.Logging` (existing project) | New `ActionLogEntry` model + table name option — reuses the existing `IAppLogWriter`/`AzureTableLogWriter` write path, no new writer needed |
| Domain | New `IBusinessAction` marker interface, new `IActionLogger` interface, new `ICurrentUserContext` interface |
| Infrastructure | `ActionLoggingService` implementing `IActionLogger`; `CurrentUserContext` implementing `ICurrentUserContext` via `IHttpContextAccessor`; DI registrations |
| Application | New `ActionLoggingBehavior<TRequest,TResponse>` MediatR pipeline behavior; ~26 existing command records annotated with `IBusinessAction` |
| API | Register `IHttpContextAccessor` if not already present; no controller changes |

No `ClinicDbContext` change, no EF migration — `ActionLogs` is a new Azure Table, not a relational table.

---

## Data Model: Action Log Entry (Azure Table Storage, not EF)

**Table:** `ActionLogs` (configurable name, same storage account as `ApiErrorLogs`/`FrontendErrorLogs`)

| Field | Type | Notes |
|-------|------|-------|
| PartitionKey | string | Date bucket (yyyy-MM-dd) |
| RowKey | string | Reverse-chronological sortable id |
| CorrelationId | string (Guid) | Same value on the Started row and its matching Completed/Failed row |
| ActionName | string | Human-readable, e.g. `"Patient Registration"`, `"OPD Registration"`, `"Prescription Submission"`, `"Amount Handover - Accept"` |
| Status | string | `Started` / `Completed` / `Failed` |
| RequestType | string | CQRS command type name (diagnostic aid, e.g. `RegisterPatientCommand`) |
| ApplicationId | string (nullable) | Clinic context, when the command carries one |
| UserId | string (nullable) | Resolved from the authenticated request via `ICurrentUserContext` |
| DurationMs | long (nullable) | Set on Completed/Failed only |
| ErrorMessage | string (nullable) | Set on Failed only — short exception message, not the stack trace (full detail already goes to `IErrorLogger`/`ApiErrorLogs` via the existing exception middleware) |
| TimestampUtc | DateTime | Set at write time |

---

## Files to Create

### `TenantCore.Logging` (existing project — additive only)

| File | Purpose |
|------|---------|
| `ActionLogEntry.cs` | Plain model matching the schema above |

*(No new writer class — `AzureTableLogWriter`/`IAppLogWriter` already accept a target table name and a row payload; `ActionLogEntry` is written through the same path as `LogEntry`.)*

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Interfaces/IBusinessAction.cs` | Marker interface: `ActionName` (string) property. Commands that should produce an audit trail implement this |
| `Interfaces/IActionLogger.cs` | Contract: `LogStartedAsync(...)` returning a correlation id, and `LogCompletedAsync(correlationId, ...)` / `LogFailedAsync(correlationId, ...)` |
| `Interfaces/ICurrentUserContext.cs` | Contract: `UserId` (Guid?), read-only — resolves the authenticated user from the current request context |

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Services/ActionLoggingService.cs` | Implements `IActionLogger` — builds `ActionLogEntry` rows, delegates to `IAppLogWriter` from `TenantCore.Logging` against the `ActionLogs` table |
| `Services/CurrentUserContext.cs` | Implements `ICurrentUserContext` via `IHttpContextAccessor` — reads the user id claim from `HttpContext.User`; returns null outside an HTTP request (e.g. background services) |

### Application Layer (`src/TenantCore.Application/Common/Behaviors/`)

| File | Purpose |
|------|---------|
| `ActionLoggingBehavior.cs` | New `IPipelineBehavior<TRequest,TResponse>`. If `TRequest` implements `IBusinessAction`: reads `ApplicationId` off the request via a small reflection helper (property named `ApplicationId`, consistent with existing convention), resolves `UserId` from `ICurrentUserContext`, calls `LogStartedAsync`, invokes `next()`, calls `LogCompletedAsync` on success or `LogFailedAsync` on exception (then rethrows unchanged — this behavior never swallows or alters existing error handling). Requests that don't implement `IBusinessAction` pass through untouched — this keeps queries and non-audited commands silent, unlike the existing generic `LoggingBehavior` |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Application/DependencyInjection.cs` | Register `ActionLoggingBehavior` as an `IPipelineBehavior<,>`, ordered after `ValidationBehavior` (so validation failures don't get logged as a "Started" business action) |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `IActionLogger → ActionLoggingService` (Scoped), `ICurrentUserContext → CurrentUserContext` (Scoped); extend the existing `AddAppLogging` call/options with the `ActionLogs` table name |
| `src/TenantCore.Api/appsettings.json` | Add `ActionLogTable` (`ActionLogs`) under the existing `AppLogging` section |
| `src/TenantCore.Api/Program.cs` (or wherever `AddHttpContextAccessor` is/isn't already called) | Ensure `IHttpContextAccessor` is registered — add only if not already present |
| `src/TenantCore.Application/Features/Patients/Commands/RegisterPatientCommand.cs` | Implement `IBusinessAction`, `ActionName = "Patient Registration"` |
| `src/TenantCore.Application/Features/Patients/Commands/DeletePatientCommand.cs` | Implement `IBusinessAction`, `ActionName = "Patient Deactivation"` |
| `src/TenantCore.Application/Features/OpdRegistrations/Commands/CreateOpdRegistrationCommand.cs` | Implement `IBusinessAction`, `ActionName = "OPD Registration"` |
| `src/TenantCore.Application/Features/OpdRegistrations/Commands/DeleteOpdRegistrationCommand.cs` | Implement `IBusinessAction`, `ActionName = "OPD Registration Deactivation"` |
| `src/TenantCore.Application/Features/Prescriptions/Commands/SubmitPrescriptionCommand.cs` | Implement `IBusinessAction`, `ActionName = "Prescription Submission"` |
| `src/TenantCore.Application/Features/AmountHandovers/Commands/CreateAmountHandoverCommand.cs` | Implement `IBusinessAction`, `ActionName = "Amount Handover - Create"` |
| `src/TenantCore.Application/Features/AmountHandovers/Commands/AcceptAmountHandoverCommand.cs` | Implement `IBusinessAction`, `ActionName = "Amount Handover - Accept"` |
| `src/TenantCore.Application/Features/AmountHandovers/Commands/DisputeAmountHandoverCommand.cs` | Implement `IBusinessAction`, `ActionName = "Amount Handover - Dispute"` |
| `src/TenantCore.Application/Features/OpdPayments/Commands/ProcessOpdRefundCommand.cs` | Implement `IBusinessAction`, `ActionName = "OPD Refund"` |
| `src/TenantCore.Application/Features/OpdPayments/Commands/ApplyOpdDiscountCommand.cs` | Implement `IBusinessAction`, `ActionName = "OPD Discount"` |
| `src/TenantCore.Application/Features/VitalPresets/Commands/DeleteVitalPresetCommand.cs` | Implement `IBusinessAction`, `ActionName = "Vital Preset Deletion"` |
| `src/TenantCore.Application/Features/MedicineBundles/Commands/DeleteMedicineBundleCommand.cs` | Implement `IBusinessAction`, `ActionName = "Medicine Bundle Deletion"` |
| `src/TenantCore.Application/Features/Particulars/Commands/DeleteParticularCommand.cs` | Implement `IBusinessAction`, `ActionName = "Particular Deletion"` |
| `src/TenantCore.Application/Features/ExpenseRecords/Commands/DeleteExpenseRecordCommand.cs` | Implement `IBusinessAction`, `ActionName = "Expense Record Deletion"` |
| `src/TenantCore.Application/Features/ExpenseCategories/Commands/DeleteExpenseCategoryCommand.cs` | Implement `IBusinessAction`, `ActionName = "Expense Category Deletion"` |
| `src/TenantCore.Application/Features/DoctorFeeConfigs/Commands/DeleteDoctorFeeConfigCommand.cs` | Implement `IBusinessAction`, `ActionName = "Doctor Fee Config Deletion"` |
| `src/TenantCore.Application/Features/Wards/Commands/DeleteWardCommand.cs` | Implement `IBusinessAction`, `ActionName = "Ward Deletion"` |
| `src/TenantCore.Application/Features/Rooms/Commands/DeleteRoomCommand.cs` | Implement `IBusinessAction`, `ActionName = "Room Deletion"` |
| `src/TenantCore.Application/Features/DosageRemarks/Commands/DeleteDosageRemarkCommand.cs` | Implement `IBusinessAction`, `ActionName = "Dosage Remark Deletion"` |
| `src/TenantCore.Application/Features/Beds/Commands/DeleteBedCommand.cs` | Implement `IBusinessAction`, `ActionName = "Bed Deletion"` |
| `src/TenantCore.Application/Features/Applications/Commands/DeleteInvitationCommand.cs` | Implement `IBusinessAction`, `ActionName = "Invitation Deletion"` |
| `src/TenantCore.Application/Features/Applications/Commands/DeleteApplicationCommand.cs` | Implement `IBusinessAction`, `ActionName = "Application (Clinic) Deletion"` |
| `src/TenantCore.Application/Features/MedicineDosageForms/Commands/DeleteMedicineDosageFormCommand.cs` | Implement `IBusinessAction`, `ActionName = "Medicine Dosage Form Deletion"` |

*(Each of the above is a one-line addition to the record's base-type list plus a one-line `ActionName` property — no other change to the file.)*

---

## Business Rules

1. Only commands that explicitly implement `IBusinessAction` produce audit rows — this is intentionally opt-in so read queries and internal sub-commands (e.g. `EnsureOpdPaymentCommand` invoked from inside `CreateOpdRegistrationHandler`) stay silent unless deliberately added.
2. `ActionLoggingBehavior` never changes control flow: it always rethrows the original exception after logging `Failed`, and never swallows or wraps it. The existing `ExceptionHandlingMiddleware` → `IErrorLogger` path is unaffected and remains the source of full exception detail (type, stack trace).
3. Failure to write an action log must never fail the underlying request — `ActionLoggingService` swallows its own internal exceptions after a best-effort write attempt, mirroring `ErrorLoggingService`'s existing write-and-forget behavior.
4. `UserId` is best-effort — `ICurrentUserContext` returns null when there is no authenticated `HttpContext.User` (e.g. requests before login, or future background-triggered commands); the log row is still written with `UserId = null`.
5. `ApplicationId` is read off the command via its existing `ApplicationId` property (present on every tenant-scoped command per the workspace convention) — commands without one (there are none in this instrumented set) would log `ApplicationId = null`.
6. Login (Admin/Doctor/Reception) is explicitly out of scope for this plan — it is implemented in the separate TenantCore.Auth repository. A companion action-logging plan would need to be raised there separately if wanted.
7. "Flag actions" are out of scope — no such feature exists anywhere in the current TenantCore.App codebase today.

---

## Multi-Tenancy Checklist

- [x] `ApplicationId` captured on `ActionLogEntry` when the instrumented command carries one (all of them do)
- [ ] Not applicable: `ActionLogs` itself is intentionally a global/cross-tenant store (same design as `ApiErrorLogs`/`FrontendErrorLogs`), not filtered per clinic, for a future cross-tenant admin audit view

---

## EF Migration

None — Azure Table Storage, not SQL Server.

---

## Implementation Order

1. Domain — `IBusinessAction`, `IActionLogger`, `ICurrentUserContext`
2. `TenantCore.Logging` — `ActionLogEntry` model
3. Infrastructure — `ActionLoggingService`, `CurrentUserContext`
4. Infrastructure `DependencyInjection.cs` — register both services, extend `AppLogging` options with `ActionLogTable`
5. `TenantCore.Api/appsettings.json` — add `ActionLogTable` config value
6. Verify/add `AddHttpContextAccessor()` registration
7. Application — `ActionLoggingBehavior`
8. Application `DependencyInjection.cs` — register the new behavior after `ValidationBehavior`
9. Annotate all ~22 listed command records with `IBusinessAction` + `ActionName`
10. Unit tests

---

## Test Files to Create

All test files live under `tests/TenantCore.Application.Tests/Common/Behaviors/` (new) and `tests/TenantCore.Infrastructure.Tests/Services/` (existing or new, per the centralized-logging-service test project).

| File | What it covers |
|------|---------------|
| `ActionLoggingBehaviorTests.cs` | Request implementing `IBusinessAction` → `LogStartedAsync` then `LogCompletedAsync` called on success, with matching correlation id; on handler exception → `LogFailedAsync` called and the original exception is rethrown unchanged; request NOT implementing `IBusinessAction` → `IActionLogger` never called; `ApplicationId` correctly extracted via reflection when present |
| `ActionLoggingServiceTests.cs` | Correct `ActionLogEntry` fields on Started/Completed/Failed, `DurationMs` set only on Completed/Failed, `ErrorMessage` set only on Failed, swallow-and-continue when the underlying `IAppLogWriter` throws |
| `CurrentUserContextTests.cs` | Returns the user id claim when `HttpContext.User` is authenticated; returns null when there is no `HttpContext` or no claim |

---

## Open Questions / Risks

- Reflection-based `ApplicationId` extraction in `ActionLoggingBehavior` is a small amount of "magic" — acceptable here since it mirrors an existing hard convention (every tenant-scoped command already has an `ApplicationId` property per CLAUDE.md), but flag if a future command needs a non-standard shape.
- No admin UI to browse `ActionLogs` in this plan — this only adds the write path (same as how `centralized-logging-service` initially shipped write-only for errors); a read/browse UI would be a separate future feature.
- Login and "flag actions" are explicitly excluded (see Business Rules 6–7) — revisit if you want a companion Auth-repo plan for login auditing.
