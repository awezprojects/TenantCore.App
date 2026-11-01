# Feature Plan: Clinic Subscription Admin Controls (Phase 1)

**Repo:** TenantCore.App
**Date:** 2026-09-29
**Domain area:** Subscriptions & platform administration
**Status:** Approved — ready for execution
**Phase:** 1 of 3. Phase 2 `registration-fee-and-usage-billing` and Phase 3 `usage-billing-autopay` are separate, not-yet-written plans.
**Paired plan:** `TenantCore.Admin/plan/clinic-subscription-admin-controls/PLAN.md`. **Execute this App plan first**, because the Admin portal reads the new tables and calls the new internal endpoints.
**TenantCore.Auth:** not affected.
**Extends (registry):** "Clinic Subscription & Access Gating" and "Approval-Based Clinic Onboarding & Razorpay Subscriptions". Nothing is duplicated; both are extended.

---

## Overview

This phase gives the internal admin team full control over the subscriptions of clinics that already exist, and fixes how a paid upgrade is scheduled. From the TenantCore.Admin portal, an admin can:
- suspend or reactivate a clinic;
- send a clinic a payment link for any plan and amount, or grant a plan for free;
- cancel a term that hasn't started yet;
- manage the plan catalogue: create, edit, activate and deactivate plans, including private packages;
- decide which plans each clinic sees, including special prices for one clinic.

Every admin action arrives through new `api/internal/*` endpoints protected by the existing `InternalService` scheme.

On the clinic side, the Clinic Admin's Subscription page shows the current plan and any upcoming plan. A plan bought mid-term starts automatically, with no gap, the moment the current one ends. The Clinic Admin can switch plans while a link is open, and can ask the app to check a payment straight away instead of waiting. A suspended clinic shows a "suspended" screen to every user.

---

## Background (verified 2026-09-29)

**The reported problem.** A clinic "not active after payment" was not an access problem. In the local DB, both paid onboarding requests were activated within about 20 seconds of payment, and both clinics are active in Auth. The Admin portal showed a stale page: it loads once and has no Clinics page. Also, in Dev, `Razorpay:WebhookSecret` is empty, so webhooks never arrive. Activation then depends on the doctor returning to the payment-callback page, or on the 15-minute reconciliation sweep, which only picks up payments older than 10 minutes.

**Defects in the current code that this plan fixes:**

| # | Defect | Effect |
|---|--------|--------|
| D1 | `ClinicSubscriptionRepository.GetActiveForClinicAsync` ignores `StartDate` and orders by `EndDate` | A queued (future) term is reported as the current plan straight after purchase, with the wrong plan name and start date |
| D2 | A queued term starts at `EndDate + 1 day` (`ActivateSubscriptionTaskHandler`) | Once D1 is fixed, the clinic would be locked for 24 hours between terms |
| D3 | Chaining relies on `IsCurrentlyActive(latest)` | Once D1 is fixed, a second purchase made while a term is already queued would start "now" and overlap |
| D4 | `CreateRenewalPaymentLinkHandler` returns any open renewal link, even for a different plan | A Clinic Admin who picks Quarterly while a Monthly link is open gets the Monthly link again |
| D5 | Renewal payments have no "I've paid" check (onboarding does) | Activation waits for a webhook or the sweep, which takes 10–25 minutes in Dev |
| D6 | `CancelSubscriptionHandler` sets the status to Cancelled, which locks the clinic immediately. Its own comment says access continues until `EndDate`. No screen calls it | A latent lock-out through the API |
| D7 | `CancelPaymentLinkTaskHandler.HandleAlreadyPaidAsync` does nothing for a paid non-onboarding payment that has no replacement | A renewal link paid just as it was cancelled would never activate |

---

## Decisions Taken (redirect before execution if wrong)

| # | Decision | Why |
|---|----------|-----|
| 1 | **Clinic suspension lives in the App.** It is stored in a new `ClinicAccount` row and enforced by the existing `SubscriptionGuardMiddleware`: no new middleware and no pipeline reorder. Auth is untouched. Users can still sign in, but every guarded clinic-scoped call returns **403** with `errorCode = clinic_suspended`, and the Blazor layout shows a suspended screen to every role | This is the smallest change that fully blocks use of the clinic, and it needs no Auth work |
| 2 | **Suspension does not pause the subscription clock.** The term keeps running while the clinic is suspended; an admin can compensate with a free grant | Simple, predictable rule. Pausing would ripple through every date |
| 3 | **Terms are contiguous.** A new term starts exactly at the clinic's current coverage end, or now if the clinic has no coverage. "Current" means Active and StartDate ≤ now ≤ EndDate. "Upcoming" means Active and StartDate > now | Fixes D1–D3. No gap, no overlap |
| 4 | **Plan catalogue:** the four seeded plans keep their codes. Admin-created plans use a new code, `SubscriptionPlanCode.Custom = 5`. The unique index on `Code` becomes a filtered unique index (`Code <> 5`), so `GetByCodeAsync(Trial)` stays unambiguous. A new `IsPublic` flag decides whether a plan appears on clinics' self-serve page | Makes packages configurable without breaking the existing Trial lookup |
| 5 | **Per-clinic packages:** a new `ClinicPlanOffer` gives one clinic access to a plan, optionally at a special price and with an expiry. `ClinicAccount.RestrictToOfferedPlans` hides the public catalogue from that clinic | Covers "monthly or yearly package we decide, per clinic" |
| 6 | **Admin payment link for an existing clinic** uses a new `PaymentPurpose.AdminAssigned = 3`. The admin enters the amount; a reason is required when it differs from the clinic's effective price. The link is emailed to the clinic contact and shows on the clinic's Subscription page. Once paid, it activates exactly like a renewal | Keeps payments auditable and reuses the whole durable payment workflow |
| 7 | **Free grant** creates the `ClinicSubscription` directly (price 0, with `GrantedByAdminEmail` and `GrantReason`) and queues it after current coverage. In the Admin portal this is Operator-only | Replaces manual DB edits |
| 8 | **One open link per clinic, newest wins.** Creating a new link while one is open supersedes the old one, using the existing CancelPaymentLink → replacement mechanism. If the old link was already paid, that payment is kept and activated, and the new one is dropped. A Clinic Admin cannot replace an admin-assigned link (409) | Fixes D4 and prevents double payment |
| 9 | **Cancelling a term is admin-only, and only for a term that has not started.** Later upcoming terms are moved earlier to close the gap. A cancelled paid term needs a manual refund outside the system | Refunds stay out of scope. Fixes D6 |
| 10 | **The clinic-side cancel endpoint is retired.** `POST api/subscriptions/{id}/cancel` now always returns 409 "Contact CloudClinic support". The route stays for compatibility | Fixes D6 without deleting API surface |
| 11 | **Clinic existence is trusted from the internal caller.** The App has no local clinic registry and no service-key read from Auth. The Admin portal only sends application ids it read from Auth's `Applications` table (clinic type), plus the clinic name and contact snapshot. The App falls back to its own latest subscription snapshot when a contact field is blank | Keeps Auth unaffected. The internal endpoints are service-key only |

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Shared | 1 new enum, 2 enum values, 9 new DTOs, 3 DTOs extended, 1 error code |
| Domain | 2 new entities + 2 repository interfaces; `ClinicSubscription`, `SubscriptionPlan`, `SubscriptionPayment` extended; 3 repository interfaces extended |
| Infrastructure | 2 EF configs + 2 repositories; 3 configs and 3 repositories modified; DbContext (+2 DbSets → 46); DI |
| Application | New `Features/PlatformAdmin` area (12 commands); 1 new Subscriptions command; a plan-catalogue service; 10 existing handlers or translators modified |
| API | 2 new internal controllers; `SubscriptionPaymentsController` (+1 endpoint); `SubscriptionGuardMiddleware` (suspension check) |
| Web.Client | Subscription page rework, suspended screen, banner/pill/context updates, API client (+1 call) |

