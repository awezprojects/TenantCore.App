# Feature Plan: Subscription Billing Activation — App Repo

**Repo:** TenantCore.App
**Date:** 2026-10-02
**Domain area:** Subscription & billing — activation-time setup, OT billing item, clinic-side dashboards
**Status:** Draft — awaiting approval
**Companion plan:** `TenantCore.Admin/plan/subscription-billing-activation/PLAN.md` — must be read together;
Admin calls the new/extended internal API endpoints this plan adds.

---

## Context

The existing billing infrastructure (Phase 2: `registration-fee-and-usage-billing`) gave admins
post-activation tools: they can change billing mode, set a registration fee, set free test days, and
edit usage rates through the BillingTab. What it does NOT do is capture billing intent at the moment
the clinic is first approved. Today the approval flow only selects a subscription plan and sends a
payment link — there is no way to set up a Pay-as-you-go clinic, no way to set registration fee or
free test days at approval time, and OT (theatre) is marked as "Phase 3" and absent from the billing
system.

This plan finishes that picture:

1. Extend the internal `approve` endpoint so Admin can set billing mode, registration fee, free test
   days, and per-operation rates (including OT) at the moment of approval.
2. Add `OtSurgery` as the fourth `BillingItem` (the "Phase 3" placeholder is now Phase 3).
3. Improve the clinic-facing subscription/billing UI for billing-mode awareness, subscription history,
   and a pay-as-you-go consumption dashboard with PDF and Excel export.
4. Add plan hard-delete (currently only activate/deactivate exists).

---

## What Is Already Built (do not re-implement)

- `BillingMode` enum, `BillingItem` enum (OPD/IPD/Discharge), `ClinicUsageRate`, `ClinicUsageRecord`
- `ClinicWallet`, `ClinicWalletTransaction`, wallet top-up and credit commands
- `SetBillingModeCommand`, `SetRegistrationFeeCommand`, `SetFreeTestDaysCommand`,
  `SetUsageRatesCommand`, `CreditClinicWalletCommand` — all exist and work for post-activation edits
- `/api/internal/billing/*` endpoints — used by BillingTab
- Subscription plan CRUD, `SubscriptionGuardMiddleware`, subscription timeline
- `/subscription` plan picker, `/billing`, `/billing/usage`, `/billing/wallet` pages

---

## Decisions Taken (redirect before execution if wrong)

| # | Decision | Why |
|---|----------|-----|
| 1 | **Extend the existing `ApproveOnboardingRequestCommand`** rather than create a new command | The approval is still one atomic intent; splitting it would create a race window between approve and set-billing |
| 2 | **Billing setup at approval is optional.** If Admin sends no billing fields, the clinic activates exactly as today | Backward-compatible; Admin can still use BillingTab post-activation for fields they forgot |
| 3 | **For Pay-as-you-go approval the registration fee and subscription plan are both null / omitted.** A Pay-as-you-go clinic never goes through the subscription plan selection flow | A PAYG clinic does not have a "subscription plan"; its access is governed by wallet balance |
| 4 | **For Package (subscription) approval, free test days and registration fee are optional per-clinic overrides.** If absent, the plan's own defaults apply, exactly as today | Matches the spec: admin *can* set them but is not forced to |
| 5 | **The `SubscribeAsync` / trial-grant path on the approve handler is reused for free-test-days activation.** Free test days = activate the clinic straight away with `FreeTestDays` as the duration, on a synthetic "Trial" plan row | Consistent with the existing grant-trial path; the clinic is not locked during its free period |
| 6 | **OT is added as `OtSurgery = 4` without renumbering existing values.** The comment "Phase 3" in both enums is removed | Phase 3 is now |
| 7 | **Plan hard-delete requires zero active or upcoming `ClinicSubscription` rows referencing the plan** | Guards billing history integrity; a plan with history is deactivated, not deleted |
| 8 | **Clinic `/subscription` page is split into two views by `BillingMode`**: Subscription mode shows the plan picker; Pay-as-you-go mode shows the billing summary and links to `/billing/usage` | The plan picker is meaningless for a PAYG clinic |
| 9 | **Consumption dashboard = the existing `/billing/usage` page enhanced**, not a new route | Avoids duplication; the route and fetch-once pattern stay |
| 10 | **PDF export = `window.print()` on a dedicated print page** (`/billing/usage/print`). Excel = a client-side `.xlsx` blob via `ClosedXML.Excel` NuGet loaded on demand (server-side streaming endpoint, not WASM) | WASM cannot write binary zip-based files directly; ClosedXML on the API side is the established pattern for this repo's PDF/Excel tasks |

