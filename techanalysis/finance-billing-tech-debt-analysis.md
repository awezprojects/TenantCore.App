# Finance & Clinic Billing — Tech Debt & Gap Analysis

**Repo:** TenantCore.App
**Date:** 2026-11-07
**Trigger:** Admin/doctor finance dashboards reported wrong — doctors can see other doctors' collections, "My Finance" shows more than it should, and the per-doctor "Fee Configs" screen appears to have no real effect.
**Method:** Static code review of the finance/billing code paths — `FinanceReportsController`, `OpdPaymentsController`, `FinanceDashboard.razor`, `Dashboard.razor`, `NavMenu.razor`, `ClinicContextService`, the OPD payment/particular domain model, and the doctor-fee-config slice. No runtime/DB access was used; line references are from the current working tree.

---

## 1. Executive Summary

The underlying **data model is sound**: `OpdPayment` + `OpdParticular` already track visit fees and itemized extras (injections, procedures, etc.) per OPD registration, and the doctor-wise / reception-wise / dashboard-summary queries already do the hard part correctly (day-attribution of multi-session collections, per-doctor and per-receptionist grouping, tenant scoping). The problems are almost entirely in the **authorization layer and the Blazor presentation layer wired on top of that model**, plus one real **billing-configuration split-brain**. Concretely:

1. **Every finance-reporting API endpoint is reachable by any authenticated clinic user** (Doctor, Nurse, Pharmacist, Lab Tech) — there is no role check beyond "has a valid token." A doctor (or anyone) can call `/api/finance-reports/doctor-wise?doctorUserId=<any-other-doctor>` directly and get that doctor's earnings. **This is the literal bug reported** — it's not a UI glitch, it's a missing server-side authorization check.
2. **The "My Finance" page's doctor/admin decision is based on the user's raw role claims, not on which panel they are currently using.** A user who holds both `Doctor` and `Clinic Admin` roles for a clinic and explicitly chooses "Enter as Doctor" is still treated as admin by the finance page, so they see every doctor's collection, not just their own — exactly the scenario described.
3. **Even when the doctor filter *is* applied correctly**, it only narrows one of the five sections on the page. The clinic-wide income total, expense breakdown, counter-session cash log, and staff handover log are shown to every doctor unfiltered, regardless of role. A plain doctor with no admin role at all still sees the whole clinic's P&L today.
4. **There are two, non-communicating sources for "what is the doctor's fee":** a per-doctor `DoctorFeeConfig` (exposed in the UI as "Fee Configs") and a single clinic-wide default fee, plus a free-form fee override reception can type per registration. `DoctorFeeConfig` is **never read** by the OPD registration flow — the admin screen that promises "set each doctor's fee" has no effect on what patients are actually charged.
5. Three of the eight finance-report endpoints/queries that exist in code (`dashboard` summary, daily/weekly/monthly breakdowns, reception-wise collection) are **never called from any page** — dead backend functionality, including the one report (reception-wise) that would directly answer "how much did each receptionist collect," which is relevant to the user's billing-integrity concern.

None of this requires new domain modeling — it's an authorization fix, a dashboard-composition fix, a dead-code/wiring decision, and a one-line removal of a redundant config screen (or a decision to actually wire it up).

---

## 2. How Finance & Billing Currently Work

```
Reception registers OPD visit (CreateOpdRegistrationCommand)
  └─ Fee = request.Fee (ad-hoc, reception-typed, 0–100,000)
         ?? ClinicFeeConfig.OpdFee   (ONE clinic-wide default)
         (DoctorFeeConfig is never consulted)
  └─ OpdPayment.Create(fee)  → VisitFee, Pending
  └─ if PrepaidOpdEnabled: OpdPayment.AcceptVisitFee() → CollectedAmount = VisitFee, tied to active CounterSession

During the visit, doctor/reception adds extra billable items (injection, dressing, procedure, …)
  └─ OpdParticular.Create(particularId, name, amount)  — one row per item, own PaymentStatus
  └─ Collected individually (Collect()) or folded into a discounted full-bill collection (AcceptFull() → MarkCollectedViaPayment())

Reporting reads these two tables back out:
  GetDoctorWiseOpdCollectionHandler   → per-doctor total = Σ(payment.CollectedAmount − item portion) + Σ(items collected in range)
  GetReceptionWiseReportHandler       → per-receptionist total, grouped by CounterSession.OpenedByUserId
  GetFinanceDashboardSummaryHandler   → today's total collected + expenses + active session
  GetExpenseSummaryReportHandler      → ExpenseRecord totals by category
  GetDateRangeCollectionReportHandler → clinic-wide day-by-day total (used as the page's "OPD Income" KPI)
```

