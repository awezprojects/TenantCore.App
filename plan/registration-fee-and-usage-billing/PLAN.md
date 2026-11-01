# Feature Plan: Registration Fee & Usage Billing (Phase 2)

**Repo:** TenantCore.App
**Date:** 2026-10-01
**Domain area:** Platform billing — package-wise subscription extras (registration fee, free test days, plan switching) and pay-as-you-go metered usage billing against a prepaid wallet
**Status:** Draft — awaiting approval
**Phase:** 2 of 3. Phase 1 `clinic-subscription-admin-controls` was executed 2026-09-30. Phase 3 `usage-billing-autopay` is a separate, not-yet-written plan.
**Paired plan:** `TenantCore.Admin/plan/registration-fee-and-usage-billing/PLAN.md`. **Execute and migrate this App plan first** — the portal reads the new tables and calls the new internal endpoints.
**Blocker to note now:** the Admin repo's **Phase 1** (`clinic-subscription-admin-controls`) is approved but **not yet executed**. Its Phase 2 work cannot compile or run until Phase 1 lands there. Sequence: App P1 ✅ → App P2 (this) → Admin P1 → Admin P2.
**TenantCore.Auth:** not affected. `PaymentPurpose`/enum additions are an App↔Admin contract only.
**Extends (registry):** "Clinic Subscription & Access Gating", "Approval-Based Clinic Onboarding & Razorpay Subscriptions", "Clinic Subscription Admin Controls (Phase 1)". Nothing is duplicated; all three are extended.

---

## Overview

Phase 1 decided **who may use the app and for how long**. Phase 2 decides **what a clinic pays for**.

Two independent halves, one page area (`/subscription`), one internal admin surface (`api/internal/clinics/*`):

**A. Package-wise subscription extras.**
- A **registration fee** that is charged **once, ever, per clinic** — a default on the plan catalogue, overridable per clinic, waivable by an Operator with a reason.
- **Free test days**: a package (or one clinic) may grant a free trial of **30–90 days**, overriding the Trial plan's `DurationDays`, recorded once (`TrialStartedAt`).
- **Plan switching** while a term is running, taking effect when coverage ends — the same no-gap rule Phase 1 established for renewals and admin grants.
- A **billing-mode switch** (subscription ⇄ pay-as-you-go) that also queues to the end of the current coverage, so a clinic can never escape days it has already paid for.

**B. Pay-as-you-go metered billing.**
A clinic on `BillingMode.PayAsYouGo` is not charged a subscription at all. Instead three clinic operations are metered at admin-set rates and debited from a prepaid wallet:

| # | Metered operation | Where it is charged |
|---|---|---|
| 1 | OPD registration | `CreateOpdRegistrationHandler` |
| 2 | IPD admission | `CreateIpdRegistrationHandler` |
| 3 | IPD discharge | `DischargePatientHandler` |

Each charge appends exactly one row to a dedicated `ClinicUsageRecord` ledger (snapshotting the rate that applied) and debits `ClinicWallet` — **in the same `SaveChanges` as the registration itself**, so a charge can never exist without the operation and an operation can never exist un-charged.

- **An empty wallet never locks the app.** Only the three metered creates fail, with 403 `insufficient_wallet_balance`. Every other page keeps working; a clinic can always reach its wallet screen to top up.
- **The clinic tops up itself** through a Razorpay payment link it requests on `/subscription/wallet` (`PaymentPurpose.WalletTopUp`), exactly the way renewals work today.
- **Subscription-mode clinics are metered too, but free.** Their usage rows are written with `PayerMode = Subscription`, `Amount = 0`, `IsFree = true`, and the published `UnitRate` snapshotted — so the platform gets real usage analytics for every clinic, not only the prepaid ones.
- **OT / surgery is deliberately out of scope.** No OT/surgery entity exists in the domain (verified 2026-10-01); inventing one would be a clinical feature, not a billing feature. `ClinicUsageRate` is row-based precisely so Phase 3 can add OT as *data* plus one enum value.

---

## Naming — read before touching anything

The word **"Billing" is already taken** in this repo. `ClinicFeatureFlags.BillingEnabled`, `BillingFeatureGuardMiddleware`, `BillingErrorCodes.BillingDisabled` / `billing_disabled` and the `billing-feature-flag` plan all mean **the clinic's patient-facing billing module** (OPD payments, counter sessions, expenses, handovers). Platform billing must never be confused with it, so every new name in this plan is prefixed **`UsageBilling`**:

| Concern | Name used here | Never |
|---|---|---|
| Feature folder | `Features/UsageBilling/` | `Features/Billing/` |
| Clinic route prefix | `/api/usage-billing/*` | `/api/billing/*` |
| Config section | `UsageBilling` | `Billing` |
| Error codes | `UsageBillingErrorCodes` | `BillingErrorCodes` |
| Context cache (client) | `UsageBillingContextService` | `BillingContextService` |

**Guard placement (both matter, they are opposite decisions):**
- `/api/usage-billing` **is** added to `SubscriptionGuardMiddleware.ExemptPathPrefixes` — a locked or pay-as-you-go clinic must be able to reach the screen that fixes its billing state.
- `/api/usage-billing` is **deliberately NOT** added to `BillingFeatureGuardMiddleware.GuardedPathPrefixes` — a clinic with the patient-billing module switched off must still be able to pay the platform.

---

## Decisions Taken (redirect before execution if wrong)

| # | Decision | Why |
|---|----------|-----|
| 1 | **Three metered operations only:** `BillingItem.OpdRegistration`, `BillingItem.IpdAdmission`, `BillingItem.IpdDischarge` | These are the only three "a patient was taken in/out" events that already exist as handler hooks. OT has no entity |
| 2 | **Rates are rows, not columns.** `ClinicUsageRate` holds one row per (clinic, `BillingItem`) with its own `decimal(18,4)` rate | Phase 3 adds OT/surgery as data + one enum value, with no schema change. Three fixed columns would force a migration per new item |
| 3 | **A dedicated `ClinicUsageRecord` ledger**, never a count over `OpdRegistrations`/`IpdRegistrations` | Survives cancellations, snapshots the price actually applied, and gives subscription-mode clinics usage analytics for free. Counting source tables can do none of that |
| 4 | **`ChargeUsageCommand` is an internal sub-command dispatched through `ISender`** from the three handlers (the `EnsureOpdPaymentCommand` precedent) and **stages its rows without calling `SaveChanges`**, so the usage row + wallet debit commit inside the parent handler's single `SaveChangesAsync` | Atomicity: either the operation and its charge both exist, or neither does. Mirrors the existing sub-command pattern |
| 5 | **A zero wallet never locks the app.** Only the three metered creates return 403 `insufficient_wallet_balance` | The clinic must always be able to open the app and top up. Locking out a clinic that owes ₹20 of usage would be a support incident, not a control |
| 6 | **`BillingMode` defaults to `Subscription`**, and `ClinicAccount` rows are still created lazily | Existing clinics are untouched. 46 DbSets → 50; no backfill, no data migration |
| 7 | **Idempotency by unique index + snapshots**, not by counters: `(ApplicationId, BillingItem, SourceEntityId)` unique on usage, `(ApplicationId, ReferenceType, ReferenceId)` unique on wallet transactions | A retried request, a duplicated workflow task or a replayed webhook can never double-charge |
| 8 | **Wallet debits use `ClinicWallet.RowVersion` optimistic concurrency, not `ExecuteUpdateAsync ... WHERE Balance >= @amount`** | **Refinement of the pre-plan sketch.** `ExecuteUpdateAsync` bypasses the change tracker and runs *outside* the parent's `SaveChangesAsync` transaction, which decision 4 requires. The RowVersion makes the same check-and-write safe inside the one transaction. A lost race surfaces as `DbUpdateConcurrencyException` → the same 403 |
| 9 | **Money types:** rates `decimal(18,4)`, all amounts `decimal(18,2)`, currency `INR` | Rates need sub-paisa precision for per-operation pricing; wallet arithmetic must not accumulate rounding drift |
| 10 | **Registration fee is collected through its own Razorpay link** (`PaymentPurpose.RegistrationFee = 5`), created by the Clinic Admin, and **waived** by an Operator through the internal API. It never rides inside a plan payment | A separate purpose leaves the existing onboarding/renewal amount arithmetic, the Admin amount-reason rule and the reconciliation sweep untouched. It is also the only way to charge the fee to a pay-as-you-go clinic, which has no plan payment at all |
| 11 | **Wallet top-ups reuse `SubscriptionPayment`** with `PaymentPurpose.WalletTopUp = 4`; therefore `SubscriptionPayment.SubscriptionPlanId`, `PlanCode` and `PlanName` become **nullable** | One payments table, one webhook path, one reconciliation sweep, one Admin payments page. Nullable plan columns are the cost, and every reader gets a null-guard |
| 12 | **A handler that must refuse *with a code* gets a new `ErrorCodeException`** (`StatusCode` + `ErrorCode`), mapped by the existing `ExceptionHandlingMiddleware` into `problemDetails.Extensions["errorCode"]` | Today only the two guard middlewares emit `errorCode`; `SubscriptionErrorCodes.TrialAlreadyUsed`/`SubscriptionAlreadyActive` are declared but never surfaced. The client's `ApiResponseReader` already reads `errorCode`, so this is additive on both sides |
| 13 | **Switching billing mode queues to coverage end** (`PendingBillingMode` + `PendingBillingModeEffectiveFrom`, resolved by a pure `EffectiveBillingMode(utcNow)`), with an Admin "apply now" override | A clinic mid-term cannot escape days it has already paid for, and the guard and the metering command need no background job to agree on which mode is live |
| 14 | **`ClinicAccount` gains 11 columns, not the 7 sketched pre-plan** — the waive action stores who waived it and why (`RegistrationFeeWaivedByAdminEmail`, `RegistrationFeeWaiveReason`) | Phase 1 set the precedent with `GrantedByAdminEmail` + `GrantReason`. Storing only a bool loses the trail the Admin clinic-detail card must display |
| 15 | **Export is print-then-CSV**, never a server-side generator: an ADR-007 print page plus a client-side CSV blob with formula-injection escaping | `TenantCore.Web.Client` has no QuestPDF/ClosedXML, and ADR-007 is the established pattern (`PrescriptionPrint.razor`) |

---

## Background (verified 2026-10-01)