---

## Entity: ClinicAccount (new)

The platform-side state of one clinic. Phase 2 will add its billing model here. If no row exists, the clinic is Active and sees the public catalogue; a row is created on the first admin action that needs one.

**Tenant-scoped:** Yes (one row per clinic)
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required, **unique index** |
| AccessStatus | ClinicAccessStatus | int; Active = 1, Suspended = 2; default Active |
| SuspensionMessage | string? | max 500. Shown to the clinic's users |
| SuspendedAt | DateTime? | UTC |
| SuspendedByAdminEmail | string? | max 256 |
| ReactivatedAt | DateTime? | UTC |
| ReactivatedByAdminEmail | string? | max 256 |
| RestrictToOfferedPlans | bool | default false |
| RowVersion | byte[] | concurrency token |
| CreatedAt / UpdatedAt / CreatedBy / UpdatedBy | — | AuditableEntity |

Domain methods:
- `CreateDefault(applicationId)`
- `Suspend(message, adminEmail)`: throws InvalidOperationException if already suspended.
- `Reactivate(adminEmail)`: throws if not suspended.
- `SetRestrictToOfferedPlans(bool)`

## Entity: ClinicPlanOffer (new)

Makes a plan available to one clinic, optionally at a special price.

**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required, indexed |
| SubscriptionPlanId | Guid | required, FK → SubscriptionPlans (Restrict) |
| OfferPrice | decimal? | (18,2). Null means the plan's list price |
| ValidUntil | DateTime? | UTC. Null means no expiry |
| Note | string? | max 250 |
| IsActive | bool | default true |
| CreatedByAdminEmail | string | required, max 256 |
| WithdrawnAt | DateTime? | |
| WithdrawnByAdminEmail | string? | max 256 |
| RowVersion | byte[] | |

Unique filtered index on (ApplicationId, SubscriptionPlanId) where IsActive = 1: at most one live offer per plan per clinic.

Domain methods:
- `Create(...)`
- `Withdraw(adminEmail)`: idempotent-safe; throws if already withdrawn.
- `IsLive(utcNow)`: IsActive and (ValidUntil is null or ValidUntil ≥ now).

## Entity changes

| Entity | Change |
|--------|--------|
| `ClinicSubscription` | New columns: `GrantedByAdminEmail` (string?, 256), `GrantReason` (string?, 500), `CancellationReason` (string?, 500). `IsCurrentlyActive(utcNow)` now also requires StartDate ≤ now. New `IsUpcoming(utcNow)`. New factory `CreateAdminGrant(applicationId, plan, startDate, clinicName, contactEmail, contactName, adminEmail, reason)` with price paid 0. New `CancelUpcoming(adminEmail, reason)`, which throws unless the term is upcoming. New `Reschedule(newStartDate)`, upcoming only, which recomputes EndDate from DurationDays |
| `SubscriptionPlan` | New `IsPublic` (bool, default true). New factory `CreateCustom(name, description, durationDays, price, isPopular, displayOrder, isPublic)`: Code Custom, currency INR, IsTrial false, active. New `UpdateDetails(name, description, durationDays, price, isPopular, displayOrder, isPublic)`, which throws if the plan is the Trial and the price is not 0. New `Activate()` / `Deactivate()` |
| `SubscriptionPayment` | New columns: `InitiatedByAdminEmail` (string?, 256), `AmountReason` (string?, 500), `ClinicName` (string?, 200), `LastCheckAt` (DateTime?). New factory `CreateForAdminAssignment(applicationId, plan, amount, listPrice, clinicName, payerName, payerEmail, payerPhone, adminEmail, amountReason)` with Purpose AdminAssigned. `CreateForRenewal` gains optional `listPrice` (defaults to amount, so existing callers are unchanged) and `clinicName`. New `RecordCheck()` |
| `SubscriptionPlanCode` (Shared enum) | Add `Custom = 5` |
| `PaymentPurpose` (Shared enum) | Add `AdminAssigned = 3` |
| `ClinicAccessStatus` (Shared enum, new) | `Active = 1`, `Suspended = 2` |

---

## Subscription timeline rules (the core fix)

