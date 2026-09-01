# Feature Plan: Prepaid OPD Fee Collection & Refund Workflow

**Repo:** TenantCore.App
**Date:** 2026-08-31
**Domain area:** OPD Registrations / OPD Payments / Clinic Settings
**Status:** Executed 2026-08-31

---

## Overview

Clinics currently collect the OPD visit fee at some point after the appointment is booked, via a manual "Collect Visit Fee" action on the payment. This feature adds a per-clinic toggle — "Prepaid OPD Fee Collection" — that, when enabled (the default), automatically marks the visit fee as collected the moment a receptionist books an OPD appointment, instead of leaving it Pending. Because money now changes hands at booking time, cancelling an appointment needs a way to unwind that: the doctor can apply a discount to an already-collected payment to flag a refund as due, reception then confirms the refund was physically handed back, and only after that (or if nothing was ever collected) can the receptionist permanently delete the cancelled OPD record. The toggle lives in a new extensible clinic Settings/Feature Flags area so more preferences can be added the same way later. Clinic admins interact with the flags settings page; receptionists interact with booking, refund-processing, and delete; doctors interact with the existing discount dialog, now refund-aware.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Domain | New `ClinicFeatureFlags` entity + repository interface; `OpdPayment` gains refund fields/behavior; new `RefundStatus` enum |
| Infrastructure | EF config + repository for `ClinicFeatureFlags`; migration |
| Application | New commands/queries/handlers for flags settings, OPD delete, and refund processing; changes to OPD registration creation and discount application |
| API | New endpoints on `ClinicSettingsController`, `OpdRegistrationsController` (delete), `OpdPaymentsController` (refund) |
| Shared | New DTOs for flags and refund |
| Web.Client | New Feature Flags settings section; Delete action on OPD list; refund-aware discount dialog + "Process Refund" action |

---

## Entity: ClinicFeatureFlags

**Tenant-scoped:** Yes
**Base class:** BaseEntity (singleton per clinic, get-or-create — same pattern as `ClinicFeeConfig`)

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, auto-generated |
| ApplicationId | Guid | FK to clinic — required, unique per clinic |
| PrepaidOpdEnabled | bool | required, default true |
| CreatedAt | DateTime | set by EF |
| UpdatedAt | DateTime | set by EF |

---

## Entity Change: OpdPayment (existing)

Add refund-tracking fields and behavior, no new file — modifies `src/TenantCore.Domain/Entities/OpdPayment.cs`.

| New Property | Type | Notes |
|--------------|------|-------|
| RefundDue | decimal | Amount still owed back to the patient; 0 when nothing is due |
| RefundStatus | RefundStatus (enum) | None (default) / PendingRefund / Refunded |
| RefundedAt | DateTime? | Set when refund is processed |
| RefundedByUserId | Guid? | Set when refund is processed |

**Behavior changes:**
- `ApplyDiscount`: if `PaymentStatus == Received` and the new `FinalAmount` is less than the current `CollectedAmount`, set `RefundDue = CollectedAmount - FinalAmount` and `RefundStatus = PendingRefund`. If payment is still Pending (not yet collected), behaves exactly as today — no refund implied, since nothing was taken yet.
- New method `ProcessRefund(refundedByUserId)`: requires `RefundStatus == PendingRefund`, else throws `InvalidOperationException`. Sets `CollectedAmount -= RefundDue`, `RefundedAt = UtcNow`, `RefundedByUserId`, `RefundStatus = Refunded`, `RefundDue = 0`.

---

## New Enum: RefundStatus