**What Phase 1 already gives us (do not rebuild):**
- `ClinicAccount` (one row per clinic, lazily created, absence = Active + public catalogue) with suspension state and `RestrictToOfferedPlans`; `IsSuspendedAsync` in the guard.
- `ClinicPlanOffer`, `SubscriptionPlan.IsPublic`, admin grants, admin payment links, cancel-upcoming with re-chaining, and `ClinicPlanCatalog` as the single source of truth for visibility and effective price.
- `SubscriptionGuardMiddleware` enforcing suspension unconditionally, then the current-term check behind `Subscription:GuardEnabled`.
- `SubscriptionPayment` with `Purpose`, `AdminAssigned`, `AmountReason`, `ClinicName`, `LastCheckAt` and a "check now" endpoint with a 30-second throttle.
- A durable-outbox `WorkflowTask` pipeline (`ConfirmPaymentTaskHandler` → `ActivateSubscriptionTaskHandler`) plus a 15-minute Razorpay reconciliation sweep and a webhook inbox — **wallet credits and the registration fee ride this existing pipeline, not a new one**.

**Facts established this session (source-verified, so the executor does not have to re-derive them):**

| Fact | Where verified |
|---|---|
| `ClinicDbContext` has **46** DbSets (last: `PaymentWebhookEvents`) | `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` |
| `SubscriptionErrorCodes` holds only `subscription_required`, `clinic_suspended`, `trial_already_used`, `subscription_already_active` — and the last two are not surfaced anywhere | `src/TenantCore.Shared/Errors/SubscriptionErrorCodes.cs` |
| `ExceptionHandlingMiddleware` maps `ValidationException`→400, `NotFoundException`→404, `DomainException`→400, `InvalidOperationException`→409, SQL 2601/2627→409. **It never writes `errorCode`** | `src/TenantCore.Api/Middleware/ExceptionHandlingMiddleware.cs` |
| `errorCode` is written today only by `SubscriptionGuardMiddleware` (`clinic_suspended`) and `BillingFeatureGuardMiddleware` (`billing_disabled`), both by building `ProblemDetails` by hand | both middlewares |
| `ClinicControllerBase` exposes `GetApplicationId()`, `GetCurrentUserId()`, `GetCurrentUserRoles()`; controllers stay thin and never catch exceptions | `src/TenantCore.Api/Controllers/ClinicControllerBase.cs` |
| `PaymentPurpose` in Shared is exactly `Onboarding = 1, Renewal = 2, AdminAssigned = 3` | `src/TenantCore.Shared/Enums/PaymentPurpose.cs` |
| `SubscriptionPayment` has **non-nullable** `SubscriptionPlanId`, `PlanCode`, `PlanName` today, and `IsClinicLink => Purpose is Renewal or AdminAssigned` | `src/TenantCore.Domain/Entities/SubscriptionPayment.cs` |
| `ClinicAccount` has no billing fields at all yet; it exposes `IsSuspended` and three mutators only | `src/TenantCore.Domain/Entities/ClinicAccount.cs` |
| `CreateOpdRegistrationHandler` already injects `ISender` and dispatches `EnsureOpdPaymentCommand` *after* its first `SaveChangesAsync`; `OpdRegistration.Create` assigns `Id` before the insert, so `registration.Id` is usable **before** `SaveChangesAsync` | `src/TenantCore.Application/Features/OpdRegistrations/Handlers/CreateOpdRegistrationHandler.cs` |
| There is **no** `IAdminActionService` in the Admin repo; the pattern is `AdminActionExecutor` + `IClinicActionService` (Phase 1) + `IAdminActionLogger` | `TenantCore.Admin/.claude/context/current-state.md` |

---

## Domain — new entities

### Entity: ClinicWallet (new)

The clinic's prepaid balance. **Tenant-scoped**, at most one row per clinic. Created lazily the first time a pay-as-you-go clinic is charged or the first time an admin touches the wallet — a `Subscription`-mode clinic with no usage never gets a row.

**Table:** `clinic.ClinicWallets`
**Base class:** `AuditableEntity`

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required, **unique** index — at most one wallet per clinic |
| Balance | decimal | (18,2), required, `>= 0`, default 0 |
| LowBalanceThreshold | decimal | (18,2), default `UsageBilling:DefaultLowBalanceThreshold` (100.00) |
| Currency | string | required, max 3, default `INR` |
| LastTopUpAt | DateTime? | UTC |
| LastDebitAt | DateTime? | UTC |
| RowVersion | byte[] | concurrency token — **the only thing standing between two concurrent charges and a negative balance** |

Domain methods and rules:
- `Create(applicationId, lowBalanceThreshold, currency)`
- `IsLow => Balance <= LowBalanceThreshold` — drives the client's header chip and the Admin low-balance list. **Never blocks anything.**
- `HasFunds(decimal amount) => Balance >= amount`
- `Credit(decimal amount, DateTime utcNow)` — throws `DomainException` if `amount <= 0`; sets `LastTopUpAt`.
- `Debit(decimal amount, DateTime utcNow)` — throws `DomainException("Invalid wallet amount.")` if `amount <= 0` and `DomainException("Insufficient wallet balance.")` if `!HasFunds(amount)`; sets `LastDebitAt`. **The handler converts that second throw into 403 `insufficient_wallet_balance`** (decision 12), so the domain stays HTTP-free.
- No `SetBalance`. Balance only moves through `Credit`/`Debit`, each of which is paired with exactly one `ClinicWalletTransaction` staged in the same unit of work.

### Entity: ClinicWalletTransaction (new)

The append-only wallet ledger. **Tenant-scoped**. One row per balance movement, written in the same `SaveChanges` as the movement itself. It answers "where did this balance come from and where did it go" without replaying anything else.

**Table:** `clinic.ClinicWalletTransactions`
**Base class:** `AuditableEntity`

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required, indexed |
| Type | WalletTransactionType | `Credit = 1`, `Debit = 2` |
| Amount | decimal | (18,2), required, **always positive** — `Type` carries the sign |
| BalanceAfter | decimal | (18,2) — the wallet balance immediately after this row, so the ledger reads top-to-bottom |
| ReferenceType | WalletReferenceType | `TopUpPayment = 1`, `Usage = 2`, `AdminCredit = 3`, `Adjustment = 4` |
| ReferenceId | Guid? | top-up → the paid `SubscriptionPayment.Id`; usage → the `ClinicUsageRecord.Id`; admin credit → the **admin-supplied idempotency key**; adjustment → free-form |
| Description | string | max 250, **system-generated only** — never a patient name, never free text from a user |
| OccurredAt | DateTime | UTC |
| CreatedByUserId | Guid? | the acting clinic user, when there is one |
| CreatedByAdminEmail | string? | max 256, set only for `AdminCredit` |

Indexes:
- **Unique** `(ApplicationId, ReferenceType, ReferenceId)` **filtered to `ReferenceId IS NOT NULL`** — the idempotency guarantee for top-up confirmations, usage charges and admin credits.
- Non-unique `(ApplicationId, OccurredAt DESC)` — the wallet ledger page.

Rules: no `Update`/`Delete` API is exposed anywhere; a correction is a **new** `Adjustment` row. `BalanceAfter` is computed inside the same transaction from the wallet row this transaction just wrote.

### Entity: ClinicUsageRate (new)

One admin-set price per billable operation per clinic. **Tenant-scoped**.

**Table:** `clinic.ClinicUsageRates`
**Base class:** `AuditableEntity`

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required, indexed |
| BillingItem | BillingItem | `OpdRegistration = 1`, `IpdAdmission = 2`, `IpdDischarge = 3` |
| Rate | decimal | (18,4), required, `>= 0` |
| IsActive | bool | default true |
| EffectiveFrom | DateTime? | UTC; informational — the rate in force is the newest active row |
| UpdatedByAdminEmail | string? | max 256 |
| RowVersion | byte[] | |

Index:
- **Unique filtered** `(ApplicationId, BillingItem)` **where `IsActive = 1`** — at most one live rate per item per clinic. Deactivating an old rate and inserting a new one is allowed; the pair can never both be live.

Rules:
- **No row means rate 0 means free.** An admin who has not priced an item cannot accidentally block a clinic — the operation goes through, is recorded with `UnitRate = 0`, `IsFree = true`, and shows up as unpriced on the Admin billing dashboard so the omission is visible.
- Setting a rate is an upsert: the handler deactivates the current live row and inserts the new one in the same `SaveChanges`, preserving the rate history the Admin audit shows.
- Phase 3 adds `BillingItem.OtSurgery = 4` as one enum value and one row per clinic. **No schema change.**

### Entity: ClinicUsageRecord (new)

The metered-usage ledger. **Tenant-scoped**, append-only, one row per billable operation **for every clinic regardless of billing mode**.

**Table:** `clinic.ClinicUsageRecords`
**Base class:** `AuditableEntity`

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required, indexed |
| BillingItem | BillingItem | which operation |
| SourceEntityId | Guid | the `OpdRegistration.Id` / `IpdRegistration.Id` that caused it |
| Quantity | int | default 1 (kept so a future item can be per-day or per-session without a migration) |
| UnitRate | decimal | (18,4) — **price snapshot**; the live rate at charge time, 0 when unpriced |
| Amount | decimal | (18,2) — `UnitRate × Quantity` **when charged**, otherwise 0 |
| PayerMode | BillingMode | **which mode applied** — `Subscription` (charged 0) or `PayAsYouGo` |
| IsFree | bool | true when nothing was charged (`PayerMode == Subscription`, or `UnitRate == 0`) |
| WalletTransactionId | Guid? | the paired debit row; null whenever `IsFree` |
| ChargedAt | DateTime | UTC — the operation's timestamp, not the row's insert time |
| CreatedByUserId | Guid? | the acting clinic user |

Indexes:
- **Unique** `(ApplicationId, BillingItem, SourceEntityId)` — the idempotency guarantee. A retried request, a duplicated workflow task or a replayed call can never produce a second charge for the same operation. **This is what makes the whole design safe to retry.**
- Non-unique `(ApplicationId, ChargedAt)` and `(ApplicationId, BillingItem, ChargedAt)` — the usage dashboard's range queries and per-item grouping.

Rules:
- The row is written **even when nothing was charged**, so a `Subscription` clinic's usage is answerable today and convertible into pay-as-you-go pricing tomorrow (`notional = UnitRate × Quantity`).
- `Amount` is what the clinic actually paid. The dashboard computes notional value from `UnitRate`; it never confuses the two.
- A clinic switching to pay-as-you-go mid-term simply starts producing non-free rows from the moment `EffectiveBillingMode` flips.

---

## Domain — entity changes