1. **Current term:** Status Active, StartDate ≤ now and EndDate ≥ now. If two overlap at the exact boundary instant, the one with the latest StartDate wins.
2. **Upcoming terms:** Status Active and StartDate > now, ordered by StartDate.
3. **Coverage end:** the maximum EndDate over Active terms with EndDate ≥ now, including upcoming ones. It is null when the clinic has no current or upcoming term.
4. **Every new term** starts at coverage end, or now when coverage end is null. This covers paid onboarding, renewals, admin links and admin grants.
5. **The guard** allows the request only when a current term exists (rule 1). An upcoming term alone does not unlock the clinic.
6. **Cancelling an upcoming term:** after `CancelUpcoming`, every later upcoming term of the same clinic is rescheduled in StartDate order to start at the new coverage end, keeping its duration. The work is done in one SaveChanges.
7. **Status DTO:** "expiring soon" is computed from coverage end, not from the current term's end. A clinic that has already bought its next term never sees the expiry banner.

---

## Plan visibility & pricing rules (per clinic)

A plan is **visible** to a clinic when it is active and either of these holds:
- it is public and the clinic does not have `RestrictToOfferedPlans`; or
- the clinic has a live offer for it.

The Trial keeps its existing rule: it is shown only to a clinic with no history, and shown as "already used" otherwise.

The **effective price** is the live offer's `OfferPrice` if one is set, otherwise the plan's list price.