---

## Scope — What Changes

### 1. `BillingItem` enum — add OT

**File:** `TenantCore.Shared/Enums/BillingItem.cs`
- Add `OtSurgery = 4` (remove the "Phase 3" comment).

**Also update every place that switches on `BillingItem`:**
- `TenantCore.Application/Features/UsageBilling/Handlers/ChargeUsageCommandHandler.cs` — add OT case
- `TenantCore.Application/Features/UsageBilling/Handlers/GetUsageSummaryHandler.cs` — include OT in totals
- `TenantCore.Infrastructure/Persistence/Configurations/Clinic/ClinicUsageRateConfiguration.cs` — no change needed (rates are keyed by int)
- `TenantCore.Web.Client/Pages/Billing/Usage.razor` — add OT to item-filter dropdown and stat cards

**Migration:** none needed — `ClinicUsageRates.BillingItem` is a plain `int`; the new value is inserted via the rate-setter, not a DDL change.

---

### 2. Extend the internal approve endpoint

**Request DTO:** `TenantCore.Shared/Dtos/PlatformAdmin/ApproveOnboardingRequest.cs`
Add new optional fields:
```
BillingMode?       BillingModeOverride          // null → Package (default, existing behaviour)
decimal?           RegistrationFeeOverride       // per-clinic fee; null → use plan default
int?               FreeTestDaysOverride          // 30–90; null → use plan default
decimal?           PayAsYouGoOpdRate
decimal?           PayAsYouGoIpdAdmissionRate
decimal?           PayAsYouGoOtSurgeryRate
decimal?           PayAsYouGoIpdDischargeRate
decimal?           PayAsYouGoInitialWalletCredit
```

**Validator:** `ApproveOnboardingRequestCommandValidator`
- If `BillingModeOverride == PayAsYouGo`: require `PlanId` to be null; optionally require at least one rate > 0.
- If `BillingModeOverride == Subscription` or null: existing rules unchanged.
- `FreeTestDaysOverride`: if set, must be 30–90.
- Rate values: ≥ 0 and ≤ 4 decimal places.
- `PayAsYouGoInitialWalletCredit`: ≥ 0.

**Handler:** `TenantCore.Application/Features/Onboarding/Handlers/ApproveOnboardingRequestHandler.cs`
After the clinic is created and activated:
1. If `BillingModeOverride` is provided, call the existing `SetBillingModeCommand` inline.
2. If `RegistrationFeeOverride` is provided, call `SetRegistrationFeeCommand` inline.
3. If `FreeTestDaysOverride` is provided, call `SetFreeTestDaysCommand` inline.
4. If any PAYG rates are provided, call `SetUsageRatesCommand` inline for each non-null rate.
5. If `PayAsYouGoInitialWalletCredit > 0`, call `CreditClinicWalletCommand` inline with
   `ReferenceType = AdminGrant` and the approval request ID as the reference (idempotency).

All five commands already exist and are tested. The handler chains them; each failure rolls back via
the existing `InvalidOperationException` → 409 path, so the approve either completes fully or stops
with a clear error.

**Controller:** `TenantCore.Api/Controllers/Internal/OnboardingController.cs` — no change needed (the
command is reconstructed from the request; the new fields flow through automatically).

---

### 3. Plan hard-delete

**New command:** `TenantCore.Application/Features/PlatformAdmin/Commands/DeleteSubscriptionPlanCommand.cs`

**Handler:** checks `IClinicSubscriptionRepository.AnyActiveOrUpcomingAsync(planId)`.
- If any rows exist → throw `InvalidOperationException("Cannot delete a plan that clinics are using.")`.
- Otherwise → delete from `SubscriptionPlanRepository`.

**Validator:** `DeleteSubscriptionPlanCommandValidator` — `PlanId` must not be empty.

**Controller:** `TenantCore.Api/Controllers/Internal/SubscriptionPlansController.cs`
- Add `DELETE /api/internal/subscription-plans/{id}` → returns 204.

**Tests:**
- `DeleteSubscriptionPlanHandlerTests`: happy path (no subscriptions → deleted), blocked path (active subscription exists).
- `DeleteSubscriptionPlanCommandValidatorTests`: empty guid fails.