| Entity | Change |
|--------|--------|
| `ClinicAccount` | **+11 columns.** `BillingMode` (enum, default `Subscription`), `PendingBillingMode` (`BillingMode?`), `PendingBillingModeEffectiveFrom` (`DateTime?`), `RegistrationFee` (`decimal(18,2)?`, the per-clinic override; null = use the plan default), `RegistrationFeeChargedAt` (`DateTime?`), `RegistrationFeeChargedBy` (`string?` 256 — an admin email for a waive/adjustment, or `payment:<paymentId>` when a Razorpay link settled it), `RegistrationFeeWaived` (`bool`, default false), `RegistrationFeeWaiveReason` (`string?` 500), `RegistrationFeeWaivedByAdminEmail` (`string?` 256), `FreeTestDays` (`int?`, 30–90 when set), `TrialStartedAt` (`DateTime?`). New members: `EffectiveBillingMode(utcNow)`, `SetBillingMode(mode, utcNow, applyNow)`, `ApplyPendingBillingMode(utcNow)`, `HasRegistrationFeeDue`, `SetRegistrationFee(amount, adminEmail)`, `WaiveRegistrationFee(reason, adminEmail)`, `MarkRegistrationFeeCharged(chargedBy, utcNow)`, `SetFreeTestDays(days)`, `StartTrial(utcNow)`, `HasUsedTrial => TrialStartedAt is not null` |
| `SubscriptionPlan` | **+2 columns:** `RegistrationFee` (`decimal(18,2)`, default 0) and `FreeTestDays` (`int?`, 30–90 when set — the **default** for a clinic that has no per-clinic override). `CreateCustom`/`UpdateDetails` gain both parameters. The Trial plan keeps price 0 and gains `FreeTestDays` as its default trial length |
| `SubscriptionPayment` | `SubscriptionPlanId` becomes `Guid?`, `PlanCode` becomes `SubscriptionPlanCode?`, `PlanName` becomes `string?`. New factory `CreateForWalletTopUp(applicationId, amount, currency, payerName, payerEmail, payerPhone, clinicName, adminEmail?)` (Purpose `WalletTopUp`, no plan), and `CreateForRegistrationFee(applicationId, amount, currency, payerName, payerEmail, payerPhone, clinicName)` (Purpose `RegistrationFee`). `IsClinicLink` gains `WalletTopUp` and `RegistrationFee` so the existing "one open clinic link at a time / supersede" rules apply to them **unchanged**. New `IsPlanPayment => Purpose is Onboarding or Renewal or AdminAssigned` |

**Every existing reader of the three nullable plan columns gets a null-guard** — the App's timeline DTOs, the Admin payments page (Phase 2 of that repo), and the Razorpay payment-link task (a wallet top-up's Razorpay description must not read `PlanName`). The compile errors from making them nullable are the checklist: fix each, do not paper over them with `!`.

## Domain — enums (Shared, so Admin can mirror them)

| Enum | Values | Contract note |
|------|--------|---------------|
| `BillingMode` (new) | `Subscription = 1`, `PayAsYouGo = 2` | Integer values are a contract with `TenantCore.Admin` |
| `BillingItem` (new) | `OpdRegistration = 1`, `IpdAdmission = 2`, `IpdDischarge = 3` | Phase 3 appends `OtSurgery = 4`; **never renumber** |
| `WalletTransactionType` (new) | `Credit = 1`, `Debit = 2` | |
| `WalletReferenceType` (new) | `TopUpPayment = 1`, `Usage = 2`, `AdminCredit = 3`, `Adjustment = 4` | |
| `PaymentPurpose` (extend) | `+ WalletTopUp = 4`, `+ RegistrationFee = 5` | Existing values 1–3 unchanged |

**Error codes** — the existing `SubscriptionErrorCodes` gains nothing; new codes go in a **new** `UsageBillingErrorCodes` static class, keeping the "Billing" word out of the platform-billing namespace:

| Constant | Value | Surfaced as |
|---|---|---|
| `InsufficientWalletBalance` | `insufficient_wallet_balance` | 403 from `ChargeUsageCommand` (the client opens the top-up panel) |
| `WalletBalanceLow` | `wallet_balance_low` | carried in the summary DTO as a warning flag, not an error |
| `BillingModeLocked` | `billing_mode_locked` | 409 — a mode change is already queued |
| `TopUpLinkOpen` | `topup_link_open` | 409 — a top-up link is already open; the existing link is returned instead |

`SubscriptionErrorCodes` keeps exactly its four existing constants. **No pass-through re-export of the new codes is added there** — one code lives in exactly one class, so a future rename cannot leave two spellings of the same string in the codebase.

---

## Guard change — `SubscriptionGuardMiddleware` (quote the current file before editing)

Current behaviour, verbatim from `src/TenantCore.Api/Middleware/SubscriptionGuardMiddleware.cs`:

```csharp
    private static readonly string[] ExemptPathPrefixes =
    [
        "/api/subscriptions",
        "/api/subscription-alert-settings",
        "/api/auth",
        "/api/clinic/dashboard",
        "/health",
        "/api/onboarding",
        "/api/internal",
        "/api/payments/razorpay"
    ];
```

and

```csharp
        var subscriptionRepository = context.RequestServices.GetRequiredService<IClinicSubscriptionRepository>();
        var active = await subscriptionRepository.GetActiveForClinicAsync(applicationId, context.RequestAborted);

        if (active is not null)
        {
            await next(context);
            return;
        }
```

**Two edits:**

1. Add `"/api/usage-billing"` to `ExemptPathPrefixes`. It goes **after** `"/api/subscriptions"` so the two platform-billing surfaces read together, with the existing comment style explaining why (a locked or pay-as-you-go clinic must reach the screen that fixes its state).
2. Insert the pay-as-you-go branch immediately **before** the `GetActiveForClinicAsync` call shown above.

**The new guard block, in full:**

```csharp
        // A pay-as-you-go clinic has no subscription and never will — it is metered instead.
        // It must pass the guard, or it could not reach the wallet screen that tops it up.
        // BillingMode comes from the same single no-tracking read used for the mode shown in the UI.
        if (await accountRepository.GetBillingModeAsync(applicationId, context.RequestAborted) == BillingMode.PayAsYouGo)
        {
            await next(context);
            return;
        }
```

**Rules this edit must respect:**
- The **suspension check stays first and unconditional**, exactly as it is today. A suspended pay-as-you-go clinic is still blocked with 403 `clinic_suspended`.
- The new branch goes **after** the `Subscription:GuardEnabled` check, so a developer working with the guard off is unaffected in the other direction too.
- **No pipeline reorder.** This is an addition inside an existing middleware, per ADR-005.
- `GetBillingModeAsync` must be **no-tracking, single-row, indexed** — it runs on every clinic-scoped request for a pay-as-you-go clinic. Where a clinic's `ClinicAccount` row is missing, it returns `BillingMode.Subscription`, preserving today's behaviour for every existing clinic.
- The **effective** mode is what matters, so `GetBillingModeAsync` must resolve `EffectiveBillingMode(utcNow)` server-side (i.e. the repository query projects `PendingBillingMode`/`PendingBillingModeEffectiveFrom` and the mapping applies the same rule as the entity method). The entity method stays the single definition of that rule; the repository must not re-implement it differently.

## Error-code plumbing — `ErrorCodeException` (decision 12)

New file `src/TenantCore.Application/Common/Exceptions/ErrorCodeException.cs`:

```csharp
namespace TenantCore.Application.Common.Exceptions;

/// <summary>
/// A business refusal that carries a stable machine-readable code. The API's
/// ExceptionHandlingMiddleware turns StatusCode into the HTTP status and
/// ErrorCode into problemDetails.Extensions["errorCode"], which the Blazor
/// client keys UI behaviour off (ApiResponseReader).
/// </summary>
public sealed class ErrorCodeException(int statusCode, string errorCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;
}
```

`ExceptionHandlingMiddleware` gains exactly two changes:
1. A `switch` arm placed **before** the generic `DomainException` arm: `ErrorCodeException ece => ((HttpStatusCode)ece.StatusCode, "Request Refused", ece.Message)`.
2. After the existing `errors` block, an additive:

```csharp
        if (exception is ErrorCodeException errorCodeException)
            problemDetails.Extensions["errorCode"] = errorCodeException.ErrorCode;
```

Existing status mappings and the `correlationId` extension are untouched, and the existing Phase-1 guard codes keep being written by their own middlewares.

---

## Persistence, configuration & migrations

**`ClinicDbContext`: 46 DbSets → 50.** Append in this order after `PaymentWebhookEvents`:

```csharp
    public DbSet<ClinicWallet> ClinicWallets => Set<ClinicWallet>();
    public DbSet<ClinicWalletTransaction> ClinicWalletTransactions => Set<ClinicWalletTransaction>();
    public DbSet<ClinicUsageRate> ClinicUsageRates => Set<ClinicUsageRate>();
    public DbSet<ClinicUsageRecord> ClinicUsageRecords => Set<ClinicUsageRecord>();
```

**Fluent API only — no data annotations** (ADR-004). Four new configuration files under `Persistence/Configurations/Clinic/`, plus edits to the existing `ClinicAccountConfiguration`, `SubscriptionPlanConfiguration` and `SubscriptionPaymentConfiguration`:

| Configuration | What it declares |
|---|---|
| `ClinicWalletConfiguration` | Table `clinic.ClinicWallets`; unique index on `ApplicationId`; `Balance`/`LowBalanceThreshold` `HasPrecision(18, 2)`; `Currency` max 3; `RowVersion` `IsRowVersion()` |
| `ClinicWalletTransactionConfiguration` | Table `clinic.ClinicWalletTransactions`; enum columns as `int`; `Amount`/`BalanceAfter` `HasPrecision(18, 2)`; **`HasIndex(x => new { x.ApplicationId, x.ReferenceType, x.ReferenceId }).IsUnique().HasFilter("[ReferenceId] IS NOT NULL")`**; `HasIndex(x => new { x.ApplicationId, x.OccurredAt }).IsDescending(false, true)` |
| `ClinicUsageRateConfiguration` | Table `clinic.ClinicUsageRates`; `Rate` `HasPrecision(18, 4)`; unique filtered index on `(ApplicationId, BillingItem)` `.HasFilter("[IsActive] = 1")`; `BillingItem` enum as `int`; `RowVersion` |
| `ClinicUsageRecordConfiguration` | Table `clinic.ClinicUsageRecords`; `UnitRate` `HasPrecision(18, 4)`, `Amount` `HasPrecision(18, 2)`; **unique** index on `(ApplicationId, BillingItem, SourceEntityId)`; the two non-unique query indexes; `PayerMode` enum as `int` |
| `ClinicAccountConfiguration` (edit) | New enum columns as `int`; `RegistrationFee` `HasPrecision(18, 2)`; max lengths 500/256 |
| `SubscriptionPlanConfiguration` (edit) | `RegistrationFee` `HasPrecision(18, 2)`, default 0; `FreeTestDays` nullable int |
| `SubscriptionPaymentConfiguration` (edit) | Drop `IsRequired()` on `SubscriptionPlanId`, `PlanCode`, `PlanName` |