This pipeline already correctly answers the user's "appointment fee *and* other indications like injections should count toward collection" requirement — `OpdParticular` amounts are folded into every one of the above totals. **That part is not a gap.**

### Dashboards on top of it

- `Dashboard.razor` (`/dashboard`) — role-aware landing page. Shows the doctor their *own* OPD queue/prescriptions (correctly scoped by `doctorUserId = AuthState.CurrentUser.UserId`), or reception's queue. Redirects pure admins (`Clinic Admin`/`System Admin` with no `Doctor` role) to `/admin/dashboard`.
- `FinanceDashboard.razor` (`/finance-dashboard`) — one shared page, linked from the nav as **"Finance"** in the Admin panel and **"My Finance"** in the Doctor panel. Shows OPD income, doctor-wise collection, expense breakdown, counter sessions, and amount handovers for a date range.
- `NavMenu.razor` decides which panel ("Admin" / "Doctor" / default) to render based on `ClinicContextService.ActivePanel` — a **client-side, per-session choice** the user makes via `ClinicLanding.razor`'s "Enter as Doctor" / "Enter as Admin" buttons when they hold more than one role at a clinic, persisted in `localStorage`. It is independent of their actual role claims.

---

## 3. Findings

Each finding has severity, evidence, root cause, impact, and a concrete fix.

### B1 — Finance Reports API has no role restriction (Critical / Security — Broken Access Control)

**Evidence:** [`FinanceReportsController.cs:10-71`](../src/TenantCore.Api/Controllers/FinanceReportsController.cs) — the whole controller carries only `[Authorize(Policy = AuthPolicies.RequireAuthenticated)]`; none of the 8 actions (`dashboard`, `daily`, `weekly`, `monthly`, `date-range`, `reception-wise`, `expenses`, `doctor-wise`) has a per-action policy override. Compare to every sibling controller in this codebase — `CounterSessionsController`, `AmountHandoversController`, `ExpenseRecordsController`, `DoctorFeeConfigsController`, `OpdRegistrationsController` — which all layer `RequireReception`/`RequireClinical`/`RequireClinicAdmin`/`RequireClinicStaff` on top of the base policy per action.

**Root cause:** the finance-reports feature was gated only in the UI (behind `FeatureFlagsContext.IsBillingEnabled` and the nav panel), and the corresponding server-side policy was never added. `AuthPolicies.RequireAuthenticated` means literally "any role for this clinic."

**Impact:** Any authenticated clinic user — Nurse, Pharmacist, Lab Technician, Receptionist, or Doctor — can call any of these 8 endpoints directly (devtools, Postman, a saved bearer token) and get:
- The clinic's total income/expenses for any date range (`/date-range`, `/expenses`, `/dashboard`, `/daily`, `/weekly`, `/monthly`).
- Any individual doctor's earnings by passing their `doctorUserId` (`/doctor-wise?doctorUserId=<any guid>`), or every doctor's earnings at once by omitting it.
- Per-receptionist cash-collection totals (`/reception-wise`).

This is the server-side root cause of "doctor can see other doctors' reports" — the UI bug in B2/B3 is what makes it *visible by default*, but the API would allow it even if the UI were perfect.

