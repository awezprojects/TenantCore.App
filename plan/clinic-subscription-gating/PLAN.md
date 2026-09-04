# Feature Plan: Clinic Subscription & Access Gating

**Repo:** TenantCore.App
**Date:** 2026-09-04
**Domain area:** Subscriptions (new)
**Status:** Approved — ready for execution

---

## Overview

Every clinic must hold an active subscription before anyone can use it. Four plans ship as seeded catalogue data — Trial, Monthly, Quarterly and Yearly — and the Clinic Admin of a clinic picks one; nobody else can. Until a clinic has an active subscription, its API surface is closed at the middleware layer and its UI shows a locked state instead of the dashboard, so no user of any role can view or work in that clinic. Once active, the dashboard carries a status strip showing how many days remain before expiry, and a countdown pill sits in the top bar on every page.

---

## Out of Scope (Deferred)

**All email sending and the reminder background job are out of scope for this plan** and will be built separately as an Azure Function. Nothing in this plan sends mail, and no `BackgroundService` is added.

Two things are retained here because the deferred Function will depend on them and they are cheap to ship now:

| Retained | Why |
|----------|-----|
| `SubscriptionAlertSetting` table + seed + admin endpoints | You asked for the reminder thresholds to live in a table so the super-admin portal can configure them. That requirement outlives the worker — the Function will read this table, and the admin portal will write it. It ships as configuration with no consumer inside this repo. |
| `BillingContactEmail` / `BillingContactName` / `ClinicName` snapshot on `ClinicSubscription` | `AuthApplicationService` forwards the caller's bearer token from `IHttpContextAccessor`. An out-of-process Azure Function has no user token and cannot ask TenantCore.Auth who to email. Capturing the contact at subscribe time is the only thing that makes the later Function possible, and it costs three columns. |

Say the word if you would rather drop the settings table too and let the Function own its own configuration schema — it is a two-file removal.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Shared | 7 DTOs, 3 enums, error codes |
| Domain | 3 new entities + 3 repository interfaces |
| Infrastructure | 3 EF configs (2 with seed data), 3 repositories, migration |
| Application | 3 commands + 4 queries, 7 handlers, 2 validators, 2 translators |
| API | 2 controllers, 1 new middleware |
| Web.Client | 1 typed client, 1 context service, 2 pages, 2 components, layout gate |
| Tests | 10 test files |

---

## Design Decisions Locked In

1. **Catalogue and purchase are separate entities.** `SubscriptionPlan` is a global, non-tenant-scoped lookup seeded with exactly four rows. `ClinicSubscription` is a tenant-scoped row per purchase, so renewals append history rather than overwrite it.
2. **Days remaining is computed, never stored.** It is derived from `EndDate` at read time. Nothing has to run for the number to stay correct — which is what makes deferring the background job harmless.
3. **Activation is immediate.** No payment gateway, no invoice, no pending state. Selecting a plan writes an Active subscription.
4. **Existing clinics stay locked.** The migration seeds no subscriptions for existing clinics. Every clinic that exists today lands on the plan-picker on next login and starts working after choosing a plan. This is the confirmed behaviour, not an oversight.
5. **Guard is middleware, not an attribute.** Inserted between `ClinicContextMiddleware` and `UseAuthorization()` — an addition to the pipeline, not a reorder. An authorization requirement would only guard endpoints someone remembered to decorate; the middleware closes the whole surface by default.
6. **Expiry is evaluated by date, not by a stored status flag.** Since no job runs to mark rows Expired, the guard must never depend on one having run. A subscription counts as active only when `Status == Active && EndDate >= UtcNow`.

---

## Entity: SubscriptionPlan

**Tenant-scoped:** No — global catalogue shared by all clinics
**Base class:** AuditableEntity
**Seeded:** Yes, via `HasData` with fixed GUIDs (same pattern as `MedicineDosageFormConfiguration`)

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, `ValueGeneratedNever` (fixed seed GUIDs) |
| Code | SubscriptionPlanCode | required, unique — Trial / Monthly / Quarterly / Yearly |
| Name | string | required, maxlength(60) |
| Description | string | maxlength(250) |
| DurationDays | int | required, > 0 |
| Price | decimal(18,2) | required, >= 0 |
| Currency | string | required, maxlength(3), default "INR" |
| IsTrial | bool | required, default false |
| IsPopular | bool | required, default false — drives the "Most Popular" ribbon |
| DisplayOrder | int | required — sort order on the plan picker |
| IsActive | bool | required, default true |