**`UsageBillingOptions`** (new, `src/TenantCore.Infrastructure/Configuration/UsageBillingOptions.cs`), bound to the `UsageBilling` config section and registered in `TenantCore.Infrastructure/DependencyInjection.cs`:

| Key | Default | Used for |
|---|---|---|
| `UsageBilling:Enabled` | `true` | Master switch for metering. **Off ⇒ every usage row is written free** (`IsFree = true`, `Amount = 0`), never "no row" — analytics keep working while a rollout is paused |
| `UsageBilling:Currency` | `INR` | Wallet currency on creation |
| `UsageBilling:DefaultLowBalanceThreshold` | `100.00` | `ClinicWallet.LowBalanceThreshold` when a wallet is first created |
| `UsageBilling:TopUpMinAmount` / `TopUpMaxAmount` | `100` / `100000` | Validation on clinic and admin top-ups |
| `UsageBilling:PaymentCheckThrottleSeconds` | `30` | The "I've paid — check now" throttle, same value and behaviour as Phase 1's subscription check |

**Two migrations, in this order — the first is reviewed in isolation before the second is generated.**

```
dotnet ef migrations add MakeSubscriptionPaymentPlanOptional --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations

dotnet ef migrations add AddUsageBillingAndWallet --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

- **`MakeSubscriptionPaymentPlanOptional`** touches three columns on an existing table with rows. `AlterColumn(..., nullable: true)` is a metadata-only change for `Guid`/`int`/`nvarchar` — **verify the generated `Up` contains only `AlterColumn` calls and no table rebuild**, and review the `Down` before it is ever run.
- **`AddUsageBillingAndWallet`** creates the four tables, adds the 11 `ClinicAccounts` columns and the 2 `SubscriptionPlans` columns, and creates the five indexes. **No data seeding and no raw SQL.** `BillingMode` defaults to `1` (`Subscription`) at the column level so every existing row is correct without a backfill.
- Print both commands and **do not auto-run them** (the house rule for this repo — Phase 1 followed the same rule).

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Enums/BillingMode.cs` | `Subscription = 1`, `PayAsYouGo = 2` (contract comment pointing at the Admin mirror) |
| `Enums/BillingItem.cs` | `OpdRegistration = 1`, `IpdAdmission = 2`, `IpdDischarge = 3` |
| `Enums/WalletTransactionType.cs` | `Credit = 1`, `Debit = 2` |
| `Enums/WalletReferenceType.cs` | `TopUpPayment = 1`, `Usage = 2`, `AdminCredit = 3`, `Adjustment = 4` |
| `Errors/UsageBillingErrorCodes.cs` | The four constants in the table above |
| `Dtos/UsageBilling/ClinicBillingSummaryDto.cs` | `BillingMode`, `EffectiveBillingMode`, `PendingBillingMode?`, `PendingBillingModeEffectiveFrom?`, `CoverageEnd?`, `Currency`, `WalletBalance?`, `WalletIsLow`, `LowBalanceThreshold?`, `Rates` (`IReadOnlyList<UsageRateDto>`), `RegistrationFeeDue`, `RegistrationFee` (resolved: clinic override else plan default), `RegistrationFeeWaived`, `RegistrationFeeChargedAt?`, `FreeTestDays?`, `TrialStartedAt?` |
| `Dtos/UsageBilling/UsageRateDto.cs` | `BillingItem`, `Rate`, `IsPriced` |
| `Dtos/UsageBilling/ClinicUsageDto.cs` | `From`, `To`, `TotalCharged`, `TotalNotional`, `IReadOnlyList<UsageItemTotalsDto>` (per item: count, charged, notional), `IReadOnlyList<UsageSeriesPointDto>` (daily), `IReadOnlyList<UsageRecordDto>` (capped) |
| `Dtos/UsageBilling/ClinicWalletDto.cs` | `Balance`, `LowBalanceThreshold`, `IsLow`, `Currency`, `LastTopUpAt?`, `Transactions` (`IReadOnlyList<WalletTransactionDto>`) |
| `Dtos/UsageBilling/WalletTransactionDto.cs` | `OccurredAt`, `Type`, `Amount`, `BalanceAfter`, `ReferenceType`, `Description` |
| `Dtos/UsageBilling/CreateWalletTopUpRequest.cs` | `Amount` |
| `Dtos/UsageBilling/WalletTopUpPaymentDto.cs` | `PaymentId`, `Amount`, `PaymentLinkUrl?`, `Status` |
| `Dtos/UsageBilling/RegistrationFeePaymentDto.cs` | Same shape, for the fee link |
| `Dtos/PlatformAdmin/SetClinicBillingRequest.cs` | `BillingMode`, `ApplyNow`, `RegistrationFee?`, `FreeTestDays?`, `Reason` + admin fields |
| `Dtos/PlatformAdmin/SetClinicUsageRatesRequest.cs` | `IReadOnlyList<UsageRateInput>` (`BillingItem`, `Rate`), `Reason` + admin fields |
| `Dtos/PlatformAdmin/ClinicWalletTopUpRequest.cs` | `Amount`, `Reason`, `ReferenceId` + admin fields |
| `Dtos/PlatformAdmin/ClinicWalletCreditRequest.cs` | `Amount`, `Reason`, **`ReferenceId` (required — the idempotency key)** + admin fields |
| `Dtos/PlatformAdmin/WaiveRegistrationFeeRequest.cs` | `Reason` + admin fields |

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Entities/ClinicWallet.cs` | Entity above |
| `Entities/ClinicWalletTransaction.cs` | Entity above |
| `Entities/ClinicUsageRate.cs` | Entity above |
| `Entities/ClinicUsageRecord.cs` | Entity above |
| `Interfaces/IClinicWalletRepository.cs` | `GetByApplicationIdAsync` (**tracked** — the debit path needs the change tracker), `GetByApplicationIdNoTrackingAsync`, `AddAsync` |
| `Interfaces/IClinicWalletTransactionRepository.cs` | `AddAsync`, `GetForClinicAsync(applicationId, limit)`, `ExistsAsync(applicationId, referenceType, referenceId)` |
| `Interfaces/IClinicUsageRateRepository.cs` | `GetActiveForClinicAsync`, `GetActiveAsync(applicationId, billingItem)`, `AddAsync`, `Update` |
| `Interfaces/IClinicUsageRecordRepository.cs` | `ExistsAsync(applicationId, billingItem, sourceEntityId)`, `AddAsync`, `GetInRangeAsync(applicationId, from, to)`, `GetTotalsAsync(applicationId, from, to)` |

`IClinicAccountRepository` (existing) gains `GetBillingModeAsync(applicationId, ct)` and `GetOrCreateAsync(applicationId, ct)` (the second is already needed by Phase 1's commands; reuse it rather than adding a second creator).

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Configuration/UsageBillingOptions.cs` | Options class bound to the `UsageBilling` section |
| `Persistence/Configurations/Clinic/ClinicWalletConfiguration.cs` | Fluent configuration above |
| `Persistence/Configurations/Clinic/ClinicWalletTransactionConfiguration.cs` | Fluent configuration above |
| `Persistence/Configurations/Clinic/ClinicUsageRateConfiguration.cs` | Fluent configuration above |
| `Persistence/Configurations/Clinic/ClinicUsageRecordConfiguration.cs` | Fluent configuration above |
| `Repositories/ClinicWalletRepository.cs` | Extends `ClinicRepository<ClinicWallet>` |
| `Repositories/ClinicWalletTransactionRepository.cs` | Extends `ClinicRepository<ClinicWalletTransaction>` |
| `Repositories/ClinicUsageRateRepository.cs` | Extends `ClinicRepository<ClinicUsageRate>` |
| `Repositories/ClinicUsageRecordRepository.cs` | Extends `ClinicRepository<ClinicUsageRecord>` |
| `Persistence/ClinicMigrations/<ts>_MakeSubscriptionPaymentPlanOptional.cs` | Generated (columns only) |
| `Persistence/ClinicMigrations/<ts>_AddUsageBillingAndWallet.cs` | Generated (4 tables, 13 columns, 5 indexes) |

DI in `TenantCore.Infrastructure/DependencyInjection.cs`: the four repositories (scoped) and `services.Configure<UsageBillingOptions>(configuration.GetSection("UsageBilling"))`.

### Application Layer — new area `src/TenantCore.Application/Features/UsageBilling/`

| File | Purpose |
|------|---------|
| `Commands/ChargeUsageCommand.cs` | `sealed record ChargeUsageCommand(Guid ApplicationId, BillingItem BillingItem, Guid SourceEntityId, int Quantity, Guid? UserId, DateTime? OccurredAt) : IRequest<UsageChargeDto>`, **also `IBusinessAction` (`"Usage Charged"`) and `IActionLogContext`** — see the note below |
| `Handlers/ChargeUsageCommandHandler.cs` | The metering engine, below |
| `Commands/CreateWalletTopUpCommand.cs` + handler + validator | Create a Razorpay top-up link |
| `Commands/CheckWalletTopUpPaymentCommand.cs` + handler + validator | "I've paid — check now", 30-second throttle |
| `Commands/CreateRegistrationFeeLinkCommand.cs` + handler + validator | Create the fee's Razorpay link |
| `Commands/CheckRegistrationFeePaymentCommand.cs` + handler | "I've paid — check now" for the fee |
| `Queries/GetClinicBillingSummaryQuery.cs` + handler | The summary DTO |
| `Queries/GetClinicUsageQuery.cs` + handler | Range usage, capped |
| `Queries/GetClinicWalletQuery.cs` + handler | Balance + ledger |
| `Translators/UsageBillingTranslator.cs` | **Static** (never AutoMapper) — entity → DTO for the three queries |
| `Services/IUsageBillingService.cs` + `UsageBillingService.cs` | Rate resolution, `EffectiveBillingMode`, "resolved registration fee", low-balance flag. Injected by the command handlers so the rules have one home |
| `Services/ITrialTermRules.cs` + `TrialTermRules.cs` | `ResolveTrialDurationDays(int? clinicFreeTestDays, int planDurationDays)` — the one place free test days are applied |

