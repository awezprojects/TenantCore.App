# Feature Plan: Billing Feature Flag (Toggleable Billing Module)

**Repo:** TenantCore.App
**Date:** 2026-09-08
**Domain area:** Clinic Settings / Feature Flags
**Status:** Approved — ready for execution

---

## Overview

Some clinics run TenantCore.App for clinical workflows (OPD/IPD registration, prescriptions, patient records) but keep their financial data — payment collection, particulars/pricing, cash-drawer sessions, expenses, handovers, finance reports — entirely off the cloud application for internal policy reasons. This feature adds a per-clinic `BillingEnabled` toggle, controlled by that clinic's own Clinic Admin from Settings → Feature Flags, extending the existing `ClinicFeatureFlags` entity (the same one that already carries `PrepaidOpdEnabled`). When disabled, every billing-related API route returns `403 Forbidden` regardless of how it's called, the corresponding navigation and UI disappear from the Blazor client, and OPD registration itself stops requiring a counter session or creating payment records — so reception can keep registering and managing patients with zero exposure to billing screens or billing data. The flag defaults to enabled, so no existing clinic's behavior changes on deploy.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Domain | Extend `ClinicFeatureFlags` entity with `BillingEnabled` |
| Infrastructure | EF migration for the new column; no new repository |
| Application | Extend feature-flags command/handler/translator; branch `CreateOpdRegistrationHandler` on the flag |
| API | New `BillingFeatureGuardMiddleware`; extend `ClinicSettingsController` feature-flags endpoints |
| Shared | New `BillingErrorCodes`; extend `ClinicFeatureFlagsDto` / `UpdateClinicFeatureFlagsDto` |
| Web.Client | New `FeatureFlagsContextService`; extend `FeatureFlagsSettings.razor`, `NavMenu.razor`, OPD/IPD registration pages |

No new entity, no new controller, no new DbSet — this is an extension of an existing, already-executed pattern (`prepaid-opd-fee-collection`).

---

## Entity: `ClinicFeatureFlags` (extended, not new)