---

### 4. Clinic subscription page — billing-mode awareness

**File:** `TenantCore.Web.Client/Pages/Subscription/SubscriptionPlans.razor`

Add a check at page load: if `UsageBillingContext.Summary?.EffectiveBillingMode == BillingMode.PayAsYouGo`:
- Hide the plan picker grid and the pending payment banner entirely.
- Show a "Pay as you go" info card:
  - Current wallet balance (from `UsageBillingContext.Summary.WalletBalance`).
  - Link → `/billing/wallet` (top up).
  - Link → `/billing/usage` (view consumption).
  - Rates at a glance (from `UsageBillingContext.Summary.Rates`).

Add a **Subscription history** section below the plan picker (for both modes):
- Table showing all `ClinicSubscription` rows (existing `GetSubscriptionHistoryQuery`), with plan name,
  start/end dates, price paid, status, and who granted it (if an admin grant).
- Currently only payment history is shown; this adds the term history.

**New query:** `GetSubscriptionHistoryQuery` — already exists; just wire it into the page if not already.

---

### 5. Consumption dashboard — enhance `/billing/usage`

**File:** `TenantCore.Web.Client/Pages/Billing/Usage.razor`

After loading, add above the existing chart:
- **Stat cards** — one per `BillingItem` (OPD / IPD Admission / OT / Discharge):
  - Count of operations in range
  - Amount charged
  - Notional value
- **Wallet balance card** (for PAYG clinics, pulled from `UsageBillingContext.Summary`):
  shows remaining balance prominently.

Add to the header action row:
- **Export Excel** button → calls new `GET /api/billing/usage/export/excel?from=&to=` endpoint
- **Export PDF** button → `href="/billing/usage/print"` (opens `UsagePrint.razor` — already exists)

**New endpoint:** `TenantCore.Api/Controllers/UsageBillingController.cs`
- `GET /api/billing/usage/export/excel`
- Parameters: `from` (DateOnly), `to` (DateOnly), `ApplicationId` (from header).
- Streams an `.xlsx` file using `ClosedXML.Excel`.
- Returns `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.

**New NuGet:** `ClosedXML` (`ClosedXML.Excel`) in `TenantCore.Api.csproj`.

**Handler:** `TenantCore.Application/Features/UsageBilling/Queries/GetUsageForExportQuery.cs`
Returns the same `UsageSummaryDto` as the existing usage query; the controller formats it to Excel.

---

## Tests

| Test file | What it covers |
|-----------|---------------|
| `ApproveOnboardingRequestHandlerTests` (extend) | PAYG approval sets rates + wallet; Package approval sets fee + free days; null billing fields leave clinic at defaults |
| `ApproveOnboardingRequestCommandValidatorTests` (extend) | FreeTestDays out of range (< 30, > 90) fails; PAYG + PlanId together fails; negative rates fail |
| `DeleteSubscriptionPlanHandlerTests` | Active plan → blocked; unused plan → deleted |
| `DeleteSubscriptionPlanCommandValidatorTests` | Empty guid rejected |
| `GetUsageForExportHandlerTests` | Returns correct summary for given range |

---

## Logging (ADR-011 — mandatory)

| Operation | What is logged | Identifiers |
|-----------|---------------|-------------|
| Approve with billing override | `ApproveOnboardingRequestCommand` action log already records it; the billing-mode field is added to `AdditionalContext` | `OnboardingRequestId`, `ApplicationId`, `BillingMode` |
| Delete subscription plan | New `ActionLog` entry: `Action = "DeleteSubscriptionPlan"`, `TargetId = planId` | `PlanId` |
| Excel export | `ApiRequestLog` row (on-by-default request logging) | `ApplicationId`, range |

---

## Migration

One migration in `TenantCore.Infrastructure`:
- None for OT (it is a pure enum/int; no DDL change).
- None for the approve extension (no new columns; the handler calls existing commands).
- None for plan delete (existing table).

No migration needed for this plan.

---

## Execution Order

1. `BillingItem.OtSurgery` enum addition (both repos in same commit pair)
2. Extend `ApproveOnboardingRequestCommand` + validator + handler
3. Plan hard-delete command/handler/controller
4. `/subscription` billing-mode awareness + history section
5. Usage dashboard stat cards + wallet balance card
6. Excel export endpoint + ClosedXML
7. Tests for all of the above