| Value | Meaning |
|-------|---------|
| None = 1 | No refund ever needed (default) |
| PendingRefund = 2 | Doctor's discount reduced an already-collected payment; cash owed back to patient |
| Refunded = 3 | Reception has confirmed the refund was handed back |

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Enums/RefundStatus.cs` | None / PendingRefund / Refunded |
| `Dtos/ClinicFeatureFlagsDto.cs` | Read response — Id, ApplicationId, PrepaidOpdEnabled |
| `Dtos/UpdateClinicFeatureFlagsDto.cs` | PUT body — PrepaidOpdEnabled |

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Entities/ClinicFeatureFlags.cs` | Singleton-per-clinic settings entity — Create/Update(PrepaidOpdEnabled) |
| `Interfaces/IClinicFeatureFlagsRepository.cs` | Extends IRepository<ClinicFeatureFlags>, adds GetByApplicationAsync(applicationId) |

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Persistence/Configurations/Clinic/ClinicFeatureFlagsConfiguration.cs` | Fluent API config — unique index on ApplicationId |
| `Repositories/ClinicFeatureFlagsRepository.cs` | Implements IClinicFeatureFlagsRepository — extends ClinicRepository<ClinicFeatureFlags> |

### Application Layer — Clinic Settings (`src/TenantCore.Application/Features/ClinicSettings/`)

| File | Purpose |
|------|---------|
| `Queries/GetClinicFeatureFlagsQuery.cs` | Read query — ApplicationId; returns flags, creating a default row if none exists |
| `Commands/UpdateClinicFeatureFlagsCommand.cs` | Write command — ApplicationId + PrepaidOpdEnabled |
| `Handlers/GetClinicFeatureFlagsHandler.cs` | Get-or-create flags row, returns DTO |
| `Handlers/UpdateClinicFeatureFlagsHandler.cs` | Get-or-create flags row, applies update, saves |
| `Translators/ClinicFeatureFlagsTranslator.cs` | Static ToDto mapping |

### Application Layer — OPD Registrations (`src/TenantCore.Application/Features/OpdRegistrations/`)

| File | Purpose |
|------|---------|
| `Commands/DeleteOpdRegistrationCommand.cs` | Id, ApplicationId — removes a cancelled, refund-clear OPD registration |
| `Handlers/DeleteOpdRegistrationHandler.cs` | Validates Status == Cancelled and payment has no outstanding collected/refund-due balance, then cascades delete of OpdParticulars, OpdPayment, OpdRegistration |

### Application Layer — OPD Payments (`src/TenantCore.Application/Features/OpdPayments/`)

| File | Purpose |
|------|---------|
| `Commands/ProcessOpdRefundCommand.cs` | OpdRegistrationId, ApplicationId, RefundedByUserId — confirms cash was returned |
| `Handlers/ProcessOpdRefundHandler.cs` | Loads payment, calls ProcessRefund, saves, returns updated DTO |

### API Layer (`src/TenantCore.Api/Controllers/`)

No new controller files — new endpoints added to existing controllers (see "Files to Modify").

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Domain/Entities/OpdPayment.cs` | Add RefundDue/RefundStatus/RefundedAt/RefundedByUserId fields; update ApplyDiscount; add ProcessRefund method |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/OpdPaymentConfiguration.cs` | Configure new refund columns |
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add `DbSet<ClinicFeatureFlags> ClinicFeatureFlags` |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `IClinicFeatureFlagsRepository → ClinicFeatureFlagsRepository` as Scoped |
| `src/TenantCore.Application/Features/OpdRegistrations/Commands/CreateOpdRegistrationCommand.cs` | Add `ReceivedByUserId` (Guid) field |
| `src/TenantCore.Application/Features/OpdRegistrations/Handlers/CreateOpdRegistrationHandler.cs` | After ensuring the payment exists, read `ClinicFeatureFlags.PrepaidOpdEnabled`; if true, send `AcceptOpdPaymentCommand` to immediately collect the visit fee using the active counter session and the booking user |
| `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` | Pass `GetCurrentUserId()` into `CreateOpdRegistrationCommand`; add `DELETE /{id}` endpoint calling `DeleteOpdRegistrationCommand` |
| `src/TenantCore.Api/Controllers/ClinicSettingsController.cs` | Add `GET /feature-flags` and `PUT /feature-flags` endpoints |
| `src/TenantCore.Api/Controllers/OpdPaymentsController.cs` | Add `POST /{id}/refund` endpoint calling `ProcessOpdRefundCommand` |
| `src/TenantCore.Web.Client/Clients/ClinicApiClient.cs` (or dedicated flags client interface) | Add GetFeatureFlagsAsync / UpdateFeatureFlagsAsync |
| `src/TenantCore.Web.Client/Clients/IOpdRegistrationApiClient.cs` + implementation | Add DeleteAsync |
| `src/TenantCore.Web.Client/Clients/IOpdPaymentApiClient.cs` + implementation | Add ProcessRefundAsync |
| `src/TenantCore.Web.Client/Pages/Settings/*` (new page or existing settings hub) | Add "Feature Flags" section with the Prepaid OPD toggle |
| `src/TenantCore.Web.Client/Pages/Opd/OpdRegistrationList.razor` | Add Delete button (visible only when Status == Cancelled and no refund outstanding); confirmation prompt before delete |
| `src/TenantCore.Web.Client/Components/OpdDiscountDialog.razor` | Show RefundDue/RefundStatus when present; when PendingRefund, offer a "Confirm Refund Returned" action calling ProcessRefundAsync |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| GET | `api/clinic-settings/feature-flags` | — | `ClinicFeatureFlagsDto` | RequireAuthenticated |
| PUT | `api/clinic-settings/feature-flags` | `UpdateClinicFeatureFlagsDto` | `ClinicFeatureFlagsDto` | RequireClinicAdmin |
| DELETE | `api/opdregistrations/{id}` | — | 204 No Content | RequireReception |
| POST | `api/opdpayments/{id}/refund` | — | `OpdPaymentDto` | RequireReception |

---

## Validation Rules

| Field | Rules |
|-------|-------|
| ApplicationId (flags update) | NotEmpty — always required |
| OpdRegistration delete | Must exist (404); Status must be Cancelled (409 otherwise); associated OpdPayment must not be Received with RefundStatus other than Refunded/None-with-nothing-collected (409 "refund the collected amount before deleting") |
| Process refund | Payment must exist (404); RefundStatus must be PendingRefund (409 otherwise) |

---

## Business Rules

Rules enforced in handlers — throw named domain exceptions on violation:

1. When `PrepaidOpdEnabled` is true, booking an OPD registration auto-collects the visit fee against the active counter session — throws the existing `InvalidOperationException` ("No active counter session…") if none is open, same as today's manual collection path.
2. Applying a discount to a payment that has already been collected (`PaymentStatus == Received`) does not silently move money — it sets `RefundDue`/`RefundStatus = PendingRefund` for reception to action, via `OpdPayment.ApplyDiscount`.
3. A refund can only be processed once per discount event — `ProcessRefund` throws `InvalidOperationException` if `RefundStatus != PendingRefund`.
4. A cancelled OPD registration can only be deleted once there is no outstanding collected balance — i.e., payment was never collected, or any collected amount has already been fully refunded (`RefundStatus == Refunded` or nothing was ever `Received`). Delete throws `InvalidOperationException` otherwise.
5. Deleting an OPD registration cascades: `OpdParticular` rows, then `OpdPayment`, then `OpdRegistration` itself — in that order, within the same handler.
6. Finance/collection totals require no extra bookkeeping — they already derive from `OpdPayment.CollectedAmount`, so `ProcessRefund` reducing that field keeps every report correct automatically.

---

## Multi-Tenancy Checklist

- [x] `ApplicationId` property present on `ClinicFeatureFlags`
- [x] `ApplicationId` passed in all new commands and queries
- [x] Repository queries filter by `applicationId`
- [x] Controllers use `GetApplicationId()` / `GetCurrentUserId()` from `ClinicControllerBase`
- [x] Blazor client sends `X-Application-Id` header (existing typed clients already do this)

---

## EF Migration

**Migration name:** `AddClinicFeatureFlagsAndOpdPaymentRefund`

Run after all infrastructure files are created and `OpdPayment`/`ClinicDbContext` changes are made:
```
dotnet ef migrations add AddClinicFeatureFlagsAndOpdPaymentRefund --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

---

## Implementation Order

Execute in this sequence to avoid compile errors:

1. Shared: `RefundStatus` enum, `ClinicFeatureFlagsDto`, `UpdateClinicFeatureFlagsDto`
2. Domain: `ClinicFeatureFlags` entity, `IClinicFeatureFlagsRepository`
3. Domain: modify `OpdPayment` — refund fields, `ApplyDiscount` change, `ProcessRefund` method
4. Infrastructure: `ClinicFeatureFlagsConfiguration`, `ClinicFeatureFlagsRepository`; modify `OpdPaymentConfiguration`
5. Modify `ClinicDbContext` — add `ClinicFeatureFlags` DbSet
6. Modify Infrastructure `DependencyInjection` — register new repository
7. Application: flags query/command/handlers/translator
8. Application: `DeleteOpdRegistrationCommand`/Handler
9. Application: `ProcessOpdRefundCommand`/Handler
10. Application: modify `CreateOpdRegistrationCommand`/Handler for auto-accept behavior
11. API: modify `ClinicSettingsController`, `OpdRegistrationsController`, `OpdPaymentsController`
12. Web.Client: typed API client methods (flags, delete, refund)
13. Web.Client: Settings Feature Flags section; OPD list Delete action; discount dialog refund UI
14. Unit tests — all handler, validator, and translator test files
15. Run EF migration

---

## Test Files to Create

All test files live under `tests/TenantCore.Application.Tests/Features/`.

| File | What it covers |
|------|---------------|
| `ClinicSettings/Queries/GetClinicFeatureFlagsHandlerTests.cs` | Returns existing flags; creates default (PrepaidOpdEnabled=true) row when none exists; cross-tenant isolation |
| `ClinicSettings/Commands/UpdateClinicFeatureFlagsHandlerTests.cs` | Updates PrepaidOpdEnabled on existing row; creates then updates when no row exists |
| `OpdRegistrations/Commands/DeleteOpdRegistrationHandlerTests.cs` | Happy path delete (Cancelled + no outstanding balance) cascades correctly; EntityNotFoundException when not found; InvalidOperationException when Status != Cancelled; InvalidOperationException when payment Received and RefundStatus != Refunded; cross-tenant entity treated as not found |
| `OpdRegistrations/Handlers/CreateOpdRegistrationHandlerTests.cs` (extend existing or new) | When PrepaidOpdEnabled=true, payment is auto-collected against active session; when false, payment remains Pending as today |
| `OpdPayments/Commands/ProcessOpdRefundHandlerTests.cs` | Happy path — CollectedAmount reduced by RefundDue, RefundStatus becomes Refunded, RefundedAt/RefundedByUserId set; NotFoundException when payment missing; InvalidOperationException when RefundStatus != PendingRefund |
| `OpdPayments/Translators/OpdPaymentTranslatorTests.cs` (extend existing) | ToDto maps RefundDue/RefundStatus/RefundedAt/RefundedByUserId |
| `ClinicSettings/Translators/ClinicFeatureFlagsTranslatorTests.cs` | ToDto maps all fields |

Also extend the existing `OpdPayment` domain unit tests (if present under Domain tests) or add coverage in the handler tests above for: `ApplyDiscount` on a Pending payment sets no refund; `ApplyDiscount` on a Received payment sets RefundDue/PendingRefund correctly; `ProcessRefund` throws when not PendingRefund.

---

## Open Questions / Risks

- Auth policy names `RequireReception` are assumed to exist per `AuthorizationConstants` (used elsewhere for OPD/particular collection endpoints) — confirm during execution that the constant name matches exactly.
- The Web.Client Settings navigation currently has `PrescriptionSettings`, `ClinicProfile`, `DosageRemarkSettings` — during execution, decide whether Feature Flags becomes a new page or a section within `ClinicProfile.razor`; this plan assumes a new page for clarity and future extensibility.
- No dependency on TenantCore.Auth — this feature is entirely within TenantCore.App's tenant-scoped data.