### Seeded rows

| Code | Name | DurationDays | IsTrial | IsPopular | DisplayOrder |
|------|------|-------------|---------|-----------|-------------|
| Trial | Free Trial | 14 | Yes | No | 1 |
| Monthly | Monthly | 30 | No | No | 2 |
| Quarterly | Quarterly | 90 | No | Yes | 3 |
| Yearly | Yearly | 365 | No | No | 4 |

Prices are seeded as placeholder values and are editable later through the admin portal; the execute step must use round placeholder amounts, not zero, so the UI renders meaningfully.

---

## Entity: ClinicSubscription

**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required — tenant key, indexed |
| SubscriptionPlanId | Guid | required, FK → SubscriptionPlan, Restrict delete |
| PlanCode | SubscriptionPlanCode | required — denormalised snapshot |
| PlanName | string | required, maxlength(60) — snapshot |
| PricePaid | decimal(18,2) | required — price at time of purchase |
| Currency | string | required, maxlength(3) |
| DurationDays | int | required — snapshot |
| StartDate | DateTime | required, UTC |
| EndDate | DateTime | required, UTC, indexed |
| Status | SubscriptionStatus | required — Active / Expired / Cancelled |
| CancelledAt | DateTime? | nullable, UTC |
| CancelledBy | string? | maxlength(256) |
| ClinicName | string | required, maxlength(200) — snapshot, for the future notification Function |
| BillingContactEmail | string | required, maxlength(256) — snapshot, for the future notification Function |
| BillingContactName | string | required, maxlength(200) — snapshot, for the future notification Function |

**Indexes:** composite `(ApplicationId, Status, EndDate)` — serves the guard's hot-path lookup on every request.

---

## Entity: SubscriptionAlertSetting

**Tenant-scoped:** No — global platform configuration, owned by the future super-admin portal
**Base class:** AuditableEntity
**Seeded:** Yes, four rows
**Consumer in this repo:** the admin read/update endpoints only. No code in this repo acts on these rows.

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, `ValueGeneratedNever` |
| AlertType | SubscriptionAlertType | required — ExpiryReminder / Expired |
| DaysBeforeExpiry | int | required, >= 0, unique together with AlertType |
| Subject | string | required, maxlength(200) — supports `{ClinicName}` and `{DaysRemaining}` tokens |
| Headline | string | required, maxlength(200) |
| BodyMessage | string | required, maxlength(1000) — supports the same tokens |
| IsEnabled | bool | required, default true |
| DisplayOrder | int | required |

### Seeded rows

| AlertType | DaysBeforeExpiry | Meaning |
|-----------|-----------------|---------|
| ExpiryReminder | 10 | First nudge |
| ExpiryReminder | 5 | Second nudge |
| ExpiryReminder | 2 | Final warning |
| Expired | 0 | Sent the day the subscription lapses |

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Enums/SubscriptionPlanCode.cs` | Trial, Monthly, Quarterly, Yearly |
| `Enums/SubscriptionStatus.cs` | Active, Expired, Cancelled |
| `Enums/SubscriptionAlertType.cs` | ExpiryReminder, Expired |
| `Dtos/Subscriptions/SubscriptionPlanDto.cs` | Catalogue entry for the plan picker — name, description, duration, price, currency, popular/trial flags, display order |
| `Dtos/Subscriptions/SubscriptionStatusDto.cs` | The gate's answer for one clinic — HasActiveSubscription, PlanName, StartDate, EndDate, DaysRemaining, IsExpiringSoon, CanSubscribe (true only for Clinic Admin), HasUsedTrial |
| `Dtos/Subscriptions/ClinicSubscriptionDto.cs` | Full detail of one subscription record |
| `Dtos/Subscriptions/SubscriptionHistoryItemDto.cs` | Lean row for the history list — plan name, period, price, status |
| `Dtos/Subscriptions/SubscribeRequest.cs` | POST body — SubscriptionPlanId only; dates and price are derived server-side and never trusted from the client |
| `Dtos/Subscriptions/SubscriptionAlertSettingDto.cs` | Read model of one reminder rule for the admin portal |
| `Dtos/Subscriptions/UpdateSubscriptionAlertSettingRequest.cs` | PUT body — subject, headline, body message, enabled flag, display order |
| `Errors/SubscriptionErrorCodes.cs` | Stable string codes the client keys off: `subscription_required`, `trial_already_used`, `subscription_already_active` |

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Entities/SubscriptionPlan.cs` | Catalogue entity, with a `CreateForSeed` factory mirroring `MedicineDosageForm` |
| `Entities/ClinicSubscription.cs` | Per-clinic purchase record with snapshot fields |
| `Entities/SubscriptionAlertSetting.cs` | Reminder threshold configuration row, with a `CreateForSeed` factory |
| `Interfaces/ISubscriptionPlanRepository.cs` | GetActivePlansAsync, GetByIdAsync, GetByCodeAsync |
| `Interfaces/IClinicSubscriptionRepository.cs` | GetActiveForClinicAsync, GetLatestForClinicAsync, GetHistoryForClinicAsync, HasUsedTrialAsync |
| `Interfaces/ISubscriptionAlertSettingRepository.cs` | GetAllAsync, GetByIdAsync |