**Why `ChargeUsageCommand` is audited even though it is a sub-command.** `plan/action-audit-logging/PLAN.md` records that sub-commands such as `EnsureOpdPaymentCommand` stay silent *unless deliberately added*. This one is **deliberately added**: it moves money. It therefore implements `IBusinessAction` ("Usage Charged") and `IActionLogContext` (`applicationId; billingItem; amount; sourceEntityId; mode`), giving a durable billing trail that survives even if the parent command's own row is rotated away. The resulting two rows per OPD registration (parent + charge) are intended, not an oversight.

**`ChargeUsageCommandHandler` — the contract, in order:**

1. **Kill-switch:** if `UsageBillingOptions.Enabled` is false, write the record free (`IsFree = true`, `Amount = 0`) using the resolved rate and stop. The row is never skipped.
2. **Idempotency:** `IClinicUsageRecordRepository.ExistsAsync(applicationId, billingItem, sourceEntityId)` → if a row exists, return it and change nothing. The unique index is the backstop for a genuine race; a 2601/2627 `DbUpdateException` is caught and re-read rather than surfaced.
3. **Resolve the mode:** `IClinicAccountRepository.GetByApplicationIdAsync` → `EffectiveBillingMode(utcNow)`, defaulting to `Subscription` when no row exists (never create an account row just to charge).
4. **Resolve the rate:** `IClinicUsageRateRepository.GetActiveAsync(applicationId, billingItem)` → `Rate`, or `0m` when absent.
5. **Stage the record:** `ClinicUsageRecord.Create(...)` with `PayerMode = mode`, `UnitRate = rate`, `Amount = (mode == PayAsYouGo && rate > 0) ? Math.Round(rate * quantity, 2) : 0`, `IsFree = Amount == 0`. `AddAsync` — **no `SaveChanges`.**
6. **Debit, only when `mode == PayAsYouGo` and `Amount > 0`:**
   - load the wallet **tracked**; if absent, create it with `Balance = 0` and add it (a pay-as-you-go clinic's first charge therefore fails cleanly with the explicit top-up message rather than a null reference);
   - `wallet.Debit(amount, utcNow)`;
   - stage the `ClinicWalletTransaction` (`Debit`, `Amount`, `BalanceAfter = wallet.Balance`, `ReferenceType = Usage`, `ReferenceId = record.Id`);
   - set `record.WalletTransactionId`;
   - **no `SaveChanges` here either.**
7. **Failure mapping:** a `DomainException("Insufficient wallet balance.")` from `Debit` becomes `throw new ErrorCodeException(403, UsageBillingErrorCodes.InsufficientWalletBalance, "...")`. A `DbUpdateConcurrencyException` (a concurrent charge moved the balance) is caught around the **parent's** save, the wallet is re-read and the debit re-applied **once**, and only then is the same 403 returned. **One retry, then refuse — never a silent success and never a loop.**
8. **Return** `UsageChargeDto` (`Charged`, `Amount`, `BalanceAfter?`, `IsFree`) so a calling handler can log or react, though no caller branches on it today.

**Because the handler never calls `SaveChanges`, the whole thing is one transaction with the parent.** A refusal therefore commits **nothing** — no OPD row, no usage row, no partial debit — which is exactly what decisions 4 and 5 need together.

### Application Layer — additions to `src/TenantCore.Application/Features/PlatformAdmin/`

Five new internal commands, all following the Phase-1 shape exactly: `sealed record`, `IBusinessAction`, `IActionLogContext`, `AdminUserId` + `AdminEmail`, `ApplicationId` **from the route only**, reason 3–500 chars, and `GetOrCreateAsync` on the `ClinicAccount` where a row may not exist yet.

| Command (+ Validator + Handler entry) | Returns | Behaviour |
|---|---|---|
| `SetClinicBillingCommand` | — | `SetBillingMode(mode, utcNow, applyNow)`, `SetRegistrationFee(amount)` when provided, `SetFreeTestDays(days)` when provided. `applyNow = false` queues to coverage end (403-free 409 `billing_mode_locked` if a *different* mode is already queued and unresolved) |
| `SetClinicUsageRatesCommand` | — | Upserts each supplied `(BillingItem, Rate)`: deactivate the current live row, insert the new one, one `SaveChanges`. Items not supplied are left alone |
| `SendClinicWalletTopUpCommand` | `Guid` (payment id) | Validates against `TopUpMinAmount`/`TopUpMaxAmount`, 2 decimals. If a `WalletTopUp` link is already open, **returns that link's payment id with 409 `topup_link_open`** instead of creating a second one. Creates the payment + enqueues `CreatePaymentLink` in the same `SaveChanges` |
| `GrantClinicWalletCreditCommand` | `Guid` (transaction id) | `ReferenceId` is the idempotency key; if a transaction with that `(AdminCredit, ReferenceId)` exists, returns it and changes nothing. Otherwise wallet `Credit` + `ClinicWalletTransaction` in one `SaveChanges` |
| `WaiveClinicRegistrationFeeCommand` | — | `WaiveRegistrationFee(reason, adminEmail)`. 409 if already waived or already charged — a charged fee is a **manual refund**, never a silent waive |

Plus `Features/PlatformAdmin/Queries/` — **none.** The Admin portal reads the four new tables directly from its own read-only context, so this repo exposes **no internal GETs** (decision carried from Phase 1: the portal has the data, the App has the rules).

### API Layer (`src/TenantCore.Api/`)

| File | Purpose |
|------|---------|
| `Controllers/UsageBillingController.cs` | New clinic-facing controller, `[Route("api/usage-billing")]`, `ClinicControllerBase`. Every action uses `GetApplicationId()` and `GetCurrentUserId()`; nothing comes from the body |
| `Controllers/Internal/InternalClinicsController.cs` (edit) | The five new `api/internal/clinics/{id}/...` actions, `[Authorize(AuthenticationSchemes = "InternalService")]`, `{id}` bound to `applicationId` and passed into the command — **never read from the body** |
| `Middleware/SubscriptionGuardMiddleware.cs` (edit) | The two edits above |
| `Middleware/ExceptionHandlingMiddleware.cs` (edit) | The `ErrorCodeException` arm + the `errorCode` extension |

**Full endpoint surface (clinic-facing, `SubscriptionGuardMiddleware` exempt, `BillingFeatureGuardMiddleware` not applied):**

| Verb | Route | Returns | Policy |
|---|---|---|---|
| GET | `api/usage-billing/summary` | 200 `ClinicBillingSummaryDto` | RequireAuthenticated |
| GET | `api/usage-billing/usage?from&to` | 200 `ClinicUsageDto` (defaults to the last 30 days; range capped) | RequireAuthenticated |
| GET | `api/usage-billing/wallet` | 200 `ClinicWalletDto` (balance, threshold, last 50 ledger rows) | RequireAuthenticated |
| POST | `api/usage-billing/wallet/topups` | 201 `WalletTopUpPaymentDto` · 400 · 409 `topup_link_open` | RequireClinicAdmin |
| POST | `api/usage-billing/wallet/topups/{id:guid}/check` | 204 · 404 · 409 · 429 | RequireClinicAdmin |
| POST | `api/usage-billing/registration-fee/link` | 201 `RegistrationFeePaymentDto` · 400 · 409 (already charged or waived) | RequireClinicAdmin |
| POST | `api/usage-billing/registration-fee/link/{id:guid}/check` | 204 · 404 · 409 · 429 | RequireClinicAdmin |

**Internal endpoint surface (all `InternalService` scheme, all `IBusinessAction`, `ApplicationId` from the route):**

| Verb | Route | Body | Effect |
|---|---|---|---|
| POST | `api/internal/clinics/{id}/billing` | `SetClinicBillingRequest` | Billing mode (queued or immediate), registration fee override, free test days |
| PUT | `api/internal/clinics/{id}/usage-rates` | `SetClinicUsageRatesRequest` | Upsert 1–3 rates |
| POST | `api/internal/clinics/{id}/wallet/topup` | `ClinicWalletTopUpRequest` | Send the clinic a top-up payment link |
| POST | `api/internal/clinics/{id}/wallet/credit` | `ClinicWalletCreditRequest` | Grant wallet credit directly (idempotent on `ReferenceId`) |
| POST | `api/internal/clinics/{id}/registration-fee/waive` | `WaiveRegistrationFeeRequest` | Waive the fee with a reason |

---

## Files to Modify — the three metering hooks (read each file before editing)

| File | Change |
|------|--------|
| `Features/OpdRegistrations/Handlers/CreateOpdRegistrationHandler.cs` | Dispatch the charge **between** `await opdRepository.AddAsync(registration, ct)` (line 66) and `await opdRepository.SaveChangesAsync(ct)` (line 67): `await sender.Send(new ChargeUsageCommand(request.ApplicationId, BillingItem.OpdRegistration, registration.Id, 1, request.ReceivedByUserId), ct);`. `ISender` is already injected, so no constructor change. **`OpdRegistration.Create` assigns `Id` before the insert**, so `registration.Id` is valid here. The charge is deliberately **outside** the `if (billingEnabled)` block — platform billing is not the clinic's patient-billing module |
| `Features/IpdRegistrations/Handlers/CreateIpdRegistrationHandler.cs` | Same shape with `BillingItem.IpdAdmission` and the new `IpdRegistration.Id`. **Add `ISender` to the constructor** if it is not already there (verify first) |
| `Features/Ipd/Handlers/DischargePatientHandler.cs` | Same shape with `BillingItem.IpdDischarge` and the existing `IpdRegistration.Id`, inserted before that handler's own `SaveChangesAsync`. Add `ISender` if absent. A given `IpdRegistration.Id` therefore appears twice — once as `IpdAdmission`, once as `IpdDischarge` — which the unique index `(ApplicationId, BillingItem, SourceEntityId)` permits by design |

**Non-negotiable:** each dispatch must sit **before** the handler's `SaveChangesAsync` that persists the source row. Placing it after (as the existing `EnsureOpdPaymentCommand` call is) would save the registration first and then fail with a 403, leaving an operation that exists and was never billed — the exact inconsistency this design removes.

| Other file | Change |
|------|--------|
| `Features/Onboarding/Tasks/ConfirmPaymentTaskHandler.cs` | Branch on `Purpose`: `WalletTopUp` → enqueue `CreditWallet`; `RegistrationFee` → enqueue `RecordRegistrationFee`; `Renewal`/`AdminAssigned` → `ActivateSubscription` (unchanged); `Onboarding` → unchanged |
| `Features/Onboarding/Tasks/CancelPaymentLinkTaskHandler.cs` | `HandleAlreadyPaidAsync` must also cover the two new purposes, so a top-up link paid *as* it was cancelled still credits the wallet (Phase 1's D7 rule generalised) |
| `Features/Onboarding/Tasks/CreatePaymentLinkTaskHandler.cs` | Description/notes for a top-up: `"<Clinic> — wallet top-up ₹X"`. **Do not read `PlanName`** — it is null for these purposes. RecipientName falls back to the clinic name (workspace rule 9) |
| `Features/Subscriptions/Handlers/*` and `Translators/SubscriptionTranslator.cs` | Replace `payment.PlanName`/`PlanCode` reads with `payment.PlanName ?? "—"` style null-guards; add a `PaymentPurpose` → label map that covers `WalletTopUp` ("Wallet top-up") and `RegistrationFee` ("Registration fee") |
| `Features/PlatformAdmin/Commands/*` (Phase 1's `GrantClinicSubscriptionCommand`) | When the granted plan **is the Trial plan**, term duration = `TrialTermRules.ResolveTrialDurationDays(clinic.FreeTestDays, plan.DurationDays)` and `clinic.StartTrial(utcNow)` is called. Refuse with 409 if `clinic.HasUsedTrial`. Same two lines in `SubscribeToPlanHandler`'s trial path and the onboarding-approval trial path — **the only three places a trial term is created** |
| `Features/Onboarding/Tasks/ActivateSubscriptionTaskHandler.cs` | After the term activates, call `ClinicAccount.ApplyPendingBillingMode(utcNow)`; and if `clinic.HasRegistrationFeeDue` and the clinic is **not** waived and **not** charged, leave it due — activation never charges the fee (decision 10). Do **not** create a fee link from a background task: it needs a recipient and a clinic action |
| `Persistence/ClinicDbContext.cs` | +4 DbSets (46 → 50) |
| `DependencyInjection.cs` (Infrastructure) | 4 repositories + `UsageBillingOptions` |
| `DependencyInjection.cs` (Application) | `IUsageBillingService`, `ITrialTermRules` |
| `appsettings.json` / `appsettings.Development.json` | A `UsageBilling` section. **Development values are 3 lines with comments only** — never paste a connection string or a secret into a tracked file |

---

## Client UI (`src/TenantCore.Web.Client/`) — ADR-007, no component library, `opd-*` theme

### Files to create

| File | Purpose |
|------|---------|
| `Clients/IUsageBillingApiClient.cs` + `UsageBillingApiClient.cs` | Typed client for the seven clinic routes. Registered in `Program.cs` with the same base-address/handler pattern as `SubscriptionApiClient` |
| `Services/UsageBillingContextService.cs` | Caches `ClinicBillingSummaryDto` (mode, wallet balance, low flag, registration fee due, free test days) with `IsLoaded`, `IsPayAsYouGo`, `WalletIsLow`, `RegistrationFeeDue`, an `OnBillingChanged` event, `RefreshAsync()` and `Clear()`. **Mirrors `SubscriptionContextService` exactly** — same lifecycle, same call sites |
| `Components/UsageBilling/BillingModeBadge.razor` | "Package" / "Pay as you go" pill, plus "switches on <date>" when a mode change is queued |
| `Components/UsageBilling/WalletBalanceCard.razor` | Balance, threshold, low warning, **Top up** button, last-updated. Used on `/subscription` and `/subscription/wallet` |
| `Components/UsageBilling/RegistrationFeeCard.razor` | Amount, state (due / waived / charged at), **Pay now** and **I've paid — check now** |
| `Components/UsageBilling/UsageRatesTable.razor` | The three items with their live rate, "Not priced" where no row exists |
| `Components/UsageBilling/UsageTotalsTable.razor` | Per-item count, charged, notional, from `ClinicUsageDto` |
| `Components/UsageBilling/UsageTrendChart.razor` | Inline-SVG daily series with no chart library — the same approach as the Admin `Components/Charts/*` and the existing client charts |
| `Components/UsageBilling/WalletLedgerTable.razor` | Ledger rows with credit/debit styling and `BalanceAfter` |
| `Components/UsageBilling/TopUpDialog.razor` | Amount entry, presets, min/max hints, confirms then shows the payment link with copy + "I've paid — check now" |
| `Components/UsageBilling/InsufficientBalanceDialog.razor` | Shown when any API call returns 403 `insufficient_wallet_balance`: plain explanation + a link to `/subscription/wallet`. **Never a dead end** |
| `Pages/UsageBilling/Usage.razor` | `/subscription/usage` — range picker (client-side presets; **one backend call per range change**), totals, per-item table, trend chart, capped row list, **Print** and **Export CSV** |
| `Pages/UsageBilling/Wallet.razor` | `/subscription/wallet` — balance card, rates table, ledger, **Top up** |
| `Pages/UsageBilling/UsagePrint.razor` | `/subscription/usage/print` — ADR-007 print page copying `PrescriptionPrint.razor`'s layout/print CSS conventions. Opened in a new tab, `@layout` empty print layout |
| `Pages/UsageBilling/UsagePrintCsv.cs` (or a static helper) | Client-side CSV: RFC-4180 quoting **and** formula-injection escaping — any field starting with `=`, `+`, `-`, `@`, tab or CR is prefixed with an apostrophe. **No server-side generator, no new package reference** |

### Files to modify

| File | Change |
|------|--------|
| `Pages/Subscription/SubscriptionPlans.razor` | Becomes a tabbed page: **Plan** (current content), **Usage**, **Wallet**, **Billing**. Tabs are links, not in-page state, so each tab is bookmarkable and printable |
| `Layout/AuthorizedLayout.razor` | A wallet chip in the header next to the subscription pill: `Wallet ₹X` when pay-as-you-go, warning styling when `WalletIsLow`. Rendered only when `UsageBillingContext.IsPayAsYouGo` |
| `Layout/NavMenu.razor` | "Usage & Wallet" entry in the existing subscription/finance area, visible to every authenticated clinic user |
| `Pages/Subscription/SubscriptionPlans.razor` + the OPD/IPD create flows | After a 403 `insufficient_wallet_balance`, call `UsageBillingContextService.RefreshAsync()` and open `InsufficientBalanceDialog` |
| `Clients/ApiResponseReader.cs` | Already reads `errorCode` (Phase 1). Confirm it returns the problem detail for `insufficient_wallet_balance` rather than the generic permission text — extend the existing pattern, do not add a second parser |
| `Program.cs` | Register `IUsageBillingApiClient` and `UsageBillingContextService` (scoped) |
| `Services/SubscriptionContextService.cs` | Refresh alongside the billing context wherever the selected clinic changes (one call site today, in the clinic-context resolution) |

**Cost rule (workspace rule 7):** `/subscription/usage` performs **one** API call per explicit range change, caches the whole result in the page, and serves every tab, sort, search, item filter and print/export from memory. The usage query is capped server-side and the page shows a banner when the cap is hit — never a silent truncation.

---

## Validation Rules

| Field | Rules |
|-------|-------|
| `UsageRateInput.Rate` | `>= 0`, at most **4** decimals, `<= UsageBilling:MaxRate` (guard against a fat-finger ₹1,00,000 per OPD) |
| `BillingItem` | Must be a defined enum value; the three known items only in this phase |
| Top-up `Amount` | Between `UsageBilling:TopUpMinAmount` and `TopUpMaxAmount`, at most **2** decimals, `> 0` |
| Admin credit `Amount` | Same range and decimals as a top-up |
| Admin credit `ReferenceId` | `NotEmpty` — it is the idempotency key and a blank one would defeat the unique index |
| `RegistrationFee` (per-clinic and per-plan) | `>= 0`, at most 2 decimals |
| `FreeTestDays` | When supplied: **30–90 inclusive** |
| `SetClinicBillingRequest.BillingMode` | Must be a defined enum value; 409 `billing_mode_locked` when a different value is already queued |
| `Reason` (every internal command) | `NotEmpty`, 3–500 chars |
| `AdminUserId` / `AdminEmail` | NotEmpty; email format; max 256 — reuse the Phase-1 `AdminActorValidationExtensions` |
| `ApplicationId` | NotEmpty — bound from the route |
| `from` / `to` on the usage query | `from <= to`; `to >= from`; range ≤ 366 days; both default to the last 30 days when omitted |
| `CheckWalletTopUpPaymentCommand.PaymentId` | NotEmpty; the payment must belong to the caller's clinic (404 otherwise) |

## Business Rules

1. **Registration fee is charged once, ever, per clinic.** `HasRegistrationFeeDue` is false as soon as any of `RegistrationFeeWaived`, `RegistrationFeeChargedAt != null`, `RegistrationFee == 0` or the resolved fee is 0. Once charged, it can never be charged again — a second attempt is refused, not silently ignored.
2. **A charged fee is refunded manually.** Nothing in this plan moves money back. `WaiveClinicRegistrationFeeCommand` refuses an already-charged fee with 409 and the Admin dialog says so.
3. **Free test days are a duration override, applied once.** `ResolveTrialDurationDays(clinic.FreeTestDays, plan.DurationDays)` decides a trial term's length, and `clinic.StartTrial(utcNow)` is recorded the first time. A clinic with `TrialStartedAt` set can never receive a second free trial, whatever the admin does in the UI — the handler refuses with `trial_already_used`.
4. **Plan switching takes effect at coverage end**, never immediately: the new term's `StartDate` is the existing coverage end (Phase 1's timeline rules 3–4, unchanged). A switch while a link is open follows Phase 1's supersede rules, unchanged.
5. **A billing-mode change queues to coverage end** unless the admin explicitly applies it now. `EffectiveBillingMode(utcNow)` is the single definition of "which mode is live"; the guard, the metering command and the summary DTO all call it.
6. **A pay-as-you-go clinic is never blocked for want of a subscription.** It passes the guard, and only its three metered creates can fail — with 403 `insufficient_wallet_balance` and **nothing committed**.
7. **A subscription clinic is metered and never charged.** Its usage rows are informational (`IsFree = true`, `Amount = 0`).
8. **An unpriced item is free, not blocked.** A missing `ClinicUsageRate` row means rate 0, and the Admin dashboard flags the item as unpriced.
9. **The wallet can never go negative.** `ClinicWallet.Debit` refuses, and `RowVersion` makes the check-and-write atomic. There is no overdraft and no automatic negative-balance correction.
10. **Wallet credit is always paired with a ledger row.** `Balance` and `ClinicWalletTransaction.BalanceAfter` are written in the same `SaveChanges`; a credit without a ledger row is a bug, not an option.
11. **Top-up and fee links are ordinary clinic links.** At most one open per purpose per clinic, superseded by the same Phase-1 rules, and money that arrives on a cancelled or superseded link is still honoured (Phase 1 rule 9, extended to `WalletTopUp` and `RegistrationFee`).
12. **Suspension keeps working for every mode.** A suspended clinic is blocked with 403 `clinic_suspended` on every guarded route — pay-as-you-go included. It can still read `/api/usage-billing/*` so its admin can see what it owes, and it still cannot create anything.
13. **Cross-clinic ids are not-found (404).** A payment id, usage id or wallet id belonging to another clinic.
14. **Nothing about the clinic's own patient-billing module changes.** A clinic with `BillingEnabled = false` still pays the platform: `/api/usage-billing` is outside `BillingFeatureGuardMiddleware` by design (decision in the Naming section).

---

## Multi-Tenancy Checklist

- [x] `ApplicationId` on all four new entities; every one is tenant-scoped
- [x] `UsageBillingController` uses `GetApplicationId()` only — the header, resolved by `ClinicContextMiddleware`, never a body field
- [x] Every internal command receives `ApplicationId` **from the route** (`{id}`); the request DTO deliberately has **no** `ApplicationId` property
- [x] All four repositories extend `ClinicRepository<T>` (which filters by `applicationId`) and every query takes `applicationId` as its first argument
- [x] Lookups by id are always scoped: `GetByIdForClinicAsync(id, applicationId)` style — a wallet transaction, usage record, rate or payment belonging to another clinic is **404**, never 200
- [x] `ClinicUsageRate`, `ClinicUsageRecord`, `ClinicWallet`, `ClinicWalletTransaction` are **never** queried without a clinic filter — no "all clinics" query exists in this repo (the Admin portal does cross-clinic reads from its own read-only context)
- [x] `SubscriptionPlan` stays **global** (not tenant-scoped); its new `RegistrationFee`/`FreeTestDays` are catalogue defaults, and per-clinic overrides live on `ClinicAccount`

---

## Logging (ADR-011 — mandatory)

| Operation | How it is logged | Searchable context (identifiers and amounts only) |
|---|---|---|
| `ChargeUsageCommand` (every OPD/IPD create) | ActionLogs, `"Usage Charged"` | `applicationId; billingItem; amount; mode; sourceEntityId; walletTransactionId; free=true/false` |
| `CreateWalletTopUpCommand` / `CheckWalletTopUpPaymentCommand` | ActionLogs `"Wallet Top-Up Link Created"` / `"Wallet Top-Up Check"` | `applicationId; paymentId; amount` |
| `CreateRegistrationFeeLinkCommand` / `CheckRegistrationFeePaymentCommand` | ActionLogs `"Registration Fee Link Created"` / `"Registration Fee Check"` | `applicationId; paymentId; amount` |
| `CreditWalletTaskHandler` (payment confirmed) | Existing task logging + ActionLogs `"Wallet Credited"` | `applicationId; paymentId; amount; balanceAfter` |
| `RecordRegistrationFeeTaskHandler` (payment confirmed) | Existing task logging + ActionLogs `"Registration Fee Paid"` | `applicationId; paymentId; amount` |
| `SetClinicBillingCommand` | ActionLogs `"Clinic Billing Mode Set"` | `applicationId; mode; queued=true/false; freeTestDays; adminUserId` |
| `SetClinicUsageRatesCommand` | ActionLogs `"Clinic Usage Rates Set"` | `applicationId; items=<n>; adminUserId` — **per-item rates are on the entity, not in the log context** |
| `SendClinicWalletTopUpCommand` | ActionLogs `"Clinic Wallet Top-Up Link Sent"` | `applicationId; paymentId; amount; adminUserId` |
| `GrantClinicWalletCreditCommand` | ActionLogs `"Clinic Wallet Credit Granted"` | `applicationId; amount; balanceAfter; referenceId; adminUserId` |
| `WaiveClinicRegistrationFeeCommand` | ActionLogs `"Clinic Registration Fee Waived"` | `applicationId; amount; adminUserId` |
| 403 `insufficient_wallet_balance` | ActionLogs (`Failed`) + ApiRequestLogs (automatic: failed 403) | `applicationId; billingItem; amount; balance` |
| Guard: pay-as-you-go pass / suspended block | ApiRequestLogs only (pass-through is not a business action; the block is an automatic failed 403) | path + status |
| `GET api/usage-billing/*`, `api/internal/*` | ApiRequestLogs (automatic) + ApiErrorLogs on exceptions | — |

- [x] Every command that writes money or billing state is `IBusinessAction` with an `IActionLogContext`
- [x] No `ISkipActionLog` anywhere in this feature
- [x] **Nothing sensitive is logged.** No patient name, no MR number, no `OpdRegistration` clinical field, no payer email or phone, no Razorpay ids beyond the payment id, no admin free-text reason (reasons live on the entity and in the Admin audit). `SourceEntityId` is a GUID, not a patient reference
- [x] Tests assert the `ActionLogContext` string for `ChargeUsageCommand`, `GrantClinicWalletCreditCommand` and `SetClinicBillingCommand`
- [x] Every failure path is an exception or ≥ 400 — never 200 with an error body

---

## Contract with TenantCore.Admin (coordinate — the portal reads these directly)

| Item | Value the portal must mirror |
|------|------------------------------|
| New table `clinic.ClinicWallets` | all columns above |
| New table `clinic.ClinicWalletTransactions` | all columns above |
| New table `clinic.ClinicUsageRates` | all columns above |
| New table `clinic.ClinicUsageRecords` | all columns above |
| `clinic.ClinicAccounts` | + the 11 columns listed in Decisions 14 / the entity-change table |
| `clinic.SubscriptionPlans` | + `RegistrationFee`, `FreeTestDays` |
| `clinic.SubscriptionPayments` | `SubscriptionPlanId`, `PlanCode`, `PlanName` become **nullable** (mirror as nullable — a non-nullable read model will throw on a wallet top-up row) |
| Enums | `BillingMode` 1/2 · `BillingItem` 1/2/3 · `WalletTransactionType` 1/2 · `WalletReferenceType` 1/2/3/4 · `PaymentPurpose.WalletTopUp = 4`, `RegistrationFee = 5` |
| Internal endpoints | the 5 routes above; bodies as listed; admin fields `AdminUserId` + `AdminEmail`; **`ApplicationId` from the route only** |
| Error codes | `insufficient_wallet_balance`, `wallet_balance_low`, `billing_mode_locked`, `topup_link_open` |

No column is renamed or retyped; three columns only become nullable, and the portal's read models must be widened to match.

---

## New workflow task handlers (ride the existing durable-outbox pipeline)

| File | Purpose |
|------|---------|
| `Features/Onboarding/Tasks/CreditWalletTaskHandler.cs` | On a paid `WalletTopUp` payment: load or create the wallet, `Credit(amount)`, insert the `ClinicWalletTransaction` (`ReferenceType = TopUpPayment`, `ReferenceId = payment.Id`), call `payment.AttachClinic(applicationId)` if needed, one `SaveChanges`. **Idempotent twice over** — the `ExistsAsync` pre-check and the unique index |
| `Features/Onboarding/Tasks/RecordRegistrationFeeTaskHandler.cs` | On a paid `RegistrationFee` payment: `clinic.MarkRegistrationFeeCharged($"payment:{payment.Id}", utcNow)`. Refuses to double-charge when `RegistrationFeeChargedAt` is already set |

Add `WalletTopUp` and `RegistrationFee` to `WorkflowTaskType` and register both handlers in the same DI block as the seven existing ones.

## Implementation Order

1. **Shared:** 4 enums, `PaymentPurpose` +2, `UsageBillingErrorCodes`, all DTOs
2. **Domain:** 4 entities, 4 repository interfaces, `IClinicAccountRepository` additions, the `ClinicAccount`/`SubscriptionPlan`/`SubscriptionPayment` changes and their domain methods
3. **Application:** `ErrorCodeException` + the `ExceptionHandlingMiddleware` arm (do this **early** — every later handler depends on it)
4. **Infrastructure:** 4 configuration files + 3 configuration edits; 4 repositories; `UsageBillingOptions` + DI; `ClinicDbContext` +4 DbSets
5. **Application:** `IUsageBillingService`/`UsageBillingService`, `ITrialTermRules`/`TrialTermRules`; `Features/UsageBilling` commands/queries/handlers/validators/translator; DI
6. **Application:** `ChargeUsageCommand` + handler; then wire the three metering hooks; then run the OPD/IPD test suites — **they must stay green** before anything else proceeds
7. **Application:** the 5 PlatformAdmin commands + validators; the 2 new workflow task handlers; `ConfirmPaymentTaskHandler`/`CancelPaymentLinkTaskHandler`/`CreatePaymentLinkTaskHandler` branches; the 3 trial-creation sites; the null-guards for the nullable plan columns
8. **API:** `UsageBillingController`; `InternalClinicsController` additions; `SubscriptionGuardMiddleware` edit (re-run `SubscriptionGuardMiddlewareTests`)
9. **Web.Client:** API client + `UsageBillingContextService` + `Program.cs`; the components; the three pages + print + CSV; tabs, header chip, nav, dialogs
10. **Tests** (below) — `dotnet test` on the whole solution
11. **Migrations:** generate `MakeSubscriptionPaymentPlanOptional`, **review it in isolation**, then generate `AddUsageBillingAndWallet`. **Print both commands; run neither**
12. Update `.claude/context/current-state.md` (50 DbSets, new repositories, new services, new workflow task handlers) and `plan/REGISTRY.md`

## Test Files to Create / Update

| File | What it covers |
|------|---------------|
| `tests/TenantCore.Domain.Tests/Entities/ClinicWalletTests.cs` | `Credit`/`Debit` arithmetic; `Debit` refuses zero, negative and unfunded amounts; `IsLow` boundary (`==` threshold is low); no `SetBalance` exists |
| `…/Entities/ClinicAccountBillingTests.cs` | `EffectiveBillingMode` before/after `PendingBillingModeEffectiveFrom`; `SetBillingMode(applyNow: true)` clears the pending pair; `HasRegistrationFeeDue` for each of the four false cases; `WaiveRegistrationFee` refuses a charged fee; `MarkRegistrationFeeCharged` is once-only; `SetFreeTestDays` bounds; `StartTrial`/`HasUsedTrial` |
| `…/Entities/ClinicUsageRecordTests.cs` | `IsFree`/`Amount` derivation for the four combinations (mode × priced); `PayerMode` snapshot under a mode switch |
| `…/Entities/ClinicUsageRateTests.cs` | Upsert deactivates the previous live row; `Rate` cannot go negative |
| `tests/TenantCore.Application.Tests/Features/UsageBilling/Handlers/ChargeUsageCommandHandlerTests.cs` | **The most important test file in this plan.** Subscription mode → free row, no wallet, no transaction; pay-as-you-go with funds → row + debit + ledger + correct `BalanceAfter`; pay-as-you-go without funds → `ErrorCodeException` 403 `insufficient_wallet_balance` and **nothing staged**; unpriced item → free row; `Enabled = false` → free row; **duplicate `(item, sourceEntityId)` → one row only**; a wallet row created at zero when absent |
| `…/Handlers/CreateOpdRegistrationHandlerTests.cs` (extend existing) | The charge is dispatched **before** the save; a wallet refusal means **no `OpdRegistration` row is persisted**; a subscription clinic still creates the free usage row; existing `BillingEnabled = false` behaviour unchanged |
| `…/Handlers/CreateIpdRegistrationHandlerTests.cs`, `…/DischargePatientHandlerTests.cs` (extend existing) | The same three assertions for admission and discharge; one `IpdRegistration.Id` yields one `IpdAdmission` and one `IpdDischarge` row |
| `…/Features/UsageBilling/Handlers/WalletTopUpHandlerTests.cs` | Bounds/decimals; `topup_link_open` returns the existing payment id and creates nothing; `CreatePaymentLink` enqueued in the same save; throttle → 429; wrong clinic → 404 |
| `…/Features/UsageBilling/Handlers/RegistrationFeeLinkHandlerTests.cs` | Refuses when charged or waived; creates a link with `PlanName == null` and does not throw |
| `…/Features/Onboarding/Tasks/CreditWalletTaskHandlerTests.cs` | Credits once; a replay with the same `ReferenceId` is a no-op; `BalanceAfter` correct; the ledger row and the balance move in one save |
| `…/Features/Onboarding/Tasks/RecordRegistrationFeeTaskHandlerTests.cs` | Records once; a second run is a no-op; a waived fee is not charged |
| `…/Features/Onboarding/Tasks/ConfirmPaymentTaskHandlerTests.cs` (extend) | Purpose routing: `WalletTopUp` → `CreditWallet`; `RegistrationFee` → `RecordRegistrationFee`; `Renewal`/`AdminAssigned`/`Onboarding` unchanged |
| `…/Features/Onboarding/Tasks/CancelPaymentLinkTaskHandlerTests.cs` (extend) | A paid `WalletTopUp` on a cancelled link still enqueues the credit |
| `…/Features/PlatformAdmin/Commands/SetClinicBillingHandlerTests.cs` | Queues to coverage end; `applyNow` is immediate; `billing_mode_locked` for a conflicting queue; `FreeTestDays` bounds; the account is created when missing |
| `…/Features/PlatformAdmin/Commands/SetClinicUsageRatesHandlerTests.cs` | Upsert deactivates the old row; a partial item list leaves others alone; 4-decimal precision preserved |
| `…/Features/PlatformAdmin/Commands/WalletCreditHandlerTests.cs` | `ReferenceId` idempotency; balance + ledger in one save; amount bounds |
| `…/Features/PlatformAdmin/Commands/WaiveRegistrationFeeHandlerTests.cs` | Waives with reason + admin email; 409 when already charged or already waived |
| `…/Features/Subscriptions/Services/TrialTermRulesTests.cs` | The clinic override wins; 30/90 boundaries; a null override falls back to the plan; a clinic without an override keeps today's behaviour |
| `tests/TenantCore.Api.Tests/Middleware/SubscriptionGuardMiddlewareTests.cs` (extend) | Pay-as-you-go with **no** subscription → next called; pay-as-you-go **suspended** → 403 `clinic_suspended` (suspension still wins); the subscription path is unchanged; `/api/usage-billing/*` exempt |
| `tests/TenantCore.Api.Tests/Middleware/ExceptionHandlingMiddlewareTests.cs` (create or extend) | `ErrorCodeException` → its status + `problemDetails.Extensions["errorCode"]`; `DomainException` still 400 with no `errorCode`; `correlationId` still present |
| `tests/TenantCore.Api.Tests/Controllers/Internal/InternalClinicsControllerTests.cs` (extend) | The 5 new routes exist on the `InternalService` scheme; `{id}` from the route reaches the command; a body carrying an `ApplicationId` cannot override it (the property does not exist) |
| `tests/TenantCore.Api.Tests/Controllers/UsageBillingControllerTests.cs` | Thin-controller assertions: `GetApplicationId()` is what reaches the query, not a body value; policies per action |
| `tests/TenantCore.Infrastructure.Tests/…` (whichever project holds repository/model tests) | Each new index actually exists (assert against the EF model); the `(ApplicationId, ReferenceType, ReferenceId)` index is unique and filtered; the rate index is filtered on `IsActive` |

## Open Questions / Risks

- **Admin Phase 1 is a hard prerequisite.** The paired Admin plan cannot be executed until `clinic-subscription-admin-controls` is executed **in the Admin repo** (it is approved but not yet built). This App plan is not blocked by that, but the release is.
- **Write amplification on every metered create.** Each OPD/IPD create now writes up to 3 extra rows (usage + transaction + wallet update) inside a transaction that previously wrote 1. Negligible at current volumes; if a clinic ever reaches tens of thousands of registrations a day, revisit — a batched wallet update is the escape hatch, and it must not break the atomicity rule.
- **`ChargeUsageCommand` inside the parent's transaction couples clinical paths to the wallet.** A wallet-empty pay-as-you-go clinic genuinely cannot register an OPD patient. That is the intended behaviour (decision 5), but the client **must** surface a one-click route to the wallet screen from that error, or it becomes a support call.
- **Two action rows per metered create** while metering is on. Intended (money trail). If log volume becomes a problem, the fix is to let `ChargeUsageCommand` opt out and rely on the usage ledger — **not** to drop the ledger.
- **Concurrent charges on one wallet.** Two users registering two patients at the same instant is the expected case: the loser gets a `DbUpdateConcurrencyException`, is retried **once** against a fresh balance, and either succeeds or gets a clean 403. More retries would help in theory; the plan deliberately allows one to keep the failure path deterministic.
- **`SubscriptionPayment` nullable plan columns** touch onboarding, renewals, the Admin payments page and the Razorpay link task. Every reader must be visited; the compiler is the checklist. A surviving `PlanName!` is the likely defect — grep for it in review.
- **`FreeTestDays` semantics are narrow on purpose.** It overrides the **Trial** term's duration only. If the business intent was instead "any package grants N free days *after* payment", that is a different feature and must be re-planned before execution — flag it now, not mid-implementation.
- **No refunds, no autopay.** Refunds are manual (as in Phase 1). Auto-recharge when the balance drops below the threshold is deliberately **Phase 3** (`usage-billing-autopay`).
- **Dev has no Razorpay webhook secret** (Phase 1 background). Top-ups and the fee therefore depend on the "I've paid — check now" buttons and the 15-minute sweep locally, exactly like renewals.
- **OT/surgery is absent by design.** If the business needs it, it is one enum value, one rate row per clinic and one handler hook — a small follow-up, not a re-architecture.

## Deploy Order

1. **App migration** `MakeSubscriptionPaymentPlanOptional` → apply
2. **App migration** `AddUsageBillingAndWallet` → apply
3. **Deploy App** (API + Web.Client). Until this lands, every pay-as-you-go clinic is still blocked by the guard, so **this step is what turns the feature on**
4. **Set the `UsageBilling` config** in the target environment — start with `Enabled: false` so metering runs as free analytics while rates are entered
5. **Deploy TenantCore.Admin Phase 2** (its Phase 1 must already be deployed) so the internal endpoints have a caller
6. **Switch `UsageBilling:Enabled` to `true`** and set the three rates per clinic from the Admin portal

**Rollback:** set `UsageBilling:Enabled` to `false` — metering continues as free analytics and no clinic is blocked. **Do not** roll the migrations back while usage rows exist; leaving the tables in place loses nothing.

---

## Registry row (paste into `plan/REGISTRY.md` after execution — do not edit that file now)

```
| Registration Fee & Usage Billing (Phase 2) | ClinicWallet, ClinicWalletTransaction, ClinicUsageRate, ClinicUsageRecord | Executed | Package-wise extras + pay-as-you-go metering. `ClinicAccount` +11 columns (`BillingMode` Subscription=1/PayAsYouGo=2 with a queued `PendingBillingMode`, `RegistrationFee`/`ChargedAt`/`ChargedBy`, `RegistrationFeeWaived`/`WaiveReason`/`WaivedByAdminEmail`, `FreeTestDays` 30–90, `TrialStartedAt`); `SubscriptionPlan` +`RegistrationFee`/`FreeTestDays` defaults; `SubscriptionPayment.SubscriptionPlanId`/`PlanCode`/`PlanName` now nullable to carry `PaymentPurpose.WalletTopUp = 4` and `RegistrationFee = 5`. New `ChargeUsageCommand` (IBusinessAction "Usage Charged") is dispatched as an internal sub-command from `CreateOpdRegistrationHandler`, `CreateIpdRegistrationHandler` and `DischargePatientHandler` BEFORE each handler's SaveChanges, so the metered operation, its `ClinicUsageRecord` and its wallet debit commit in one transaction — a zero wallet refuses the create with 403 `insufficient_wallet_balance` and commits nothing, and never locks the rest of the app. Rating is row-based (`ClinicUsageRate`, unique per ApplicationId+BillingItem where IsActive) so Phase 3 adds OT as data. Subscription-mode clinics are metered free (`PayerMode=Subscription`, `IsFree=true`) for analytics. Idempotency: unique `(ApplicationId, BillingItem, SourceEntityId)` on usage and `(ApplicationId, ReferenceType, ReferenceId)` on wallet transactions; wallet debits are RowVersion-guarded, never `ExecuteUpdateAsync`. `SubscriptionGuardMiddleware` now passes a `PayAsYouGo` clinic (suspension still checked first and unconditionally) and exempts `/api/usage-billing`; that prefix is deliberately NOT added to `BillingFeatureGuardMiddleware`. New `ErrorCodeException` plus an `ExceptionHandlingMiddleware` `errorCode` extension (previously only the two guard middlewares emitted a code). 5 new `api/internal/clinics/{id}/*` actions (billing, usage-rates, wallet/topup, wallet/credit, registration-fee/waive) and 7 `api/usage-billing/*` clinic routes; 2 new workflow task handlers (`CreditWallet`, `RecordRegistrationFee`) on the existing durable-outbox pipeline. 46 → 50 DbSets; migrations `MakeSubscriptionPaymentPlanOptional` then `AddUsageBillingAndWallet` (commands printed, NOT run). Paired with `TenantCore.Admin/plan/registration-fee-and-usage-billing/PLAN.md` — deploy App + migration first. |
```