**Fix:**
- Add `[Authorize(Policy = AuthPolicies.RequireClinicStaff)]` at the controller level (excludes Nurse/Pharmacist/Lab Tech, who have no legitimate reason to see money data) **and** tighten per action:
  - `expenses`, `reception-wise`: `RequireReception` (Receptionist/ClinicAdmin/ClinicManager/SystemAdmin only — a doctor has no reason to see clinic expenses or who-collected-what-cash).
  - `dashboard`, `daily`, `weekly`, `monthly`, `date-range`: `RequireReception` (these are clinic-wide totals; a plain doctor should never receive them — see B3).
  - `doctor-wise`: keep `RequireClinicStaff` (a doctor does need this one), but add server-side self-scoping — see B2's fix below.

### B2 — "My Finance" ignores the Doctor/Admin panel choice; uses raw role claims instead (Critical — this is the reported bug)

**Evidence:** [`FinanceDashboard.razor:378-381`](../src/TenantCore.Web.Client/Pages/Admin/FinanceDashboard.razor#L378-L381):
```csharp
private bool _isAdmin  => AuthState.GetRolesForApplication(ClinicContext.SelectedApplicationId)
    .Any(r => string.Equals(r, AppRoles.ClinicAdmin, ...));
private bool _isDoctor => AuthState.GetRolesForApplication(ClinicContext.SelectedApplicationId)
    .Any(r => string.Equals(r, AppRoles.Doctor, ...));
```
and [`:436-439`](../src/TenantCore.Web.Client/Pages/Admin/FinanceDashboard.razor#L436-L439):
```csharp
// Doctor-only: auto-filter to current user; admin: see all doctors
Guid? doctorFilter = _isDoctor && !_isAdmin
    ? AuthState.CurrentUser?.UserId
    : null;
```
Contrast with [`ClinicLanding.razor:429-434`](../src/TenantCore.Web.Client/Pages/Doctor/ClinicLanding.razor#L429-L434), where "Enter as Doctor" explicitly sets `ClinicContext.ActivePanel = "Doctor"` — a choice the finance page never looks at.

**Root cause:** `_isAdmin`/`_isDoctor` are computed from the user's full role set for the clinic, not from `ClinicContext.ActivePanel` (the thing that actually decided which nav/layout — and which "My Finance" vs "Finance" label — the user is looking at). This is precisely the scenario the user described: *"doctor can see the same when they log in even if doctor has the admin role, but when they log in to the doctor dashboard they should see only their related items."*

**Impact:** Any user who holds `Clinic Admin` (or `System Admin`) **and** `Doctor` on the same clinic — a very normal setup, since a doctor who self-onboards their own clinic becomes its Clinic Admin (see the comment on `ClinicControllerBase.GetCurrentUserRoles()`) — gets `_isAdmin = true` unconditionally, so `doctorFilter` is always `null`. They see every doctor's collection on the one table that is supposed to be scoped, no matter which panel they entered through.

**Fix:** Base the filter on `ClinicContext.ActivePanel`, not on raw role membership:
```csharp
Guid? doctorFilter = ClinicContext.ActivePanel == "Doctor"
    ? AuthState.CurrentUser?.UserId
    : null;
```
This must be paired with B1's server-side fix — the client choosing not to pass `doctorUserId` must not be the *only* thing stopping a dual-role user's browser session from requesting other doctors' data; the server must also refuse to honor a `doctorUserId` the caller isn't entitled to when they're acting in a non-admin capacity. Since the API has no concept of "active panel" (that's a client-only construct), the server-side rule has to be role-based: if the caller holds none of `ClinicAdmin`/`ClinicManager`/`SystemAdmin` for the clinic, force `doctorUserId = GetCurrentUserId()` regardless of what was requested. A user who legitimately holds admin can still see everyone — by design — but only while using the admin capability, and the *client* now respects the panel they chose on top of that.

### B3 — Doctor view of "My Finance" leaks clinic-wide income, expenses, counter-cash and handover data (High)

**Evidence:** [`FinanceDashboard.razor:441-453`](../src/TenantCore.Web.Client/Pages/Admin/FinanceDashboard.razor#L441-L453) — `doctorFilter` is only ever passed into `ReportClient.GetDoctorWiseAsync(...)`. The other four parallel calls — `GetDateRangeAsync` (drives the "OPD Income" KPI card and the day-wise table), `GetExpenseSummaryAsync`, `SessionClient.GetAllAsync` (counter sessions), `HandoverClient.GetAllAsync` (staff cash handovers) — take no doctor/user filter at all and are not clinic-role-gated either. Every section except "Doctor Wise OPD Collection" renders full clinic data on this page unconditionally.

**Root cause:** the page was built as one admin screen and then reused for doctors by adding a filter to a single section, without re-deciding which sections a doctor should even be allowed to see.

**Impact:** Even a doctor with *no* admin role, correctly landing on "My Finance" with `doctorFilter` set to themselves, still sees:
- the clinic's total OPD income across all doctors ("OPD Income" KPI + day-wise breakdown),
- the full expense breakdown (clinic overhead, categories, amounts),
- every counter session opened by front-desk staff and its cash totals,
- every staff-to-staff cash handover and its status.

None of that is "their related items." This is a second, independent instance of the same underlying symptom the user reported, and it affects *every* doctor, not just dual-role ones.

**Fix:** Decide what a doctor-only view should actually contain (recommend: their own appointment-fee + particulars collection total, broken down by day/date-range, and nothing else — no expenses, no counter sessions, no handovers, no clinic-wide total), and either:
- render a genuinely separate, smaller component for the Doctor panel (`MyFinance.razor`) that only calls `doctor-wise`, or
- keep one page but conditionally render sections based on `ClinicContext.ActivePanel == "Admin"`.

Either way, pair with B1: the endpoints behind the hidden sections (`expenses`, counter sessions, handovers) should also reject a plain-Doctor caller server-side, so this isn't just a rendering decision.

### B4 — Two competing, disconnected fee configurations; the admin-facing one is dead (High — Billing Correctness)

**Evidence:**
- `DoctorFeeConfig` entity (`src/TenantCore.Domain/Entities/DoctorFeeConfig.cs`) — per-doctor `VisitFee`, CRUD exposed at `/admin/doctor-fee-configs` ("Fee Configs" in the nav), via `DoctorFeeConfigsController` + handlers in `Features/DoctorFeeConfigs/`.
- `CreateOpdRegistrationHandler.cs:70-75`:
  ```csharp
  var fee = request.Fee;
  if (fee is null)
  {
      var config = await feeRepository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
      fee = config?.OpdFee ?? 0m;
  }
  ```
  `feeRepository` is `IClinicFeeConfigRepository` — a **single, clinic-wide** default fee (`ClinicFeeConfig.OpdFee`), completely separate from `DoctorFeeConfig`. `DoctorFeeConfig` does not appear anywhere in this handler, nor anywhere in `TenantCore.Application.Features.OpdRegistrations` or `FinanceReports` — a repo-wide search shows it is referenced only inside its own CRUD slice and `DummyDataStore` (seed data). It has **zero behavioral effect**.
- `CreateOpdRegistrationCommandValidator.cs:10,17` — `request.Fee` (reception-typed, optional) only has a sanity bound (`0..100,000`), no check against any configured rate for that doctor or the clinic.

**Root cause:** `DoctorFeeConfig` looks like it was built for a "per-doctor rate card" feature that was never finished wiring into registration — the clinic-wide `ClinicFeeConfig.OpdFee` fallback plus a free-form reception override shipped instead, and the per-doctor screen was left in the admin nav.

**Impact:** An admin who goes to "Fee Configs" and sets Dr. A's visit fee to ₹500 and Dr. B's to ₹800 is configuring something that is **never read**. What actually gets charged is: whatever reception types in that day (if anything), else one clinic-wide number regardless of which doctor the patient is seeing. This directly explains the kind of billing inconsistency the user is worried about ("reception is taking the amount from patient for appointment fee and other fees") — there is no enforced, doctor-specific rate at all; it's effectively ad hoc per visit unless reception remembers to type the right number every time.

**Fix (needs a product decision, not just a code fix):**
- **Option A (recommended):** make `DoctorFeeConfig` real — `CreateOpdRegistrationHandler` should resolve `fee` as `request.Fee ?? DoctorFeeConfig(doctor)?.VisitFee ?? ClinicFeeConfig.OpdFee ?? 0`, and the free-form `request.Fee` override should require a reason/flag so it's auditable as a deliberate exception rather than the default path. Validate it against the configured rate (e.g., warn or require admin approval above/below a tolerance) if "correct billing" means rates should be enforced, not just suggested.
- **Option B:** if per-doctor rates are no longer a desired feature, remove `DoctorFeeConfig`, its controller, its nav item, and its migration-era data rather than leaving a screen that lies about what it does.

### B5 — Three finance-report endpoints are dead: no UI caller exists (Medium — Dead Code / Missed Capability)

**Evidence:** `IFinanceReportApiClient` declares `GetDashboardAsync`, `GetDailyAsync`, `GetWeeklyAsync`, `GetMonthlyAsync`, `GetReceptionWiseAsync` ([`IFinanceReportApiClient.cs`](../src/TenantCore.Web.Client/Clients/IFinanceReportApiClient.cs)); a repo-wide search of `*.razor`/`*.razor.cs` shows only `GetDateRangeAsync`, `GetExpenseSummaryAsync`, and `GetDoctorWiseAsync` are ever called (from `FinanceDashboard.razor`). That means:
- `GetFinanceDashboardSummaryQuery`/Handler (today's collected/expenses/active-session snapshot) — unused.
- Daily/Weekly/Monthly collection reports — unused (the page re-derives the same thing from `date-range` + client-side preset buttons instead).
- **`GetReceptionWiseReportHandler`** (per-receptionist/counter-staff collection totals) — unused. This is the one report that would let an admin verify "which front-desk staff collected how much," directly relevant to the user's concern about reception handling patient payments correctly.

**Impact:** Working, tenant-scoped backend logic with no way to reach it from the product — wasted effort, and a real admin need (auditing reception's cash handling per staff member) goes unmet despite the report already existing.

**Fix:** Either wire `reception-wise` into the Finance Dashboard (a "Collection by Staff" section — high value, directly answers the user's billing-integrity ask) and delete the genuinely redundant daily/weekly/monthly/summary endpoints in favor of the `date-range` one already in use, or confirm there's a separate page planned for them and track it; don't leave them as silent dead code either way.

### B6 — `GetReceptionWiseReportHandler` fetches every clinic's counter sessions, filters in memory (Medium — ADR-008 violation / scalability)

**Evidence:** [`GetReceptionWiseReportHandler.cs:20-25`](../src/TenantCore.Application/Features/FinanceReports/Handlers/GetReceptionWiseReportHandler.cs#L20-L25):
```csharp
var sessions = await counterSessionRepository.GetAllAsync(cancellationToken);
var filteredSessions = sessions.Where(s => s.ApplicationId == request.ApplicationId && ...).ToList();
```
`GetAllAsync` comes from the generic `IRepository<T>` base and is not tenant-scoped (`ICounterSessionRepository`'s own methods — `GetActiveSessionAsync`, `GetAllActiveAsync`, `GetByDateAsync` — all take `applicationId`, but this handler bypasses them and uses the unscoped base method instead).

**Root cause:** reached for the convenient generic method instead of adding a scoped query method, likely because this handler currently has no caller to notice the cost (see B5).

**Impact:** As the platform grows, every call to this report loads **every counter session for every clinic on the platform** into memory before filtering. No cross-tenant data is returned (the filter does happen), so this isn't a confidentiality leak, but it's a direct violation of this repo's own rule ("every repository query for tenant-scoped entities must filter by applicationId," ADR-008) and will get slower linearly with total platform session volume, not this clinic's volume.

**Fix:** Add a scoped `GetByDateRangeAsync(from, to, applicationId)` to `ICounterSessionRepository`/`CounterSessionRepository` and use it here instead of `GetAllAsync()`.

### B7 — Discount authority includes the Doctor role (Low/Medium — needs a product decision)

**Evidence:** `OpdPaymentsController.cs:43-44` — `POST /{id}/discount` is `[Authorize(Policy = AuthPolicies.RequireClinicStaff)]`, and `RequireClinicStaff` = `[Receptionist, Doctor, ClinicAdmin, ClinicManager, SystemAdmin]` (`AuthorizationConstants.cs:26-27`). Every other money-moving action on the same controller (`accept`, `accept-full`, `refund`) is `RequireReception` (no Doctor).

**Impact:** A doctor can apply a discount to a patient's bill (which can also trigger a refund-due calculation per `OpdPayment.ApplyDiscount`), while every other cash-handling action is reception-only. This may be intentional (a doctor waiving part of a fee for a patient), but it's inconsistent with the rest of the money-handling surface and worth a deliberate decision rather than leaving it as the one exception. If intentional, it should be logged with enough context to audit (who discounted, by how much, for which visit) — confirm via ADR-011.

**Fix:** Confirm intent with product; if doctors shouldn't independently discount patient bills, tighten to `RequireReception`. If they should, document why here and in ADR-010/011 so it isn't mistaken for a gap again.

### B8 — Payment lookup endpoints have no per-action policy (Low)

**Evidence:** `OpdPaymentsController.cs:20-26` — `GET /{opdRegistrationId}` and `GET /session/{sessionId}` have no `[Authorize]` override beyond the controller's `RequireAuthenticated`, unlike every mutating action below them in the same file. Low severity because the identifiers are GUIDs (not enumerable), but it's inconsistent with the rest of the controller and with B1's pattern.

**Fix:** Add `RequireClinicStaff` for consistency once B1 is addressed, so the whole finance/billing surface follows one rule instead of ad hoc per-endpoint decisions.

---

## 4. What Is Already Correct (don't re-build this)

- **Multi-session, multi-day collection attribution** — `OpdPayment.CollectedAmount` is a running total that can span multiple counter sessions and days (prepaid visit fee today, an injection charge collected next week); `GetDoctorWiseOpdCollectionHandler`, `GetReceptionWiseReportHandler`, and `GetFinanceDashboardSummaryHandler` all correctly back out the "item portion" and re-attribute it to the day/session it was *actually* collected in (see the `DEF-E2E-12` comments). This is non-trivial and already right — any rebuild should preserve this logic.
- **Particulars (injections, procedures, etc.) already count toward collection totals** at every level (doctor-wise, reception-wise, dashboard summary) — the user's "other indications... should also be there" requirement is already met by the data model and the existing aggregation queries.
- **Refund/discount domain modeling** on `OpdPayment` correctly re-evaluates `RefundDue` when a bill grows after a discount was queued, or when a visit is cancelled after collection (`CancelForFullRefund`) — edge cases that are easy to get wrong are handled.
- **Tenant scoping is correct everywhere except B6** — every handler in this review filters by `ApplicationId` at the query level (the one exception is the unscoped `GetAllAsync` call noted above).

---

## 5. Requirement-to-Reality Checklist

| User ask | Current state |
|---|---|
| Admin sees doctor-wise income, incl. per-appointment extras (injections etc.) and total collection | **Data exists and is correct** (`doctor-wise` endpoint + `OpdParticular` folding). Not surfaced as a drill-down (no per-appointment list under a doctor's total, no doctor-vs-configured-rate comparison) — only a flat per-doctor total on the admin Finance page. |
| Admin's finance dashboard covers everything, incl. expenses | Exists (`expenses` section) but the one admin-useful breakdown that's missing is per-receptionist collection (B5) and a doctor-fee-vs-actual-collected variance (blocked by B4). |
| Doctor sees the same when logged in, but only their own items | **Broken two ways**: dual-role users still see everyone (B2); even single-role doctors see clinic-wide expenses/counter/handover data that isn't "their" data at all (B3). |
| "Doctor can see other doctors' reports" is a bug | **Confirmed and root-caused** — B1 (no server check) + B2 (client filter keyed off the wrong thing). |
| Reception takes appointment fee + other fees from patients — billing should be correct | The fee actually charged is reception-typed-or-clinic-default (B4); the per-doctor rate screen that should anchor this is dead. The collection *data* (once charged) is tracked correctly. |

---

## 6. Prioritization

Priority = (Impact + Risk) × (6 − Effort), each scored 1–5 (Effort inverted: 1 = trivial, 5 = large).

| # | Item | Impact | Risk | Effort | Priority | Category |
|---|---|---|---|---|---|---|
| B1 | No server-side RBAC on finance-reports API | 5 | 5 | 2 | **40** | Security / Architecture debt |
| B2 | Finance page uses role claims, not active panel | 5 | 4 | 1 | **45** | Code debt (direct bug fix) |
| B3 | "My Finance" leaks clinic-wide sections to doctors | 4 | 4 | 2 | **32** | Code / Architecture debt |
| B4 | Dead per-doctor fee config / split-brain fee source | 4 | 3 | 3 | **21** | Architecture / correctness debt |
| B5 | Dead finance endpoints, incl. reception-wise | 3 | 2 | 2 | **20** | Dead code / missed feature |
| B6 | Unscoped `GetAllAsync` in reception-wise handler | 2 | 2 | 1 | **20** | Code debt (ADR-008 violation) |
| B7 | Doctor can apply discounts (policy question) | 2 | 2 | 1 | **20** | Process / policy debt |
| B8 | Payment GET endpoints missing per-action policy | 1 | 2 | 1 | **15** | Code debt |

---

## 7. Phased Remediation Plan

**Phase 0 — Security fix (ship ASAP, small diff, no schema change)**
- B1: lock down `FinanceReportsController` per endpoint as specified above.
- B2: switch `FinanceDashboard.razor`'s filter to `ClinicContext.ActivePanel`.
- Add server-side self-scoping on `doctor-wise` (force `doctorUserId` for non-admin callers) so B1 and B2 reinforce each other instead of either one alone being the only guard.
- This phase alone fixes the exact bug reported and closes the IDOR; it can land independently of everything else.

**Phase 1 — Stop the doctor-side data leak (small-medium diff)**
- B3: split or conditionally render the Finance Dashboard so a doctor's view only ever requests `doctor-wise` for themselves; move expenses/counter-sessions/handovers behind admin-only rendering **and** admin-only API policy (ties back into B1's per-endpoint policies).
- B8: align the two GET endpoints on `OpdPaymentsController` with the rest of the controller's policy.

**Phase 2 — Fix billing-rate correctness (needs a product decision first)**
- B4: decide Option A vs B above with the user/product owner, then implement. This is the one item that changes what patients are actually charged, so it should go out deliberately, not bundled with the security fixes.
- B7: confirm/scope doctor discount authority; adjust policy or document intent.

**Phase 3 — Clean up and complete admin visibility (can ride along with normal feature work)**
- B5: wire `reception-wise` into the admin Finance Dashboard as a "Collection by Staff" section (direct answer to the user's billing-integrity concern); remove the unused daily/weekly/monthly/summary endpoints and handlers (or confirm a page is planned for them).
- B6: add a tenant-scoped `GetByDateRangeAsync` to `ICounterSessionRepository` and stop using the unscoped `GetAllAsync` in `GetReceptionWiseReportHandler`.
- Nice-to-have once the above is stable: a per-doctor drill-down (list of appointments/particulars behind each doctor-wise total) and a configured-rate-vs-actual-collected variance view, once B4 gives the configured rate a real meaning.

---

## 8. Logging

Per the workspace's mandatory-logging rule (ADR-011), note for whoever implements Phase 0–2:
- The new self-scoping check on `doctor-wise` (and any 403s produced by tightened policies) should be visible in `ApiRequestLogs` automatically (failed/rejected requests are logged by the existing pipeline) — no new logging code needed, just don't add `ISkipActionLog` to these paths.
- If B4 is implemented as "flag free-form fee overrides as exceptions," that override should be action-logged with the OPD registration id and the delta from the configured rate (never log patient identifying details beyond what's already logged elsewhere) so a configured-rate-vs-charged-rate audit trail exists.
- No new sensitive fields are introduced by this plan — all amounts/ids already fit the existing "safe to log" set (no patient data, no payment credentials).