> Every method on `IClinicSubscriptionRepository` is tenant-filtered. The cross-tenant sweep methods from the earlier draft are gone with the worker — if the Azure Function needs that access it will read the database directly, on its own terms.

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Persistence/Configurations/Subscriptions/SubscriptionPlanConfiguration.cs` | Fluent config + `HasData` for the four plans with fixed GUIDs |
| `Persistence/Configurations/Subscriptions/ClinicSubscriptionConfiguration.cs` | Fluent config, decimal precision, composite index, restrict-delete FK to plan |
| `Persistence/Configurations/Subscriptions/SubscriptionAlertSettingConfiguration.cs` | Fluent config + `HasData` for the 10 / 5 / 2 / 0 day rules |
| `Repositories/SubscriptionPlanRepository.cs` | Global lookup reads, no tenant filter (catalogue is shared by design) |
| `Repositories/ClinicSubscriptionRepository.cs` | Tenant-filtered reads only |
| `Repositories/SubscriptionAlertSettingRepository.cs` | Global config reads and updates |

### Application Layer (`src/TenantCore.Application/Features/Subscriptions/`)

| File | Purpose |
|------|---------|
| `Commands/SubscribeToPlanCommand.cs` | Carries plan id, ApplicationId, and the acting user's name/email/clinic name for the snapshot |
| `Commands/CancelSubscriptionCommand.cs` | Carries subscription id, ApplicationId, acting user |
| `Commands/UpdateSubscriptionAlertSettingCommand.cs` | Super-admin edit of one reminder rule |
| `Queries/GetSubscriptionPlansQuery.cs` | Catalogue list; carries ApplicationId so the handler can mark the Trial as already-used |
| `Queries/GetSubscriptionStatusQuery.cs` | The gate query — carries ApplicationId and whether the caller is Clinic Admin |
| `Queries/GetSubscriptionHistoryQuery.cs` | Past subscriptions for one clinic |
| `Queries/GetSubscriptionAlertSettingsQuery.cs` | All reminder rules for the admin portal |
| `Handlers/SubscribeToPlanHandler.cs` | Validates the plan, enforces the trial and overlap rules, computes the date window, persists |
| `Handlers/CancelSubscriptionHandler.cs` | Marks Cancelled, stamps who and when |
| `Handlers/GetSubscriptionPlansHandler.cs` | Returns ordered active plans with the trial-used flag applied |
| `Handlers/GetSubscriptionStatusHandler.cs` | Computes days remaining and the expiring-soon flag from EndDate |
| `Handlers/GetSubscriptionHistoryHandler.cs` | Returns mapped history rows, newest first |
| `Handlers/GetSubscriptionAlertSettingsHandler.cs` | Returns mapped reminder rules |
| `Handlers/UpdateSubscriptionAlertSettingHandler.cs` | Updates one rule, throws when missing |
| `Validators/SubscribeToPlanCommandValidator.cs` | FluentValidation rules for subscribe |
| `Validators/UpdateSubscriptionAlertSettingCommandValidator.cs` | FluentValidation rules for the admin edit |
| `Translators/SubscriptionTranslator.cs` | ToEntity, ToDto, ToStatusDto, ToHistoryDto, ToPlanDto |
| `Translators/SubscriptionAlertSettingTranslator.cs` | ToDto, ApplyUpdate |

### API Layer (`src/TenantCore.Api/`)

| File | Purpose |
|------|---------|
| `Controllers/SubscriptionsController.cs` | Inherits `ClinicControllerBase` — plans, status, history, subscribe, cancel |
| `Controllers/SubscriptionAlertSettingsController.cs` | Platform-level reminder configuration; SystemAdmin only; does **not** inherit `ClinicControllerBase` because it is not clinic-scoped |
| `Middleware/SubscriptionGuardMiddleware.cs` | Blocks every clinic-scoped request when the current clinic has no active subscription |

### Web Client (`src/TenantCore.Web.Client/`)

| File | Purpose |
|------|---------|
| `Clients/SubscriptionClient.cs` | Typed client for the five subscription endpoints |
| `Services/SubscriptionContextService.cs` | Caches the current clinic's `SubscriptionStatusDto`, exposes `IsLocked` / `DaysRemaining`, raises `OnSubscriptionChanged`, and invalidates on clinic switch |
| `Pages/Subscription/SubscriptionPlans.razor` | The plan picker — markup |
| `Pages/Subscription/SubscriptionPlans.razor.cs` | Plan picker code-behind |
| `Pages/Subscription/ClinicLocked.razor` | Non-admin blocked screen |
| `Components/Subscription/SubscriptionBanner.razor` | Dashboard CTA / status strip |
| `Components/Subscription/SubscriptionStatusPill.razor` | Compact days-remaining chip for the top bar |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add 3 DbSets — `SubscriptionPlans`, `ClinicSubscriptions`, `SubscriptionAlertSettings` (total becomes 33) |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register 3 repositories as Scoped |
| `src/TenantCore.Api/Program.cs` | Insert `SubscriptionGuardMiddleware` after `ClinicContextMiddleware` and before `UseAuthorization()` |
| `src/TenantCore.Api/appsettings.json` | Add `Subscription:GuardEnabled` and `Subscription:ExpiringSoonDays` |
| `src/TenantCore.Web.Client/Program.cs` | Register `SubscriptionClient` and `SubscriptionContextService` |
| `src/TenantCore.Web.Client/Layout/AuthorizedLayout.razor` | Load subscription status after clinic context; render the gate instead of `@Body` when locked; show `SubscriptionStatusPill` in the top bar |
| `src/TenantCore.Web.Client/Pages/Dashboard.razor` | Add `SubscriptionBanner` at the top |
| `src/TenantCore.Web.Client/Pages/Admin/AdminDashboard.razor` | Add `SubscriptionBanner` at the top |
| `src/TenantCore.Web.Client/Pages/Doctor/ClinicLanding.razor` | Show a lock badge and muted styling on clinic cards with no active subscription; still allow entry so the Clinic Admin can reach the picker |
| `src/TenantCore.Web.Client/Layout/NavMenu.razor` | Add a "Subscription" item under the admin section |
| `.claude/context/current-state.md` | Update DbSet table and DI table after execution |
| `plan/REGISTRY.md` | Append the executed-feature row and domain coverage entries |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| GET | `api/subscriptions/plans` | — | `IEnumerable<SubscriptionPlanDto>` | RequireAuthenticated |
| GET | `api/subscriptions/status` | — | `SubscriptionStatusDto` | RequireAuthenticated |
| GET | `api/subscriptions/history` | — | `IEnumerable<SubscriptionHistoryItemDto>` | RequireClinicAdmin |
| POST | `api/subscriptions/subscribe` | `SubscribeRequest` | `ClinicSubscriptionDto` (201) | RequireClinicAdmin |
| POST | `api/subscriptions/{id}/cancel` | — | 204 No Content | RequireClinicAdmin |
| GET | `api/subscription-alert-settings` | — | `IEnumerable<SubscriptionAlertSettingDto>` | SystemAdmin role |
| PUT | `api/subscription-alert-settings/{id}` | `UpdateSubscriptionAlertSettingRequest` | `SubscriptionAlertSettingDto` | SystemAdmin role |

The first four clinic routes plus `/health`, `/api/auth/*` and `/api/clinic/dashboard` sit on the guard's exempt list — otherwise a locked clinic could never reach the screen that unlocks it.

---

## The Access Gate

### Server side — `SubscriptionGuardMiddleware`

Runs after `ClinicContextMiddleware` (so `HttpContext.Items["ApplicationId"]` is populated and validated) and before `UseAuthorization()`.

| Condition | Behaviour |
|-----------|-----------|
| Request is unauthenticated | Pass through — authentication handles it |
| No `ApplicationId` in `HttpContext.Items` | Pass through — not a clinic-scoped request |
| Path matches the exempt list | Pass through |
| Clinic has an Active subscription with `EndDate` in the future | Pass through |
| Otherwise | Short-circuit with **402 Payment Required** and a ProblemDetails body carrying `subscription_required` |

The lookup is a single indexed read per request. It resolves a scoped `IClinicSubscriptionRepository` from `context.RequestServices`; the middleware itself holds no business logic beyond the pass/block decision, per ADR-005.

`Subscription:GuardEnabled` allows the whole guard to be switched off from configuration — needed for local development and for a controlled rollout.

### Client side — `AuthorizedLayout`

After `ClinicContext.InitializeAsync()` succeeds, the layout asks `SubscriptionContextService` for status. When the clinic is locked:

- **Clinic Admin** → the layout renders `SubscriptionPlans` in place of `@Body`. The sidebar stays visible but every nav item except Subscription and Sign Out is disabled, so there is no way to click into a locked area and collect a 402.
- **Every other role** → the layout renders `ClinicLocked`, which explains that the clinic has no active subscription and names the action to take (contact the Clinic Admin). No clinic data is requested at all.

The 402 response is also handled centrally in `ClinicAuthorizationHandler` as a safety net: any 402 forces a status refresh and re-renders the gate, which covers the case of a subscription lapsing mid-session.

---

## Subscription Page UI/UX

Built on the `opd-*` / `cc-*` custom CSS theme with native HTML elements, per ADR-007. No MudBlazor.

### Plan picker (`/subscription`)

- Header block: "Activate your clinic" with a one-line subtitle naming the clinic. When the clinic is locked, a red-tinted notice strip sits above it explaining that features stay disabled until a plan is chosen.
- Four plan cards in a responsive grid, ordered by `DisplayOrder`. Each card carries the plan name, price with currency, a "per day" derived value, duration in days, and a select button.
- The Quarterly card is visually lifted — coloured border, subtle shadow, and a "MOST POPULAR" ribbon — driven by `IsPopular`, not hardcoded.
- The Trial card shows a "FREE · once per clinic" chip. When `HasUsedTrial` is true the card is greyed, the button disabled, and the chip reads "Already used".
- Selecting a plan opens the standard fixed-position confirm modal from ADR-007, restating plan, price and the exact start and end dates before committing.
- On success: a toast, `SubscriptionContextService` refreshes, and the app navigates to the dashboard — the gate lifts in place with no reload.

### Dashboard banner (`SubscriptionBanner`)

| State | Appearance |
|-------|-----------|
| No subscription, Clinic Admin | Full-width gradient banner (`#1565C0` → `#42A5F5`), white text, "⚡ Subscribe to unlock all features" with a white pill button reading "Choose a plan" |
| No subscription, other roles | Neutral `#F1F5F9` strip — "This clinic has no active subscription. Contact your Clinic Admin." No button |
| Active, > 15 days | Slim green-tinted strip — plan name and `284 days remaining` |
| Active, 6–15 days | Amber strip (`#FEF3C7` / `#92400E`) — "Expires in 12 days" with a "Renew now" link for Clinic Admin |
| Active, ≤ 5 days | Red strip (`#FEE2E2` / `#991B1B`) — "Expires in 3 days" with a prominent "Renew now" button |

Colours are taken from the standard status palette in ADR-007 rather than invented.

### Status pill (`SubscriptionStatusPill`)

A compact chip in the `AuthorizedLayout` top bar next to the clinic name, showing the remaining-days count in the same three-tier colour scheme. Visible on every page, so the countdown is never more than a glance away.

With reminder emails deferred, this in-app countdown is the **only** expiry warning a clinic gets. That raises the stakes on the banner and pill actually being visible — they are the whole notification story until the Function ships.

---

## Validation Rules

### SubscribeToPlanCommandValidator

| Field | Rules |
|-------|-------|
| SubscriptionPlanId | NotEmpty |
| ApplicationId | NotEmpty |
| BillingContactEmail | NotEmpty, valid email format, MaxLength(256) |
| BillingContactName | NotEmpty, MaxLength(200) |
| ClinicName | NotEmpty, MaxLength(200) |

### UpdateSubscriptionAlertSettingCommandValidator

| Field | Rules |
|-------|-------|
| Id | NotEmpty |
| Subject | NotEmpty, MaxLength(200) |
| Headline | NotEmpty, MaxLength(200) |
| BodyMessage | NotEmpty, MaxLength(1000) |
| DisplayOrder | GreaterThanOrEqualTo(0) |

---

## Business Rules

Enforced in handlers, surfaced as domain exceptions that `ExceptionHandlingMiddleware` maps to HTTP status codes:

1. **The plan must exist and be active** — otherwise `EntityNotFoundException` (404).
2. **Trial is once per clinic, ever.** If the requested plan has `IsTrial` and the clinic has any prior `ClinicSubscription` with `IsTrial` — of any status, including Cancelled and Expired — throw `InvalidOperationException` (409) with `trial_already_used`. Cancelling a trial does not restore the entitlement; this is the accurate rule, not a soft check.
3. **Renewal before expiry does not truncate the current term.** If an Active subscription exists with `EndDate` in the future, the new subscription's `StartDate` is the day after that `EndDate` and its `EndDate` is `StartDate + DurationDays`. The existing subscription runs to completion; the clinic keeps every day it paid for.
4. **Only one Active subscription may cover a given moment.** The overlap rule in (3) guarantees this by construction; the handler additionally rejects a second subscribe attempt for the same window.
5. **A cancelled subscription cannot be re-cancelled** — `InvalidOperationException` (409).
6. **Cancellation does not refund or shorten the term.** Status becomes Cancelled, `EndDate` is untouched, and the clinic retains access until that date. Only the absence of a *future* renewal follows from a cancellation.
7. **Expiry is evaluated by date, not by a stored flag.** A subscription is treated as active when `Status == Active && EndDate >= UtcNow`. No process in this repo ever flips `Status` to Expired, so nothing may depend on that having happened.
8. **Dates and price are always server-derived.** `StartDate`, `EndDate`, `DurationDays` and `PricePaid` come from the plan row, never from the request body.

---

## Multi-Tenancy Checklist

- [ ] `ApplicationId` present on `ClinicSubscription`
- [ ] `SubscriptionPlan` and `SubscriptionAlertSetting` are deliberately global — documented in their XML docs so a later reviewer does not "fix" them
- [ ] `ApplicationId` passed in all clinic-scoped commands and queries
- [ ] Every `IClinicSubscriptionRepository` query filters by `applicationId` — no exceptions in this plan
- [ ] `SubscriptionsController` uses `GetApplicationId()` from `ClinicControllerBase`
- [ ] `SubscriptionAlertSettingsController` is platform-scoped and does not inherit `ClinicControllerBase`
- [ ] Blazor client sends `X-Application-Id` via the existing handler — no change needed

---

## DateTime Handling

Per ADR-007: `StartDate`, `EndDate` and all timestamps are stored UTC and stamped with `DateTimeKind.Utc` in the translator. Every display of a date or a days-remaining count goes through `DateTimeHelper`. `DaysRemaining` is computed as whole days between `DateTime.UtcNow.Date` and `EndDate.Date`, so a subscription expiring later today reads as 0 rather than a fraction.

---

## EF Migration

**Migration name:** `AddSubscriptions`

Creates three tables and seeds `SubscriptionPlans` (4 rows) and `SubscriptionAlertSettings` (4 rows). No data is written for existing clinics — they lock until an admin picks a plan, as confirmed.

```
dotnet ef migrations add AddSubscriptions --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

---

## Implementation Order

1. Shared enums and error codes
2. Shared DTOs
3. Domain entities (3)
4. Domain repository interfaces (3)
5. Infrastructure EF configurations (3, two with seed data)
6. Infrastructure repositories (3)
7. Modify `ClinicDbContext` — add 3 DbSets
8. Modify Infrastructure `DependencyInjection` — register repositories
9. Application commands and queries
10. Application validators
11. Application translators
12. Application handlers
13. API controllers (2)
14. API `SubscriptionGuardMiddleware`
15. Modify `Program.cs` (middleware insert) and `appsettings.json`
16. Web client — `SubscriptionClient`, `SubscriptionContextService`, DI registration
17. Web client — `SubscriptionPlans`, `ClinicLocked`, `SubscriptionBanner`, `SubscriptionStatusPill`
18. Modify `AuthorizedLayout`, `Dashboard`, `AdminDashboard`, `ClinicLanding`, `NavMenu`
19. Unit tests — all handler, validator and translator test files
20. Run the EF migration
21. Update `.claude/context/current-state.md` and `plan/REGISTRY.md`

---

## Test Files to Create

All under `tests/TenantCore.Application.Tests/Features/Subscriptions/`.

| File | What it covers |
|------|---------------|
| `Commands/SubscribeToPlanHandlerTests.cs` | Happy path (AddAsync + SaveChangesAsync called, dates derived from plan duration); `EntityNotFoundException` for a missing or inactive plan; `InvalidOperationException` when the trial was already used, including when the prior trial was Cancelled; renewal before expiry starts the day after the current EndDate and leaves the existing row untouched; price and dates taken from the plan and not from the request; billing contact snapshot persisted |
| `Commands/CancelSubscriptionHandlerTests.cs` | Status set to Cancelled with CancelledAt/CancelledBy stamped; EndDate unchanged; `EntityNotFoundException` when missing; cross-tenant subscription treated as not found; `InvalidOperationException` on re-cancel |
| `Commands/UpdateSubscriptionAlertSettingHandlerTests.cs` | Fields updated and saved; `EntityNotFoundException` when the rule is missing |
| `Queries/GetSubscriptionStatusHandlerTests.cs` | DaysRemaining computed correctly across boundaries (expiring today → 0, tomorrow → 1); HasActiveSubscription false when expired by date even while Status is still Active; IsExpiringSoon flips at the configured threshold; CanSubscribe true only for Clinic Admin; empty status when the clinic has never subscribed |
| `Queries/GetSubscriptionPlansHandlerTests.cs` | Four plans returned in DisplayOrder; inactive plans excluded; trial marked used when a prior trial exists |
| `Queries/GetSubscriptionHistoryHandlerTests.cs` | History rows newest first; empty list for a clinic with no history; cross-tenant rows excluded |
| `Queries/GetSubscriptionAlertSettingsHandlerTests.cs` | Rules returned in DisplayOrder including disabled ones |
| `Validators/SubscribeToPlanCommandValidatorTests.cs` | Valid command passes; each required field fails when null or empty (`[Theory]`); malformed email fails; MaxLength boundaries at and over the limit; empty ApplicationId fails |
| `Validators/UpdateSubscriptionAlertSettingCommandValidatorTests.cs` | Same boundary coverage for subject, headline, body message and display order |
| `Translators/SubscriptionTranslatorTests.cs` | ToEntity sets a non-empty Id, ApplicationId, all snapshot fields, and stamps `DateTimeKind.Utc`; ToDto and ToStatusDto map every field; ToHistoryDto maps the display fields |

---

## Open Questions / Risks

- **Every existing clinic locks on deploy.** This is the confirmed intent, but it is a hard cutover: every Clinic Admin must pick a plan before their staff can work. Worth timing the release accordingly and warning admins in advance.
- **No expiry warning leaves the app until the Function ships.** A clinic that stops logging in gets no notice at all and simply finds itself locked out on the expiry date. The in-app banner and pill only reach someone who is already using the product. This is the accepted cost of deferring the emails, and it is the strongest argument for building the Function soon after.
- **Nothing marks lapsed subscriptions Expired.** Rows keep `Status = Active` past their `EndDate` forever. Every read path in this plan compares dates, so behaviour is correct — but any future reporting that trusts the `Status` column alone will be wrong. The Azure Function is the natural place to do that housekeeping.
- **`SubscriptionAlertSetting` ships with no consumer in this repo.** It is configuration for a component that does not exist yet. If the Function ends up owning its own schema, this table becomes dead weight and should be dropped rather than left to drift.
- **Non-admin users of a clinic whose admin is absent are fully blocked.** There is no self-service path and no break-glass override beyond `Subscription:GuardEnabled`, which is global rather than per-clinic. If a per-clinic override is wanted later it belongs in the admin portal.
- **Trial abuse across clinics.** The once-per-clinic rule is scoped to `ApplicationId`. A doctor who registers a second clinic gets a second trial. Closing that would require a user-level or owner-level check against TenantCore.Auth and is out of scope here.
- **Cross-repo:** TenantCore.Admin will later read these tables from `TenantClinicDb`, which it already accesses read-only. No Admin repo work is in this plan, as confirmed.