**Tenant-scoped:** Yes
**Base class:** `BaseEntity` (unchanged)

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, auto-generated (existing) |
| ApplicationId | Guid | FK to clinic — required (existing) |
| PrepaidOpdEnabled | bool | existing, unrelated flag — untouched |
| **BillingEnabled** | **bool** | **new — default `true` on create; existing rows backfilled to `true` by migration** |
| CreatedAt | DateTime | set by EF (existing) |
| UpdatedAt | DateTime | set by EF (existing) |

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Errors/BillingErrorCodes.cs` | Constant(s) for the billing-disabled error code, mirroring `SubscriptionErrorCodes` |

### API Layer (`src/TenantCore.Api/Middleware/`)

| File | Purpose |
|------|---------|
| `BillingFeatureGuardMiddleware.cs` | Blocks requests to billing route prefixes with 403 + `BillingErrorCodes` payload when the resolved clinic has `BillingEnabled = false`. Modeled directly on `SubscriptionGuardMiddleware` — reads `ApplicationId` from `HttpContext.Items` (set by `ClinicContextMiddleware`), looks up `ClinicFeatureFlags` via `IClinicFeatureFlagsRepository`, exempts unauthenticated/no-clinic-header requests exactly as the subscription guard does |

### Web.Client Layer (`src/TenantCore.Web.Client/Services/`)

| File | Purpose |
|------|---------|
| `FeatureFlagsContextService.cs` | Scoped service caching the current clinic's `ClinicFeatureFlagsDto` (loaded once per clinic selection), exposing `IsBillingEnabled` / `IsLoaded` — mirrors `SubscriptionContextService`. Refreshed from `AuthorizedLayout` alongside the existing subscription refresh so every page/nav item can read a synchronous, already-loaded value instead of each doing its own API call |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Domain/Entities/ClinicFeatureFlags.cs` | Add `BillingEnabled` property; extend `Create(...)` and `Update(...)` to accept it (default `true`) |
| `src/TenantCore.Application/Features/ClinicSettings/Commands/UpdateClinicFeatureFlagsCommand.cs` | Add `BillingEnabled` parameter |
| `src/TenantCore.Application/Features/ClinicSettings/Handlers/UpdateClinicFeatureFlagsHandler.cs` | Pass `BillingEnabled` through to `Create`/`Update` |
| `src/TenantCore.Application/Features/ClinicSettings/Translators/ClinicFeatureFlagsTranslator.cs` | Map `BillingEnabled` onto the DTO |
| `src/TenantCore.Application/Features/OpdRegistrations/Handlers/CreateOpdRegistrationHandler.cs` | Load the clinic's feature flags up front; when `BillingEnabled` is `false`, skip the active-counter-session requirement entirely and skip the `EnsureOpdPaymentCommand` / `AcceptVisitFee` calls — OPD registration proceeds as a pure clinical record with no payment side-effects. When `true`, behavior is unchanged from today |
| `src/TenantCore.Shared/Dtos/ClinicFeatureFlagsDto.cs` | Add `BillingEnabled` to `ClinicFeatureFlagsDto` and `UpdateClinicFeatureFlagsDto` |
| `src/TenantCore.Api/Controllers/ClinicSettingsController.cs` | Pass `dto.BillingEnabled` into `UpdateClinicFeatureFlagsCommand` (existing `feature-flags` GET/PUT endpoints — no new routes) |
| `src/TenantCore.Api/Program.cs` | Register `BillingFeatureGuardMiddleware` in the pipeline, after `ClinicContextMiddleware`, alongside `SubscriptionGuardMiddleware` — additive, does not reorder existing middleware |
| `src/TenantCore.Web.Client/Pages/Settings/FeatureFlagsSettings.razor` | Add a second toggle row: "Billing" with description covering payment collection, particulars, finance reports, and cash management; wired to the same `UpdateFeatureFlagsAsync` call with both flags |
| `src/TenantCore.Web.Client/Layout/NavMenu.razor` | Hide Finance Dashboard, Particulars, Expense Categories, and Counter Session (session/expenses/handovers) nav links when `FeatureFlagsContext.IsBillingEnabled` is `false` |
| `src/TenantCore.Web.Client/Layout/AuthorizedLayout.razor` | Call `FeatureFlagsContext.RefreshAsync()` alongside the existing `SubscriptionContext.RefreshAsync()` on clinic selection |
| OPD registration / visit page(s) that show fee input, payment status, or "open a counter session" messaging | Hide those elements when billing is disabled |
| IPD registration page | Hide the `InitialFee` field when billing is disabled (field defaults to 0, no domain/entity change needed — it's set-once at creation and never validated as required elsewhere) |

---

## API Endpoints

No new endpoints. Existing endpoints change behavior only:

| Method | Route | Change |
|--------|-------|--------|
| GET | `api/clinic-settings/feature-flags` | Response now includes `billingEnabled` |
| PUT | `api/clinic-settings/feature-flags` | Request body now includes `billingEnabled`; `RequireClinicAdmin` policy unchanged |

### Routes newly gated by `BillingFeatureGuardMiddleware` (return 403 when `BillingEnabled = false`)

| Route Prefix | Controller |
|---------------|-----------|
| `/api/opd-payments` | OpdPaymentsController |
| `/api/opd-particulars` | OpdParticularsController |
| `/api/particulars` | ParticularsController |
| `/api/finance-reports` | FinanceReportsController |
| `/api/counter-sessions` | CounterSessionsController |
| `/api/expense-categories` | ExpenseCategoriesController |
| `/api/expense-records` | ExpenseRecordsController |
| `/api/amount-handovers` | AmountHandoversController |

`/api/clinic-settings/feature-flags` itself stays exempt (a clinic must always be able to reach the screen that turns billing back on), matching how `/api/subscriptions` stays exempt from the subscription guard.

---

## Validation Rules

No new user-input validation — `BillingEnabled` is a plain boolean toggle with no format/length constraints, same as `PrepaidOpdEnabled`.

---

## Business Rules

1. When `BillingEnabled` is `false`, every request under a gated route prefix (see table above) is rejected with `403 Forbidden` and `BillingErrorCodes.BillingDisabled`, regardless of role or how the request is made — this is the actual security boundary, independent of the UI.
2. When `BillingEnabled` is `false`, OPD registration no longer requires an active Counter Session and no longer auto-creates or collects an `OpdPayment` — throws no error, simply skips the billing side-effects that exist today in `CreateOpdRegistrationHandler`.
3. When `BillingEnabled` is `true` (default), all existing behavior — Counter Session requirement, auto-created `OpdPayment`, Prepaid OPD auto-collection — is completely unchanged.
4. Toggling `BillingEnabled` off does not delete or hide historical billing data in the database — it only blocks the API surface and UI going forward. (No data purge is in scope; a clinic can re-enable and its history reappears.)
5. IPD's `InitialFee` field is hidden client-side when billing is disabled; the value is not required and defaults to 0 — no domain change needed since it's immutable after creation today.

---

## Multi-Tenancy Checklist

- [x] `ApplicationId` already present on `ClinicFeatureFlags`
- [x] `ApplicationId` already passed through `GetClinicFeatureFlagsQuery` / `UpdateClinicFeatureFlagsCommand`
- [x] `IClinicFeatureFlagsRepository.GetByApplicationAsync` already filters by `applicationId`
- [x] `BillingFeatureGuardMiddleware` resolves `ApplicationId` from `ClinicContextMiddleware`'s validated header — no per-clinic bypass possible
- [x] `ClinicSettingsController` already uses `GetApplicationId()` from `ClinicControllerBase`
- [x] Blazor client already sends `X-Application-Id` on every clinic-scoped call

---

## EF Migration

**Migration name:** `AddBillingEnabledToClinicFeatureFlags`

Run after `ClinicFeatureFlags` entity and its configuration are updated:
```
dotnet ef migrations add AddBillingEnabledToClinicFeatureFlags --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

Column added with a `DEFAULT 1` constraint so existing rows backfill to enabled — zero behavior change for clinics that don't touch the new toggle.

---

## Implementation Order

1. Shared — `BillingErrorCodes`, extend `ClinicFeatureFlagsDto` / `UpdateClinicFeatureFlagsDto`
2. Domain — extend `ClinicFeatureFlags` entity (`BillingEnabled` on `Create`/`Update`)
3. Application — extend `UpdateClinicFeatureFlagsCommand`, `UpdateClinicFeatureFlagsHandler`, `ClinicFeatureFlagsTranslator`
4. Run EF migration — add `BillingEnabled` column
5. API — extend `ClinicSettingsController` feature-flags endpoints
6. API — add `BillingFeatureGuardMiddleware`, register in `Program.cs`
7. Application — modify `CreateOpdRegistrationHandler` to branch on `BillingEnabled`
8. Web.Client — add `FeatureFlagsContextService`, wire refresh into `AuthorizedLayout`
9. Web.Client — extend `FeatureFlagsSettings.razor` with the Billing toggle
10. Web.Client — hide nav items in `NavMenu.razor`, hide fee/payment UI in OPD/IPD registration pages
11. Unit tests — middleware guard behavior, handler branch, validator/translator coverage
12. Manual smoke test: toggle off → confirm OPD/IPD registration still works, all eight route prefixes return 403, nav links disappear; toggle back on → confirm full billing flow still works exactly as before

---

## Test Files to Create

All test files live under `tests/TenantCore.Application.Tests/` (and a new `tests/TenantCore.Api.Tests/` location if middleware tests need a hosted-server harness — otherwise cover the guard logic at the unit level by extracting its route-matching/flag-check logic into a testable form).

| File | What it covers |
|------|---------------|
| `Features/ClinicSettings/Commands/UpdateClinicFeatureFlagsHandlerTests.cs` | `BillingEnabled` persisted on both create-new-flags and update-existing-flags paths; `PrepaidOpdEnabled` unaffected by the new field |
| `Features/ClinicSettings/Translators/ClinicFeatureFlagsTranslatorTests.cs` | `BillingEnabled` mapped onto DTO in both `true`/`false` states |
| `Features/OpdRegistrations/Commands/CreateOpdRegistrationHandlerTests.cs` (extend existing) | New cases: `BillingEnabled = false` → registration succeeds with no active counter session and no `OpdPayment`/`EnsureOpdPaymentCommand` side-effects; `BillingEnabled = true` (or flags row missing, defaulting true) → existing behavior (counter-session requirement, payment creation) unchanged |
| `Middleware/BillingFeatureGuardMiddlewareTests.cs` (or equivalent) | Each of the eight gated prefixes blocked with 403 when disabled; `feature-flags` route itself stays exempt; unauthenticated/no-clinic-header requests pass through untouched; all prefixes pass through when enabled |

---

## Open Questions / Risks

- **Confirmed with you:** disabling billing changes the long-standing `CreateOpdRegistrationHandler` behavior (drops the counter-session requirement and payment auto-creation) — this is intentional and required for OPD registration to keep working when billing is off, but it's the highest-blast-radius change in this plan and should get focused test coverage and a manual smoke test before shipping.
- Historical billing data is not purged or hidden retroactively when the flag is toggled off — only forward API/UI access is blocked. If a hospital's compliance requirement is stricter (e.g., "no billing data should exist in the cloud DB at all"), that's a separate, larger conversation (data residency / non-collection) outside this plan's scope.
- IPD has no real billing module today beyond the single `InitialFee` field — if IPD billing is built out later, its endpoints should be added to the gated-route-prefix list in `BillingFeatureGuardMiddleware` at that time.