Both rules live in one Application service, `IClinicPlanCatalog`, used by the plan list, the clinic renewal link, and the admin assignment's price comparison. An admin can **assign** any active non-trial plan by link, and grant any active plan free, whether or not the plan is visible to the clinic.

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Enums/ClinicAccessStatus.cs` | Active = 1, Suspended = 2 (integer values are a contract with TenantCore.Admin) |
| `Dtos/Subscriptions/UpcomingSubscriptionDto.cs` | PlanName, StartDate, EndDate, PricePaid, Currency, IsGrant |
| `Dtos/PlatformAdmin/ClinicContactRequest.cs` | ClinicName, ContactName, ContactEmail, ContactPhone?: the clinic/billing snapshot supplied by the Admin portal |
| `Dtos/PlatformAdmin/SuspendClinicRequest.cs` | Message + AdminUserId/AdminEmail |
| `Dtos/PlatformAdmin/GrantClinicSubscriptionRequest.cs` | SubscriptionPlanId, Reason, Contact + admin fields |
| `Dtos/PlatformAdmin/AssignPlanPaymentLinkRequest.cs` | SubscriptionPlanId, Amount, AmountReason?, Contact + admin fields |
| `Dtos/PlatformAdmin/CancelUpcomingSubscriptionRequest.cs` | Reason + admin fields |
| `Dtos/PlatformAdmin/SetClinicPlanVisibilityRequest.cs` | RestrictToOfferedPlans + admin fields |
| `Dtos/PlatformAdmin/CreateClinicPlanOfferRequest.cs` | SubscriptionPlanId, OfferPrice?, ValidUntil?, Note? + admin fields |
| `Dtos/PlatformAdmin/SaveSubscriptionPlanRequest.cs` | Name, Description, DurationDays, Price, IsPopular, DisplayOrder, IsPublic + admin fields (used by both create and update) |

Admin fields follow the existing `AdminActionRequest` shape (AdminUserId, AdminEmail); no-body actions reuse `AdminActionRequest` itself.

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Entities/ClinicAccount.cs` | Entity above |
| `Entities/ClinicPlanOffer.cs` | Entity above |
| `Interfaces/IClinicAccountRepository.cs` | `GetByApplicationIdAsync` (tracked), `IsSuspendedAsync` (no-tracking, indexed) |
| `Interfaces/IClinicPlanOfferRepository.cs` | `GetLiveForClinicAsync(applicationId, utcNow)`, `GetActiveForClinicAndPlanAsync(applicationId, planId)`, `GetByIdForClinicAsync(id, applicationId)` |

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Persistence/Configurations/Clinic/ClinicAccountConfiguration.cs` | Table `clinic.ClinicAccounts`; unique index on ApplicationId; enum as int; max lengths; RowVersion |
| `Persistence/Configurations/Clinic/ClinicPlanOfferConfiguration.cs` | Table `clinic.ClinicPlanOffers`; FK to SubscriptionPlans (Restrict); filtered unique index; decimal(18,2); RowVersion |
| `Repositories/ClinicAccountRepository.cs` | Extends `ClinicRepository<ClinicAccount>` |
| `Repositories/ClinicPlanOfferRepository.cs` | Extends `ClinicRepository<ClinicPlanOffer>` |

### Application Layer: new area `src/TenantCore.Application/Features/PlatformAdmin/`

Every command is a `sealed record` implementing `IRequest` / `IRequest<Guid>`, `IBusinessAction` (ActionName) and `IActionLogContext`. Each carries `AdminUserId` and `AdminEmail`, and those that act on one clinic also carry `ApplicationId`.

| Command (+ Handler + Validator) | Returns | What the handler does |
|---|---|---|
| `SuspendClinicCommand` | — | Get or create the ClinicAccount, then `Suspend`. 409 if already suspended |
| `ReactivateClinicCommand` | — | 409 if not suspended, or if no account row exists |
| `GrantClinicSubscriptionCommand` | Guid (subscription id) | Plan must exist and be active. StartDate = coverage end or now. `ClinicSubscription.CreateAdminGrant`. Enqueue a `SendEmail` "SubscriptionGranted" (ClinicName, PlanName, StartDate, EndDate) in the same SaveChanges |
| `AssignPlanPaymentLinkCommand` | Guid (payment id) | Payments configured, or 409. Plan active and non-trial. Amount within `Onboarding:Min/MaxPaymentAmount` with 2 decimals. Reason required when the amount differs from the effective price. `SubscriptionPayment.CreateForAdminAssignment`. If an open link exists (Renewal or AdminAssigned), supersede it (`SetReplacement` + enqueue `CancelPaymentLink` with the same key format as ChangePaymentAmount). Otherwise enqueue `CreatePaymentLink` directly |
| `CancelUpcomingSubscriptionCommand` | — | The subscription must belong to the clinic (else 404) and be upcoming (else 409). `CancelUpcoming`, then reschedule later upcoming terms (timeline rule 6) |
| `CancelClinicPaymentLinkCommand` | — | The payment must belong to the clinic, be Renewal or AdminAssigned, and be Pending/LinkCreated (else 409). Enqueue `CancelPaymentLink` with no replacement |
| `SetClinicPlanVisibilityCommand` | — | Get or create the ClinicAccount, then `SetRestrictToOfferedPlans` |
| `CreateClinicPlanOfferCommand` | Guid (offer id) | Plan active and non-trial; no live offer for the same plan (409). OfferPrice within min/max. ValidUntil in the future |
| `WithdrawClinicPlanOfferCommand` | — | The offer must belong to the clinic, else 404; 409 if already withdrawn |
| `CreateSubscriptionPlanCommand` | Guid (plan id) | Name unique among all plans, case-insensitive (409). `SubscriptionPlan.CreateCustom` |
| `UpdateSubscriptionPlanCommand` | — | Same name rule, excluding itself. Trial price must stay 0 (409). Existing subscriptions and open links are unaffected, because they hold snapshots |
| `SetSubscriptionPlanActiveCommand` | — | Activate or deactivate. Deactivating hides the plan from the self-serve page and from new offers or links; existing terms and open links are unaffected |

| Supporting file | Purpose |
|---|---|
| `Features/PlatformAdmin/Translators/PlatformAdminTranslator.cs` | Static: request DTO → command fields; contact fallback resolution (blank ContactName → ClinicName, per workspace rule 9) |
| `Common/AdminActorValidationExtensions.cs` | Shared FluentValidation rules for AdminUserId (not empty) and AdminEmail (not empty, email, max 256), used by every PlatformAdmin validator |
| `Features/Subscriptions/Services/IClinicPlanCatalog.cs` | `GetPlansForClinicAsync(applicationId)` returns visible plans with effective price, offer flag and offer expiry. `IsVisibleAsync(applicationId, planId)` answers the visibility rule. `GetEffectivePriceAsync(applicationId, plan)` returns the live offer price or else the list price, **regardless of visibility**, because admin assignment uses it for any plan |
| `Features/Subscriptions/Services/ClinicPlanCatalog.cs` | Implements the visibility and pricing rules above (registered Scoped in Application DI) |
| `Features/Subscriptions/Commands/CheckSubscriptionPaymentCommand.cs` | Clinic Admin "I've paid" check: ApplicationId, PaymentId, UserId |
| `Features/Subscriptions/Handlers/CheckSubscriptionPaymentHandler.cs` | Payment must belong to the clinic (else 404), be Renewal or AdminAssigned and LinkCreated (else 409). Throttled to one check per 30 s via `LastCheckAt`, else `TooManyRequestsException`. Enqueues `ConfirmPayment` with the same idempotency key the webhook and sweep use |
| `Features/Subscriptions/Validators/CheckSubscriptionPaymentCommandValidator.cs` | Non-empty ids |

### API Layer (`src/TenantCore.Api/Controllers/Internal/`)

| File | Purpose |
|------|---------|
| `InternalClinicsController.cs` | `api/internal/clinics/{applicationId:guid}/…`, `[Authorize(AuthenticationSchemes = InternalService)]`, only `sender.Send`. Mirrors `InternalOnboardingController` |
| `InternalPlansController.cs` | `api/internal/plans/…`, same scheme and pattern |

### Web.Client (`src/TenantCore.Web.Client/`)

| File | Purpose |
|------|---------|
| `Components/Subscription/ClinicSuspended.razor` | Full-content "This clinic has been suspended" panel showing `SuspensionMessage` and a contact-support line. Rendered for every role |
| `Components/Subscription/SubscriptionTimelineCard.razor` | Current term card (plan, start, end, days left) plus upcoming term cards ("Starts automatically on …") |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Shared/Enums/SubscriptionPlanCode.cs` | Add `Custom = 5` |
| `src/TenantCore.Shared/Enums/PaymentPurpose.cs` | Add `AdminAssigned = 3` |
| `src/TenantCore.Shared/Dtos/Subscriptions/SubscriptionStatusDto.cs` | Add `IsSuspended`, `SuspensionMessage`, `CoverageEndDate`, `CoverageDaysRemaining`, `Upcoming` (list of UpcomingSubscriptionDto) |
| `src/TenantCore.Shared/Dtos/Subscriptions/SubscriptionPlanDto.cs` | Add `ListPrice`, `IsSpecialOffer`, `OfferValidUntil`. `Price` now carries the effective price |
| `src/TenantCore.Shared/Dtos/Subscriptions/SubscriptionPaymentDto.cs` | Add `IsAssignedByPlatform` (Purpose == AdminAssigned) |
| `src/TenantCore.Shared/Errors/SubscriptionErrorCodes.cs` | Add `ClinicSuspended = "clinic_suspended"` |
| `src/TenantCore.Domain/Entities/ClinicSubscription.cs` | Entity changes above |
| `src/TenantCore.Domain/Entities/SubscriptionPlan.cs` | Entity changes above. Update the class comment: the catalogue is admin-managed, and per-clinic access is via ClinicPlanOffer |
| `src/TenantCore.Domain/Entities/SubscriptionPayment.cs` | Entity changes above |
| `src/TenantCore.Domain/Interfaces/IClinicSubscriptionRepository.cs` | Add `GetUpcomingForClinicAsync` (tracked, ordered by StartDate) and `GetCoverageEndAsync(applicationId, utcNow)` |
| `src/TenantCore.Domain/Interfaces/ISubscriptionPlanRepository.cs` | Add `NameExistsAsync(name, excludeId?)` and `GetAllAsync` ordered by DisplayOrder (for the catalogue service) |
| `src/TenantCore.Domain/Interfaces/ISubscriptionPaymentRepository.cs` | Replace `GetOpenRenewalForClinicAsync` with `GetOpenLinkForClinicAsync` (Renewal or AdminAssigned, Pending/LinkCreated). Add `GetByIdForClinicAsync(id, applicationId)` |
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add `DbSet<ClinicAccount> ClinicAccounts` and `DbSet<ClinicPlanOffer> ClinicPlanOffers` (44 → 46) |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `IClinicAccountRepository` and `IClinicPlanOfferRepository` (Scoped) |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/SubscriptionPlanConfiguration.cs` | `IsPublic` (default true). Replace the unique index on Code with a filtered unique index (`[Code] <> 5`). Seed data unchanged, apart from the new column default |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/ClinicSubscriptionConfiguration.cs` | Three new nullable columns |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/SubscriptionPaymentConfiguration.cs` | Four new nullable columns |
| `src/TenantCore.Infrastructure/Repositories/ClinicSubscriptionRepository.cs` | `GetActiveForClinicAsync` adds `StartDate <= now` and orders by StartDate descending (fixes D1). Implement the two new methods |
| `src/TenantCore.Infrastructure/Repositories/SubscriptionPlanRepository.cs` | Implement the new methods |
| `src/TenantCore.Infrastructure/Repositories/SubscriptionPaymentRepository.cs` | Implement the renamed and new methods |
| `src/TenantCore.Application/DependencyInjection.cs` | Register `IClinicPlanCatalog → ClinicPlanCatalog` (Scoped) |
| `src/TenantCore.Application/Features/Subscriptions/Handlers/GetSubscriptionStatusHandler.cs` | Also loads upcoming terms, coverage end and suspension state |
| `src/TenantCore.Application/Features/Subscriptions/Translators/SubscriptionTranslator.cs` | `ToStatusDto` takes upcoming terms, coverage end and suspension state. Expiring-soon is computed from coverage end. `ToPlanDto` takes the effective price and offer info |
| `src/TenantCore.Application/Features/Subscriptions/Handlers/GetSubscriptionPlansHandler.cs` | Delegates to `IClinicPlanCatalog` |
| `src/TenantCore.Application/Features/Subscriptions/Handlers/CreateRenewalPaymentLinkHandler.cs` | 409 if suspended. 404 if the plan is not visible to the clinic. Amount = effective price, list price = plan price. Stores the clinic name. Open-link handling per Decision 8: same plan and amount returns the existing link; a different plan supersedes; an open AdminAssigned link returns 409 |
| `src/TenantCore.Application/Features/Subscriptions/Handlers/SubscribeToPlanHandler.cs` | 409 if suspended |
| `src/TenantCore.Application/Features/Subscriptions/Handlers/CancelSubscriptionHandler.cs` | Always throws InvalidOperationException ("Subscriptions can't be cancelled from the clinic app — contact CloudClinic support"). Decision 10 |
| `src/TenantCore.Application/Features/Onboarding/Tasks/ActivateSubscriptionTaskHandler.cs` | Start date = `GetCoverageEndAsync` ?? now (fixes D2 and D3). Clinic name = request name, else payment's ClinicName, else latest snapshot, else plan name. The activation email data gains `StartDate` |
| `src/TenantCore.Application/Features/Onboarding/Tasks/ConfirmPaymentTaskHandler.cs` | Downstream for `Renewal or AdminAssigned` → ActivateSubscription |
| `src/TenantCore.Application/Features/Onboarding/Tasks/CancelPaymentLinkTaskHandler.cs` | `HandleAlreadyPaidAsync`: a paid non-onboarding payment with no replacement enqueues `ConfirmPayment` (fixes D7) |
| `src/TenantCore.Application/Features/Onboarding/Tasks/CreatePaymentLinkTaskHandler.cs` | Email template `AssignedPaymentLink` for AdminAssigned (same data shape as RenewalPaymentLink, plus PlanName). RecipientName falls back to the clinic name |
| `src/TenantCore.Api/Controllers/SubscriptionPaymentsController.cs` | Add `POST api/subscriptions/payments/{id:guid}/check` |
| `src/TenantCore.Api/Middleware/SubscriptionGuardMiddleware.cs` | For guarded requests, check suspension first. It always runs, even when `Subscription:GuardEnabled` is false, and returns 403 with ProblemDetails `errorCode = clinic_suspended`. Then the existing subscription check. The exempt list is unchanged (`/api/subscriptions` stays reachable so the client can read its status). No pipeline reorder |
| `src/TenantCore.Web.Client/Services/SubscriptionContextService.cs` | Add `IsSuspended` |
| `src/TenantCore.Web.Client/Layout/AuthorizedLayout.razor` | New branch before the locked branch: when `SubscriptionContext.IsSuspended`, render `<ClinicSuspended />` for every role |
| `src/TenantCore.Web.Client/Pages/Subscription/SubscriptionPlans.razor` | Rework, see below |
| `src/TenantCore.Web.Client/Components/Subscription/SubscriptionBanner.razor` | Uses coverage end. Shows "Next: <plan> from <date>" when an upcoming term exists |
| `src/TenantCore.Web.Client/Components/Subscription/SubscriptionStatusPill.razor` | Days left counts to coverage end; tooltip shows current and upcoming |
| `src/TenantCore.Web.Client/Clients/ISubscriptionApiClient.cs` / `SubscriptionApiClient.cs` | Add `CheckPaymentAsync(paymentId)` |
| `src/TenantCore.Web.Client/Clients/ApiResponseReader.cs` | A 403 carrying `errorCode = clinic_suspended` returns the ProblemDetails detail, not the generic "no permission" text |

**Subscription page rework (`/subscription`):**
- **Title:** "Activate your clinic" when locked, otherwise "Subscription".
- **Timeline:** `SubscriptionTimelineCard` at the top.
- **Plan cards:**
  - Show the effective price; a special offer gets a "Special price for your clinic" badge, the list price struck through and the offer's end date.
  - While the clinic is covered, the button reads "Buy — starts <coverage end date>".
- **Pending-link card:**
  - Buttons: "Pay now", "I've paid — check now" and "Choose a different plan". The check button calls the check endpoint and then reloads status once after 5 s (a single delayed refresh, not a loop). The different-plan button appears only for Renewal links and asks for confirmation that the open link will be cancelled.
  - An admin-assigned link is labelled "Sent by CloudClinic" and has no plan-switch button.
- **Payment history:** gains a Source column (Onboarding / Renewal / Sent by CloudClinic).

---

## API Endpoints

### Internal (Admin portal only — `InternalService` scheme, `X-Internal-Service-Key`)

| Method | Route | Request Body | Response |
|--------|-------|-------------|----------|
| POST | `api/internal/clinics/{applicationId}/suspend` | `SuspendClinicRequest` | 204 · 409 already suspended |
| POST | `api/internal/clinics/{applicationId}/reactivate` | `AdminActionRequest` | 204 · 409 not suspended |
| POST | `api/internal/clinics/{applicationId}/subscriptions/grant` | `GrantClinicSubscriptionRequest` | 201 `Guid` · 404 plan |
| POST | `api/internal/clinics/{applicationId}/subscriptions/payment-link` | `AssignPlanPaymentLinkRequest` | 201 `Guid` · 404 plan · 409 payments not configured / trial plan |
| POST | `api/internal/clinics/{applicationId}/subscriptions/{subscriptionId}/cancel-upcoming` | `CancelUpcomingSubscriptionRequest` | 204 · 404 · 409 already started |
| POST | `api/internal/clinics/{applicationId}/payments/{paymentId}/cancel-link` | `AdminActionRequest` | 204 · 404 · 409 not open |
| PUT | `api/internal/clinics/{applicationId}/plan-visibility` | `SetClinicPlanVisibilityRequest` | 204 |
| POST | `api/internal/clinics/{applicationId}/plan-offers` | `CreateClinicPlanOfferRequest` | 201 `Guid` · 404 plan · 409 duplicate |
| POST | `api/internal/clinics/{applicationId}/plan-offers/{offerId}/withdraw` | `AdminActionRequest` | 204 · 404 · 409 |
| POST | `api/internal/plans` | `SaveSubscriptionPlanRequest` | 201 `Guid` · 409 duplicate name |
| PUT | `api/internal/plans/{planId}` | `SaveSubscriptionPlanRequest` | 204 · 404 · 409 |
| POST | `api/internal/plans/{planId}/activate` | `AdminActionRequest` | 204 · 404 |
| POST | `api/internal/plans/{planId}/deactivate` | `AdminActionRequest` | 204 · 404 |

`applicationId` comes from the route only. `ClinicControllerBase`/`X-Application-Id` do not apply here: this is a service-to-service caller, exactly like `InternalOnboardingController`. The Admin portal reads everything directly from the database, so no GET endpoints are added.

### Clinic-facing (changed)

| Method | Route | Change | Auth Policy |
|--------|-------|--------|-------------|
| GET | `api/subscriptions/status` | Response gains suspension, coverage end and upcoming terms | RequireAuthenticated |
| GET | `api/subscriptions/plans` | Visibility and effective price per clinic | RequireAuthenticated |
| POST | `api/subscriptions/payments/links` | Visibility, effective price and supersede rules | RequireClinicAdmin |
| POST | `api/subscriptions/payments/{id}/check` | **New.** 204 · 404 · 409 · 429 | RequireClinicAdmin |
| POST | `api/subscriptions/{id}/cancel` | Always 409 (retired) | RequireClinicAdmin |

---

## Validation Rules

| Field | Rules |
|-------|-------|
| AdminUserId | NotEmpty (all PlatformAdmin commands) |
| AdminEmail | NotEmpty, EmailAddress, MaxLength(256) |
| ApplicationId / SubscriptionPlanId / SubscriptionId / PaymentId / OfferId | NotEmpty |
| Suspension Message | NotEmpty, 3–500 chars |
| Grant Reason / CancelUpcoming Reason | NotEmpty, 3–500 chars |
| AmountReason | MaxLength(500). Required (3+ chars) when amount ≠ effective price (enforced in handler) |
| Amount / OfferPrice | Between `Onboarding:MinPaymentAmount` and `Onboarding:MaxPaymentAmount`, at most 2 decimals |
| OfferValidUntil | If set, must be in the future |
| Offer Note | MaxLength(250) |
| Contact.ClinicName | NotEmpty, MaxLength(200) |
| Contact.ContactEmail | NotEmpty, EmailAddress, MaxLength(256) |
| Contact.ContactName | MaxLength(200). If blank, the translator falls back to ClinicName |
| Contact.ContactPhone | Optional, MaxLength(20) |
| Plan Name | NotEmpty, 3–60 chars |
| Plan Description | MaxLength(250) |
| Plan DurationDays | 1–1095 |
| Plan Price | 0 to `MaxPaymentAmount`, 2 decimals. At least `MinPaymentAmount` for any non-trial plan (handler knows which plan is the Trial) |
| Plan DisplayOrder | 0–999 |

---

## Business Rules

1. **New terms never overlap and never leave a gap.** Start = coverage end, or now (timeline rules 3–4).
2. **Only a current term unlocks the clinic.** An upcoming term alone does not.
3. **A suspended clinic is blocked on every guarded route (403 `clinic_suspended`) whatever its subscription.** It can still read `api/subscriptions/status` and use the payment-check endpoint, but cannot subscribe or create links (409).
4. **At most one open link (Renewal or AdminAssigned) per clinic.** A newer admin link always supersedes. A Clinic Admin link supersedes only another Renewal link; against an AdminAssigned link it is refused with 409. If the superseded link turns out to be paid, it is kept and activated, and the replacement is cancelled (existing CancelPaymentLink behaviour, extended by D7).
5. **Offers:**
   - at most one live offer per (clinic, plan) (409);
   - no offers for the Trial plan;
   - an offer on a deactivated plan is invisible until the plan is reactivated.
6. **Plans:**
   - names are unique (409);
   - the Trial price is always 0;
   - Code is immutable;
   - deactivating never touches existing terms or open links.
7. **Cancelling a term:** only an upcoming term can be cancelled, and only by an admin. Later upcoming terms are re-chained. There is no automatic refund.
8. **Cross-clinic ids are not-found (404):** a subscription, payment or offer id belonging to another clinic.
9. **Money arriving always wins.** Any paid link, including one paid after cancellation or supersession, ends in an activated term. Nothing is silently discarded.

---

## Multi-Tenancy Checklist

- [ ] `ApplicationId` on `ClinicAccount` and `ClinicPlanOffer`
- [ ] Every PlatformAdmin command that acts on a clinic carries `ApplicationId` from the **route**, never from the body
- [ ] Repository queries filter by `applicationId`; lookups by id also check the clinic (`GetByIdForClinicAsync`)
- [ ] Clinic-facing endpoints use `GetApplicationId()` (unchanged)
- [ ] `SubscriptionPlan` stays global (not tenant-scoped); per-clinic access goes through `ClinicPlanOffer`

---

## Logging (ADR-011 — mandatory)

| Operation | How it is logged | Searchable context (identifiers only) |
|---|---|---|
| `SuspendClinicCommand` | ActionLogs (automatic, `…Command`) "Clinic Suspended" | `IActionLogContext`: `applicationId=…; adminUserId=…` |
| `ReactivateClinicCommand` | ActionLogs "Clinic Reactivated" | `applicationId; adminUserId` |
| `GrantClinicSubscriptionCommand` | ActionLogs "Subscription Granted by Admin" | `applicationId; planId; subscriptionId; adminUserId` |
| `AssignPlanPaymentLinkCommand` | ActionLogs "Plan Payment Link Sent by Admin" | `applicationId; planId; paymentId; supersededPaymentId (if any); adminUserId` |
| `CancelUpcomingSubscriptionCommand` | ActionLogs "Upcoming Subscription Cancelled" | `applicationId; subscriptionId; rescheduled=<n>; adminUserId` |
| `CancelClinicPaymentLinkCommand` | ActionLogs "Clinic Payment Link Cancelled" | `applicationId; paymentId; adminUserId` |
| `SetClinicPlanVisibilityCommand` | ActionLogs "Clinic Plan Visibility Changed" | `applicationId; restrict=true/false; adminUserId` |
| `CreateClinicPlanOfferCommand` / `WithdrawClinicPlanOfferCommand` | ActionLogs "Clinic Plan Offer Created/Withdrawn" | `applicationId; planId; offerId; adminUserId` |
| `CreateSubscriptionPlanCommand` / `UpdateSubscriptionPlanCommand` / `SetSubscriptionPlanActiveCommand` | ActionLogs "Subscription Plan Created/Updated/Activated/Deactivated" | `planId; adminUserId` |
| `CheckSubscriptionPaymentCommand` | ActionLogs "Subscription Payment Check" | `applicationId; paymentId` |
| `POST/PUT api/internal/clinics/*`, `api/internal/plans/*` | ApiRequestLogs (automatic: data-changing) + ApiErrorLogs on exceptions | — |
| Guard: suspended request blocked | ApiRequestLogs (automatic: failed 403) | path + status only |
| Workflow tasks (CreatePaymentLink / CancelPaymentLink / ConfirmPayment / ActivateSubscription / SendEmail) for the new purpose | Unchanged existing task logging | `paymentId`, `subscriptionId` |

- [ ] Every failure path is an exception or ≥ 400 status (never 200 with an error body)
- [ ] No `ISkipActionLog`
- [ ] Nothing sensitive logged. Free-text reasons, suspension messages, contact names, emails and phones **never** go into `ActionLogContext`; they live only in the entity columns and the Admin audit
- [ ] Tests assert the `ActionLogContext` string for Suspend, Grant and AssignPlanPaymentLink

---

## Contract with TenantCore.Admin (coordinate — the portal reads these directly)

| Item | Value the portal must mirror |
|------|------------------------------|
| New table `clinic.ClinicAccounts` | all columns above |
| New table `clinic.ClinicPlanOffers` | all columns above |
| `clinic.SubscriptionPlans` | + `IsPublic` (also reads `Description`, `IsPopular`) |
| `clinic.ClinicSubscriptions` | + `GrantedByAdminEmail`, `GrantReason`, `CancellationReason` (also reads `SubscriptionPlanId`, `CreatedAt`, `DurationDays`) |
| `clinic.SubscriptionPayments` | + `InitiatedByAdminEmail`, `AmountReason`, `ClinicName`, `LastCheckAt` |
| Enums | `SubscriptionPlanCode.Custom = 5`, `PaymentPurpose.AdminAssigned = 3`, `ClinicAccessStatus` Active = 1 / Suspended = 2 |
| Internal endpoints | the 13 routes above; bodies as listed; admin fields AdminUserId + AdminEmail |

No column is renamed or retyped.

---

## EF Migration

**Migration name:** `ClinicSubscriptionAdminControls`

It creates `ClinicAccounts` and `ClinicPlanOffers`, adds the new nullable columns, adds `SubscriptionPlans.IsPublic` (default true), and swaps the unique index on `SubscriptionPlans.Code` for the filtered one.

Print the command and do not auto-run it: `dotnet ef migrations add ClinicSubscriptionAdminControls --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations`

---

## Implementation Order

1. Shared enums, error code and DTOs
2. Domain: new entities and repository interfaces; `ClinicSubscription` / `SubscriptionPlan` / `SubscriptionPayment` changes; extended interfaces
3. Infrastructure: new configurations and repositories; modified configurations and repositories; `ClinicDbContext` (+2 DbSets); DI
4. Application: `IClinicPlanCatalog` + implementation + DI; `AdminActorValidationExtensions`
5. Application: PlatformAdmin commands → validators → translator → handlers
6. Application: modified Subscriptions handlers and translator; `CheckSubscriptionPayment` command, validator and handler
7. Application: workflow task handler changes (Activate, Confirm, CancelLink, CreateLink)
8. API: `SubscriptionGuardMiddleware`; `InternalClinicsController`; `InternalPlansController`; `SubscriptionPaymentsController`
9. Web.Client: context service, layout, suspended screen, timeline card, Subscription page, banner, pill, API client, response reader
10. Unit tests (below); run `dotnet test`
11. Print the migration command
12. Update `.claude/context/current-state.md` (46 DbSets, 2 repositories, catalogue service) and `plan/REGISTRY.md`

---

## Test Files to Create / Update

| File | What it covers |
|------|---------------|
| `tests/TenantCore.Domain.Tests/Entities/ClinicSubscriptionTimelineTests.cs` | `IsCurrentlyActive` respects StartDate; `IsUpcoming`; `Reschedule` recomputes EndDate and refuses started terms; `CancelUpcoming` refuses started terms; `CreateAdminGrant` sets price 0 and grant fields |
| `tests/TenantCore.Domain.Tests/Entities/ClinicAccountTests.cs` | Suspend/Reactivate state rules; double-suspend throws |
| `tests/TenantCore.Domain.Tests/Entities/SubscriptionPlanTests.cs` | `CreateCustom` code and defaults; Trial price lock on `UpdateDetails` |
| `tests/TenantCore.Domain.Tests/Entities/ClinicPlanOfferTests.cs` | `IsLive` with and without ValidUntil; Withdraw |
| `tests/TenantCore.Application.Tests/Features/PlatformAdmin/Commands/SuspendClinicHandlerTests.cs` | Creates the account when missing; 409 when already suspended; context string |
| `…/PlatformAdmin/Commands/ReactivateClinicHandlerTests.cs` | 409 when not suspended or no account |
| `…/PlatformAdmin/Commands/GrantClinicSubscriptionHandlerTests.cs` | Start = coverage end; start = now when uncovered; email enqueued in the same save; contact fallback; inactive plan → 404 |
| `…/PlatformAdmin/Commands/AssignPlanPaymentLinkHandlerTests.cs` | Purpose AdminAssigned; reason required vs effective price (offer and no offer); supersedes an open Renewal link and an open AdminAssigned link; CreatePaymentLink enqueued when nothing is open; trial → 409 |
| `…/PlatformAdmin/Commands/CancelUpcomingSubscriptionHandlerTests.cs` | Later upcoming terms re-chained contiguously; started term → 409; other clinic's id → 404 |
| `…/PlatformAdmin/Commands/CancelClinicPaymentLinkHandlerTests.cs` | Enqueues cancel with no replacement; onboarding or paid payment → 409; cross-clinic → 404 |
| `…/PlatformAdmin/Commands/ClinicPlanOfferHandlerTests.cs` | Create (dup → 409, trial → 409, past ValidUntil → validation); Withdraw (404 / 409) |
| `…/PlatformAdmin/Commands/SetClinicPlanVisibilityHandlerTests.cs` | Creates the account when missing; toggles the flag |
| `…/PlatformAdmin/Commands/SubscriptionPlanHandlerTests.cs` | Create custom; duplicate name 409; update keeps Code; Trial price lock 409; activate/deactivate |
| `…/PlatformAdmin/Validators/<Command>ValidatorTests.cs` (one per PlatformAdmin validator) | Valid passes; each required field fails ([Theory]); max-length boundaries; admin fields; amount decimals and range |
| `…/Features/Subscriptions/Services/ClinicPlanCatalogTests.cs` | Public vs private; RestrictToOfferedPlans; live vs expired offer; effective price; inactive plan hidden; trial "already used" |
| `…/Features/Subscriptions/Commands/CheckSubscriptionPaymentHandlerTests.cs` | Enqueues ConfirmPayment; 30 s throttle → 429; wrong clinic → 404; not LinkCreated → 409 |
| `…/Features/Subscriptions/Queries/GetSubscriptionStatusHandlerTests.cs` (update) | Upcoming term only → locked; current + upcoming → coverage end = upcoming end, not expiring soon; suspended flag and message |
| `…/Features/Subscriptions/Queries/GetSubscriptionPlansHandlerTests.cs` (update) | Delegates to the catalogue; offer price surfaced |
| `…/Features/Subscriptions/Commands/CreateRenewalPaymentLinkHandlerTests.cs` (update) | Offer price used with list price kept; invisible plan → 404; suspended → 409; same plan returns the existing link; different plan supersedes; open AdminAssigned → 409 |
| `…/Features/Subscriptions/Commands/CancelSubscriptionHandlerTests.cs` (update) | Always 409 |
| `…/Features/Subscriptions/Translators/SubscriptionTranslatorTests.cs` (update) | Coverage-based expiring-soon; upcoming mapping; plan DTO offer fields |
| `…/Features/Onboarding/Tasks/ActivateSubscriptionTaskHandlerTests.cs` (update) | Renewal while covered starts exactly at EndDate (no +1 day); second purchase while one is queued chains after the queued one; AdminAssigned activates; clinic-name fallback order |
| `…/Features/Onboarding/Tasks/ConfirmPaymentTaskHandlerTests.cs` (create if absent, else update) | AdminAssigned → ActivateSubscription enqueued |
| `…/Features/Onboarding/Tasks/CancelPaymentLinkTaskHandlerTests.cs` (create if absent, else update) | Paid renewal with no replacement → ConfirmPayment enqueued (D7) |
| `tests/TenantCore.Api.Tests/Middleware/SubscriptionGuardMiddlewareTests.cs` | Suspended → 403 + `clinic_suspended`, even with GuardEnabled = false; exempt paths pass; upcoming-only → 402; current term → next called |
| `tests/TenantCore.Api.Tests/Controllers/Internal/InternalClinicsControllerTests.cs` | InternalService scheme attribute present; route applicationId (not body) reaches the command; admin fields mapped |
| `tests/TenantCore.Api.Tests/Controllers/Internal/InternalPlansControllerTests.cs` | Same for plans |

---

## Open Questions / Risks

- **Rows queued under the old "+1 day" rule.** They would open a 24-hour gap once D1 is fixed. Verified none exist locally on 2026-09-29: both subscriptions start immediately. Before deploying anywhere else, look for Active `ClinicSubscriptions` whose StartDate is in the future. If any exist, fix them with Admin's cancel-upcoming + grant, or a one-off manual correction. No raw SQL is put in the migration.
- **Guard cost.** The guard now does two indexed single-row lookups per clinic-scoped request (suspension, then current term). This is acceptable at the current scale; a short-lived cache can come later if needed.
- **Refunds stay manual.** This covers cancelling a paid upcoming term, and money that arrives on a superseded or cancelled link (the latter is activated, never lost).
- **Suspension is App-only.** It does not revoke Auth sessions or accounts: users can sign in and see the suspended screen. Blocking sign-in would be a separate Auth change.
- **Dev operations (not code).** Without `Razorpay:WebhookSecret` and a public webhook URL (e.g. a tunnel), Dev activation depends on the new "check now" buttons, the onboarding callback and the sweep. Consider lowering `Workflow:ReconciliationIntervalMinutes` locally.
- **Admin portal contract.** The Admin plan must land after this one, in the same release window. Admin's `EnumContractTests` will fail until its enums gain `Custom` and `AdminAssigned`, which is intended.
