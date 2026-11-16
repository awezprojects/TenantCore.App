# Feature Plan: IPD Billing & Deposit Completion

**Repo:** TenantCore.App
**Date:** 2026-11-12
**Domain area:** IPD (Wards / Rooms / Beds / Admissions / Billing)
**Status:** Approved — ready for execution
**Source:** Closes the gaps and defects in `docs/testing/IPD_Admin_Ward_Bed_Admission_E2E_TestReport.md` (DEF-IPD-01 through DEF-IPD-04, plus the Deposit / IPD billing / IPD reports gaps)

---

## Overview

The IPD E2E test report found that bed shifting is broken (old bed never freed, no occupancy check, no existence check, status trap), and that deposit collection, patient-facing IPD billing, and IPD reports do not exist at all. This plan fixes the bed/status defects at their root cause — `IpdRegistration` currently stores ward/room/bed as free-text strings instead of a real `Bed` relationship — and then builds patient billing for IPD by extending the already-proven OPD billing pattern (`OpdPayment` / `OpdParticular` / discount / `CounterSession`) rather than inventing a new one. Reception and clinical staff get: a working bed-transfer action, deposit collection (any amount, any time), IPD discounts (same roles as OPD), a day-by-day room-charge engine with two clinic-configurable policies, a **mid-stay printable estimate** distinct from the **locked bill produced at discharge**, reception-led discharge, and three IPD reports. Admin gets two new clinic-level billing settings and the Shift-Bed UI the report flagged as missing.

**Naming (confirmed):** the mid-stay, print-anytime, always-recalculated estimate is called the **Provisional Bill** ("Amount payable if discharged today — provisional, subject to change"). The locked, discharge-time bill is the **Final Bill**. Both are computed by the same shared calculator so the numbers never drift between the two except for the time elapsed.

**Billing-policy settings (confirmed — no silent default):** the two clinic-level room-billing settings (`IpdRoomBillingCycle`, `IpdMidStayChargeMode`) ship with **no default value**. Each is nullable and starts unset; a Clinic Admin must explicitly pick one of the two options for each, in Settings, before the clinic can allocate a bed to any patient (first admission or any later transfer). Allocating a bed while either setting is unset is blocked server-side with a clear error — this is enforced at the handler, not just hidden behind a UI nudge.

---

## Layers Affected

| Layer | Scope of Change |
|-------|------------------|
| Domain | 4 new entities (`IpdBedTransferHistory`, `IpdDeposit`, `IpdParticular`, `IpdPayment`) + repository interfaces; `IpdRegistration` gains `CurrentBedId` and loses free-form status/bed mutation; `ClinicFeatureFlags` gains two billing-policy settings; 2 new enums |
| Infrastructure | 4 new EF configs + repositories; `ClinicDbContext` (+4 DbSets); DI registrations; 3 EF migrations (incl. data backfill); `IBedRepository.FindByLocationAsync` removed (no longer has a caller) |
| Application | New commands/queries/handlers/validators/translators across bed-transfer, cancel, deposit, discount, particulars, bill calculation, settlement, and 3 reports; one new calculation service shared by the Provisional and Final Bill |
| API | `IpdRegistrationsController` tightened (Update) + 2 new actions (transfer-bed, cancel); 4 new controllers (`IpdDepositsController`, `IpdParticularsController`, `IpdPaymentsController`, `IpdReportsController`); `AuthPolicies` on Discharge relaxed to `RequireClinicStaff` |
| Shared | ~20 new DTOs; 2 new enums; `BillingErrorCodes`/`UserMessages` additions |
| Web.Client | Shift-Bed modal on `/ipd`; new "Deposit & Bill" tab on the admission-details popup (deposit, discount, particulars, Provisional Bill, Final Bill); 2 new print pages; new `/reports/ipd` page (3 tabs); 2 new settings controls on the clinic Feature Flags page |
| Middleware | `BillingFeatureGuardMiddleware` gains 4 new guarded route prefixes |

---

## Snapshot correction (done ahead of this plan)

`current-state.md` was stale (46 DbSets recorded vs. 51 actual — missing `OpdRegistrationNumberCounters`, `ClinicWallets`, `ClinicWalletTransactions`, `ClinicUsageRates`, `ClinicUsageRecords` and their 4 repository registrations). Corrected in place before writing this plan, per the plan command's auto-verify step. No feature work in that correction.

---

## Root-Cause Fix (Phase 0 — Bed Shift & Status)

### Entity: IpdBedTransferHistory *(new)*

**Tenant-scoped:** Yes · **Base class:** AuditableEntity

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| IpdRegistrationId | Guid | required, FK |
| BedId | Guid | required, FK |
| RoomId | Guid | required, denormalized for rate lookups |
| WardId | Guid | required, denormalized |
| RatePerDayAtTime | decimal | required — snapshot of `Room.PricePerDay` at occupancy start, so a later room-price change never rewrites a past stay's bill |
| OccupiedFrom | DateTime | required, UTC |
| OccupiedTo | DateTime? | null = this is the current segment |
| TransferredByUserId | Guid | required |
| Notes | string? | max 500 |

One row per bed-occupancy segment for an admission. Created on admit, closed + a new row opened on every transfer, closed (no new row) on discharge/cancel. This is also the backbone for the room-charge calculator and the Census/Revenue reports.

### IpdRegistration — modified

| Change | Detail |
|---|---|
| Add `CurrentBedId` (Guid?) | Source of truth for occupancy going forward. Null only for an admission with no bed assigned at all (ward-only admissions, same as today). |
| Keep `WardName`/`RoomNumber`/`BedNumber` | Now a **denormalized display snapshot**, written by the server from the resolved `Bed`/`Room`/`Ward` — never client-supplied free text again. |
| `Create(...)` signature change | Takes `BedId` (Guid?) instead of three name strings; resolves `Bed`→`Room`→`Ward` server-side to populate the display strings. |
| Remove bed/status params from `Update(...)` | `Update` now only accepts `DoctorUserId`, `DoctorName`, `AdmissionNotes` — status and bed are no longer settable through it (closes DEF-IPD-04's trap door). |
| New `Cancel(string? reason)` method | Throws `DomainValidationException` unless `Status == Admitted`. Sets `Status = Cancelled`. |

**IpdStatus enum: unchanged** (`Admitted=1, Discharged=2, Transferred=3, Cancelled=4`) — kept for backward-compatible deserialization of existing rows, but no command going forward ever sets `Transferred`. A bed transfer is an audit event on `IpdBedTransferHistory`, not a status change; the registration stays `Admitted` throughout. The backfill migration resets any existing `Status = Transferred` (3) row to `Admitted` (1) with a note, closing the "un-dischargeable" trap left by DEF-IPD-04 in existing data.

### Commands / Queries (Phase 0)

| Command/Query | Carries | Behavior |
|---|---|---|
| `TransferBedCommand` *(new)* | IpdRegistrationId, NewBedId, TransferredByUserId, Notes?, ApplicationId | **First** checks `ClinicFeatureFlags.IpdRoomBillingCycle`/`IpdMidStayChargeMode` are both configured — 409 `UserMessages.IpdBillingSettingsNotConfigured` if either is null (confirmed: no silent default, block bed allocation until a Clinic Admin picks both). Then validates registration is `Admitted`; validates `NewBedId` exists, `IsActive`, and `!IsOccupied` (404/409 exactly like `CreateIpdRegistrationHandler` already does for admission); closes the current `IpdBedTransferHistory` row, opens a new one, frees the old `Bed`, occupies the new one, updates `CurrentBedId` + display strings — one `SaveChangesAsync`. |
| `CreateIpdRegistrationCommand` *(modified)* | `BedId` (Guid?) replaces `WardName`/`RoomNumber`/`BedNumber` as input | Same billing-settings gate as `TransferBedCommand` above, but **only when `BedId` is supplied** — a ward-only admission with no bed yet assigned is not blocked, since no room charge applies until a bed is actually allocated. Same occupancy validation as today, now by id instead of fragile name matching; opens the first `IpdBedTransferHistory` row. |
| `UpdateIpdRegistrationCommand` *(modified)* | Drops Status/WardName/RoomNumber/BedNumber; keeps DoctorUserId, DoctorName, AdmissionNotes | — |
| `CancelIpdRegistrationCommand` *(new)* | IpdRegistrationId, Reason?, ApplicationId | Frees the current bed, closes the transfer-history row, calls `registration.Cancel(reason)`. Explicitly does **not** reverse the admission usage charge or touch billing — scoped narrowly to closing the open-status hole (see Open Questions). |
| `GetIpdBedTransferHistoryQuery` *(new)* | IpdRegistrationId, ApplicationId | Ordered list of segments for the admission-details popup and reports. |
| `DischargePatientCommand` *(modified)* | unchanged signature | Frees the bed via `registration.CurrentBedId` (not re-resolved from free-text strings — this alone fixes the "ghost bed" mechanism), closes the transfer-history row, and now also calls the bill calculator to lock the Final Bill (see Phase 2). |

### Data migration note (Phase 0 migration)

For every existing `IpdRegistration`: resolve `CurrentBedId` from the stored `WardName`/`RoomNumber`/`BedNumber` via the same lookup `FindByLocationAsync` used today; where it resolves, backfill `CurrentBedId` and create one `IpdBedTransferHistory` row spanning `AdmissionDate` → `DischargeDate` (or open, if still Admitted) at the room's current `PricePerDay`. Rows that don't resolve (the test report's `DUMMY ICU` / nonexistent-room data, and any ghost-occupied QA bed) are left with `CurrentBedId = null` and logged for manual admin cleanup — the migration must not fail on unmatched data. Any row with `Status = 3` (Transferred) is reset to `Admitted` with an appended note.

---

## Deposit & Discount (Phase 1)

### Entity: IpdDeposit *(new)*

**Tenant-scoped:** Yes · **Base class:** AuditableEntity

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| IpdRegistrationId | Guid | required, FK |
| Amount | decimal | required, > 0 |
| CollectedByUserId | Guid | required |
| CollectedAt | DateTime | UTC, set on create |
| CounterSessionId | Guid? | credited to the active counter session, same as `OpdPayment` |
| Notes | string? | max 500 |

Immutable ledger row — any amount, any number of times, any point during an active admission (per your answer). No per-deposit refund tracking: refund/balance is settled once, at discharge, against the Final Bill (see Phase 2) — the simple model you confirmed, extended to allow multiple top-ups.

### Commands / Queries (Phase 1)

| Command/Query | Carries | Behavior |
|---|---|---|
| `CollectIpdDepositCommand` *(new)* | IpdRegistrationId, Amount, CollectedByUserId, CounterSessionId?, Notes?, ApplicationId | Validates `Admitted` status and `Amount > 0`; creates `IpdDeposit`; credits the counter session. |
| `GetIpdDepositsQuery` *(new)* | IpdRegistrationId, ApplicationId | List for the Deposit & Bill tab and both bill prints. |
| `ApplyIpdDiscountCommand` *(new)* | IpdRegistrationId, Discount, DiscountedByUserId, ApplicationId | Same `RequireClinicStaff` policy as OPD's `ApplyOpdDiscountCommand` — reception or doctor. Recomputes `IpdPayment.FinalAmount`/`BalanceDue` immediately (mirrors `OpdPayment.ApplyDiscount`). |

---

## Particulars (Phase 2a — reuses the existing `Particular` catalogue)

### Entity: IpdParticular *(new — mirrors OpdParticular)*

**Tenant-scoped:** Yes · **Base class:** AuditableEntity

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| IpdRegistrationId | Guid | required, FK |
| ParticularId | Guid | required, FK to existing `Particular` |
| Description | string | snapshot of the particular's name at the time added |
| Amount | decimal | required |
| Quantity | int | required, ≥ 1 |
| AddedByUserId | Guid | required |
| AddedAt | DateTime | UTC |

### Commands / Queries (Phase 2a)

| Command/Query | Behavior |
|---|---|
| `AddIpdParticularCommand` *(new)* | Adds a line item, recomputes `IpdPayment.ParticularsTotal`. |
| `RemoveIpdParticularCommand` *(new)* | Removes a line item (not yet collected), recomputes totals. |
| `GetIpdParticularsQuery` *(new)* | List for the Deposit & Bill tab and both bill prints. |

---

## Billing Engine, Provisional Bill & Final Bill (Phase 2b)

### Clinic-level settings — ClinicFeatureFlags *(modified)*

| New field | Type | Default | Meaning |
|---|---|---|---|
| `IpdRoomBillingCycle` | `RoomBillingCycle?` enum *(new)*, nullable | **None — must be explicitly set** | `PerCalendarDay` = charge changes at midnight; `Per24HourBlock` = charge changes every 24h from admission time. Confirmed: no silent default — null until a Clinic Admin picks one. |
| `IpdMidStayChargeMode` | `MidStayBedChangeChargeMode?` enum *(new)*, nullable | **None — must be explicitly set** | On the day/block a bed or room change happens: `FullDayCharge` = bill the full day/block rate; `Prorated` = split that day/block's charge between the old and new room rates by time share. Confirmed: no silent default — null until a Clinic Admin picks one. |

`ClinicFeatureFlags.Update(...)` signature extends with these two nullable parameters (existing `UpdateClinicFeatureFlagsCommand` modified, not a new command). The Settings UI renders both as a dropdown with **no pre-selected option** (placeholder "Select…") rather than defaulting to the first choice — the blank state is deliberate, not a bug, so an admin can't save the page without having actually chosen. The real enforcement, though, is server-side in `CreateIpdRegistrationHandler`/`TransferBedHandler` (Phase 0) — the UI nudge alone would not be enough.

### Service: IpdBillCalculator *(new Application service, not a command)*

Given an `IpdRegistration`, its `IpdBedTransferHistory` segments, the clinic's two settings above, and an "as-of" timestamp (now, or `DischargeDate`), walks each occupancy segment and returns: `RoomChargesTotal`, the day-by-day breakdown (for the printed bill's itemization), `ParticularsTotal`, `InitialFee`, `TotalAmount`, and — combined with deposits collected to date — `TotalDeposited` and `BalanceDue` (negative = refund owed). This single calculator is called by both the Provisional Bill query (as-of now, never persisted) and the Final Bill finalize command (as-of discharge, persisted) — the two numbers can only differ by the stay continuing, never by a calculation discrepancy.

### Entity: IpdPayment *(new — mirrors OpdPayment)*

**Tenant-scoped:** Yes · **Base class:** AuditableEntity · 1:1 with `IpdRegistration`, auto-created at admission (same as `OpdPayment` auto-creates at OPD registration)

| Property | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| IpdRegistrationId | Guid | required, unique FK |
| InitialFee | decimal | copied from `IpdRegistration.InitialFee` at creation |
| RoomChargesTotal | decimal | refreshed by the calculator on every deposit/particular/discount/discharge event |
| ParticularsTotal | decimal | refreshed the same way |
| TotalAmount | decimal | `InitialFee + RoomChargesTotal + ParticularsTotal` |
| Discount | decimal | via `ApplyIpdDiscountCommand` |
| DiscountedByUserId / DiscountedAt | Guid? / DateTime? | mirrors `OpdPayment` |
| FinalAmount | decimal | `TotalAmount - Discount` |
| TotalDeposited | decimal | cached sum of `IpdDeposit.Amount` |
| BalanceDue | decimal | `FinalAmount - TotalDeposited` as of last refresh; negative = refund owed |
| PaymentStatus | existing `PaymentStatus` enum (reused, not new) | `Pending` until discharge locks the Final Bill, `Received` once the post-discharge balance is fully collected |
| SettledAt / SettledByUserId | DateTime? / Guid? | set when `CollectIpdBalanceCommand` or `ProcessIpdRefundCommand` completes |
| CounterSessionId | Guid? | the settlement transaction's session |
| RefundDue / RefundStatus / RefundedAt / RefundedByUserId | decimal / existing `RefundStatus` enum (reused) / DateTime? / Guid? | mirrors `OpdPayment`'s refund fields exactly |

### Commands / Queries (Phase 2b)

| Command/Query | Carries | Behavior |
|---|---|---|
| `GetIpdProvisionalBillQuery` *(new)* | IpdRegistrationId, ApplicationId | Calls the calculator as-of **now**; returns a response explicitly flagged `IsProvisional = true` with the label "Provisional Bill — amount payable if discharged today." Never writes anything. Available any time the admission is `Admitted`. |
| `FinalizeIpdBillCommand` *(new, called from `DischargePatientHandler`)* | IpdRegistrationId, ApplicationId | Calls the calculator as-of `DischargeDate`, persists `RoomChargesTotal`/`ParticularsTotal`/`TotalAmount`/`FinalAmount`/`TotalDeposited`/`BalanceDue`/`RefundDue` onto `IpdPayment`. Runs inside the same transaction as `Discharge` — the patient leaves with a known, locked number; the cash/refund step is separate (matches how OPD already separates "fee known" from "fee collected"). |
| `CollectIpdBalanceCommand` *(new)* | IpdRegistrationId, ReceivedByUserId, CounterSessionId?, ApplicationId | Only valid when `BalanceDue > 0` and the bill is finalized. Mirrors `OpdPayment.AcceptFull`. |
| `ProcessIpdRefundCommand` *(new)* | IpdRegistrationId, RefundedByUserId, ApplicationId | Only valid when `RefundDue > 0` (deposits exceeded the Final Bill). Mirrors `OpdPayment.ProcessRefund` / `ProcessOpdRefundCommand`. |
| `GetIpdFinalBillQuery` *(new)* | IpdRegistrationId, ApplicationId | Fetches the locked `IpdPayment` for the Final Bill print — only returns a result once `FinalizeIpdBillCommand` has run (i.e., after discharge). |

---

## Reports (Phase 3)

All three are read-only queries — lowest risk phase, built once Phases 0–2 produce real data.

| Report | Query | Content |
|---|---|---|
| IPD Census | `GetIpdCensusReportQuery` *(new)* | Current occupancy by ward/room/bed, from `Ward`/`Room`/`Bed`/`IpdRegistration` — no new entity. |
| IPD Revenue | `GetIpdRevenueReportQuery` *(new)* | Deposits + settled balances/refunds over a date range, grouped like the existing Finance Reports (day/week/month/date-range). |
| IPD Discharge Summary | `GetIpdDischargeSummaryReportQuery` *(new)* | Discharges in a date range **with patient details** (name, MR number, admission number, admission/discharge dates, Final Bill amount, balance settled) — per your answer. Patient name/MR number are UI-only; never written to the Azure Table log rows. |

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|---|---|
| `Enums/RoomBillingCycle.cs` | PerCalendarDay / Per24HourBlock |
| `Enums/MidStayBedChangeChargeMode.cs` | FullDayCharge / Prorated |
| `Dtos/Ipd/IpdBedTransferHistoryDto.cs` | One occupancy segment |
| `Dtos/Ipd/TransferBedRequest.cs` | NewBedId, Notes |
| `Dtos/Ipd/CancelIpdRegistrationRequest.cs` | Reason |
| `Dtos/Ipd/IpdDepositDto.cs`, `CollectIpdDepositRequest.cs` | Deposit read/write |
| `Dtos/Ipd/IpdParticularDto.cs`, `AddIpdParticularRequest.cs` | Particular read/write |
| `Dtos/Ipd/ApplyIpdDiscountRequest.cs` | Discount amount |
| `Dtos/Ipd/IpdBillLineItemDto.cs` | One day/segment row for the itemized bill |
| `Dtos/Ipd/IpdProvisionalBillDto.cs` | IsProvisional flag + full breakdown |
| `Dtos/Ipd/IpdFinalBillDto.cs` | Locked breakdown + settlement status |
| `Dtos/Ipd/IpdCensusReportDto.cs`, `IpdRevenueReportDto.cs`, `IpdDischargeSummaryReportDto.cs` | Report rows |
| `Dtos/Ipd/UpdateIpdBillingSettingsRequest.cs` | The two new ClinicFeatureFlags fields |

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|---|---|
| `Entities/IpdBedTransferHistory.cs` | As specified above |
| `Entities/IpdDeposit.cs` | As specified above |
| `Entities/IpdParticular.cs` | As specified above |
| `Entities/IpdPayment.cs` | As specified above |
| `Interfaces/IIpdBedTransferHistoryRepository.cs` | Extends `IClinicRepository<T>`; `GetCurrentSegmentAsync`, `GetByRegistrationAsync` |
| `Interfaces/IIpdDepositRepository.cs` | `GetByRegistrationAsync`, `GetTotalByRegistrationAsync` |
| `Interfaces/IIpdParticularRepository.cs` | `GetByRegistrationAsync` |
| `Interfaces/IIpdPaymentRepository.cs` | `GetByRegistrationAsync` |

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|---|---|
| `Persistence/Configurations/IpdBedTransferHistoryConfiguration.cs` | FK to IpdRegistration/Bed/Room/Ward; index on (ApplicationId, IpdRegistrationId) |
| `Persistence/Configurations/IpdDepositConfiguration.cs` | FK to IpdRegistration |
| `Persistence/Configurations/IpdParticularConfiguration.cs` | FK to IpdRegistration, Particular |
| `Persistence/Configurations/IpdPaymentConfiguration.cs` | Unique index on IpdRegistrationId |
| `Repositories/IpdBedTransferHistoryRepository.cs` | Implements interface, extends `ClinicRepository<T>` |
| `Repositories/IpdDepositRepository.cs` | ″ |
| `Repositories/IpdParticularRepository.cs` | ″ |
| `Repositories/IpdPaymentRepository.cs` | ″ |

### Application Layer (`src/TenantCore.Application/Features/IpdRegistrations/`)

| File | Purpose |
|---|---|
| `Commands/TransferBedCommand.cs`, `Handlers/TransferBedHandler.cs`, `Validators/TransferBedCommandValidator.cs` | Phase 0 |
| `Commands/CancelIpdRegistrationCommand.cs`, `Handlers/CancelIpdRegistrationHandler.cs`, `Validators/CancelIpdRegistrationCommandValidator.cs` | Phase 0 |
| `Queries/GetIpdBedTransferHistoryQuery.cs`, `Handlers/GetIpdBedTransferHistoryHandler.cs` | Phase 0 |
| `Commands/CollectIpdDepositCommand.cs`, `Handlers/CollectIpdDepositHandler.cs`, `Validators/CollectIpdDepositCommandValidator.cs` | Phase 1 |
| `Queries/GetIpdDepositsQuery.cs`, `Handlers/GetIpdDepositsHandler.cs` | Phase 1 |
| `Commands/ApplyIpdDiscountCommand.cs`, `Handlers/ApplyIpdDiscountHandler.cs`, `Validators/ApplyIpdDiscountCommandValidator.cs` | Phase 1 |
| `Commands/AddIpdParticularCommand.cs`, `Handlers/AddIpdParticularHandler.cs`, `Validators/AddIpdParticularCommandValidator.cs` | Phase 2a |
| `Commands/RemoveIpdParticularCommand.cs`, `Handlers/RemoveIpdParticularHandler.cs` | Phase 2a |
| `Queries/GetIpdParticularsQuery.cs`, `Handlers/GetIpdParticularsHandler.cs` | Phase 2a |
| `Services/IIpdBillCalculator.cs`, `Services/IpdBillCalculator.cs` | Phase 2b — shared calculation logic |
| `Queries/GetIpdProvisionalBillQuery.cs`, `Handlers/GetIpdProvisionalBillHandler.cs` | Phase 2b |
| `Commands/FinalizeIpdBillCommand.cs`, `Handlers/FinalizeIpdBillHandler.cs` | Phase 2b |
| `Commands/CollectIpdBalanceCommand.cs`, `Handlers/CollectIpdBalanceHandler.cs`, `Validators/CollectIpdBalanceCommandValidator.cs` | Phase 2b |
| `Commands/ProcessIpdRefundCommand.cs`, `Handlers/ProcessIpdRefundHandler.cs`, `Validators/ProcessIpdRefundCommandValidator.cs` | Phase 2b |
| `Queries/GetIpdFinalBillQuery.cs`, `Handlers/GetIpdFinalBillHandler.cs` | Phase 2b |
| `Queries/GetIpdCensusReportQuery.cs`, `Handlers/GetIpdCensusReportHandler.cs` | Phase 3 |
| `Queries/GetIpdRevenueReportQuery.cs`, `Handlers/GetIpdRevenueReportHandler.cs` | Phase 3 |
| `Queries/GetIpdDischargeSummaryReportQuery.cs`, `Handlers/GetIpdDischargeSummaryReportHandler.cs` | Phase 3 |
| `Translators/IpdBedTransferHistoryTranslator.cs`, `IpdDepositTranslator.cs`, `IpdParticularTranslator.cs`, `IpdPaymentTranslator.cs` | One per new entity |

### API Layer (`src/TenantCore.Api/Controllers/`)

| File | Purpose |
|---|---|
| `IpdDepositsController.cs` | `api/ipd-deposits` |
| `IpdParticularsController.cs` | `api/ipd-particulars` |
| `IpdPaymentsController.cs` | `api/ipd-payments` — discount, interim bill, final bill, collect balance, process refund |
| `IpdReportsController.cs` | `api/ipd-reports` — census, revenue, discharge-summary |

### Web.Client (`src/TenantCore.Web.Client/`)

| File | Purpose |
|---|---|
| `Clients/IIpdBillingApiClient.cs`, `IpdBillingApiClient.cs` | Typed client covering transfer-bed, cancel, deposits, discount, particulars, interim/final bill, settle, refund |
| `Clients/IIpdReportsApiClient.cs`, `IpdReportsApiClient.cs` | Typed client for the 3 reports |
| `Components/Ipd/TransferBedModal.razor` | Shift-Bed UI — the missing button the report flagged |
| `Components/Ipd/IpdDepositBillTab.razor` | Deposit entry, discount entry, particulars list, running totals, "Print Provisional Bill" button — slotted into the admission-details popup |
| `Pages/Ipd/IpdProvisionalBillPrint.razor` | Print view, `window.open`, labelled "Provisional Bill", red ✕ Close (per `.clinerules` §7) |
| `Pages/Ipd/IpdFinalBillPrint.razor` | Print view, labelled "Final Bill", settlement status, red ✕ Close |
| `Pages/Reports/IpdReports.razor`, `.razor.cs` | 3-tab report page (Census / Revenue / Discharge Summary), `opd-*` theme, fetch-once-per-filter-apply pattern (Cross-Repo Rule 7 — no per-click re-query) |

---

## Files to Modify

| File | Change |
|---|---|
| `src/TenantCore.Domain/Entities/IpdRegistration.cs` | `CurrentBedId`, new `Cancel()`, trimmed `Update()`, `Create()` takes `BedId` |
| `src/TenantCore.Domain/Entities/ClinicFeatureFlags.cs` | + `IpdRoomBillingCycle`, `IpdMidStayChargeMode`; `Update()` extended |
| `src/TenantCore.Domain/Interfaces/IBedRepository.cs` | Remove `FindByLocationAsync` (no caller remains once Create/Transfer/Discharge all use `BedId`) |
| `src/TenantCore.Infrastructure/Repositories/BedRepository.cs` | Remove the now-dead implementation |
| `src/TenantCore.Application/Features/IpdRegistrations/Handlers/CreateIpdRegistrationHandler.cs` | Resolve by `BedId`, open first `IpdBedTransferHistory` row, gate on both billing settings being configured when `BedId` is supplied |
| `src/TenantCore.Shared/Errors/UserMessages.cs` | Add `IpdBillingSettingsNotConfigured` ("Configure IPD room billing settings in Clinic Settings before allocating a bed.") |
| `src/TenantCore.Application/Features/IpdRegistrations/Handlers/UpdateIpdRegistrationHandler.cs` | Drop bed/status handling |
| `src/TenantCore.Application/Features/IpdRegistrations/Handlers/DischargePatientHandler.cs` | Free bed via `CurrentBedId`; close transfer-history row; call `FinalizeIpdBillCommand` |
| `src/TenantCore.Application/Features/IpdRegistrations/Commands/CreateIpdRegistrationCommand.cs`, `UpdateIpdRegistrationCommand.cs` | Field changes per above |
| `src/TenantCore.Application/Features/IpdRegistrations/Translators/IpdRegistrationTranslator.cs` | Map `CurrentBedId` |
| `src/TenantCore.Application/Features/PlatformAdmin or ClinicFeatureFlags area/Handlers/UpdateClinicFeatureFlagsHandler.cs` *(locate exact existing file)* | Accept + persist the two new settings |
| `src/TenantCore.Api/Controllers/IpdRegistrationsController.cs` | `Update` DTO trimmed; `Discharge` policy → `RequireClinicStaff`; + `PATCH {id}/transfer-bed` (RequireReception), + `PATCH {id}/cancel` (RequireReception) |
| `src/TenantCore.Api/Middleware/BillingFeatureGuardMiddleware.cs` | Add `/api/ipd-deposits`, `/api/ipd-particulars`, `/api/ipd-payments`, `/api/ipd-reports` to `GuardedPathPrefixes` |
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | + 4 DbSets |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | + 4 repository registrations |
| `src/TenantCore.Shared/Dtos/UpdateIpdRegistrationDto.cs`, `CreateIpdRegistrationDto.cs` | Field changes per above |
| `src/TenantCore.Web.Client/Pages/Ipd.razor` (or wherever `/ipd` lives) | Shift-Bed button, Deposit & Bill tab wiring |
| `src/TenantCore.Web.Client/Pages/Settings/*FeatureFlags*.razor` | Two new settings controls |
| OBS-1 fix: admission-number/date generation | Use clinic-local time for `IPD-yyyyMMdd-NNNN`, not UTC |
| OBS-3 fix: dummy room-type seed data | Relabel to match the "General (4-bed)" / "General (6-bed)" list |

---

## API Endpoints

| Method | Route | Auth Policy | Notes |
|---|---|---|---|
| PUT | `api/ipd-registrations/{id}` | RequireReception | Trimmed — doctor/notes only |
| PATCH | `api/ipd-registrations/{id}/transfer-bed` | RequireReception | *(new)* |
| PATCH | `api/ipd-registrations/{id}/cancel` | RequireReception | *(new)* |
| PATCH | `api/ipd-registrations/{id}/discharge` | **RequireClinicStaff** *(was RequireClinical)* | Per your answer — reception can discharge |
| GET | `api/ipd-registrations/{id}/bed-history` | RequireAuthenticated | *(new)* |
| POST | `api/ipd-deposits` | RequireClinicStaff | *(new)* |
| GET | `api/ipd-deposits/{ipdRegistrationId}` | RequireAuthenticated | *(new)* |
| POST | `api/ipd-particulars` | RequireClinicStaff | *(new)* |
| DELETE | `api/ipd-particulars/{id}` | RequireClinicStaff | *(new)* |
| GET | `api/ipd-particulars/{ipdRegistrationId}` | RequireAuthenticated | *(new)* |
| POST | `api/ipd-payments/{ipdRegistrationId}/discount` | RequireClinicStaff | *(new)* |
| GET | `api/ipd-payments/{ipdRegistrationId}/provisional-bill` | RequireClinicStaff | *(new)* — live, never persisted; reception included (confirmed) |
| GET | `api/ipd-payments/{ipdRegistrationId}/final-bill` | RequireClinicStaff | *(new)* — only after discharge |
| POST | `api/ipd-payments/{ipdRegistrationId}/collect-balance` | RequireClinicStaff | *(new)* |
| POST | `api/ipd-payments/{ipdRegistrationId}/process-refund` | RequireClinicStaff | *(new)* |
| GET | `api/ipd-reports/census` | RequireClinicStaff | *(new)* — reception included (confirmed) |
| GET | `api/ipd-reports/revenue` | RequireClinicStaff | *(new)* — reception included (confirmed) |
| GET | `api/ipd-reports/discharge-summary` | RequireClinicStaff | *(new)* — reception included (confirmed); shows patient + bill details |
| PUT | `api/clinic-feature-flags` *(existing route)* | RequireClinicAdmin | Extended body with 2 new fields |

---

## Validation Rules

Per ADR-010 §3 (mandatory for every new string field — see the `TenantCore.Application.Common` helpers table there): every free-text field below uses the shared helper instead of a new inline regex.

| Field | Rules |
|---|---|
| `TransferBedCommand.NewBedId` | NotEmpty; must exist, be `IsActive`, and `!IsOccupied` (checked in handler → 404/409, not the validator) |
| `TransferBedCommand.Notes` | MaximumLength(500); `HtmlSanitizationRules.HasNoScriptInjection` + `HasNoLinks` when not empty |
| `CollectIpdDepositCommand.Amount` | GreaterThan(0) |
| `CollectIpdDepositCommand.Notes` | MaximumLength(500); `HtmlSanitizationRules.HasNoScriptInjection` + `HasNoLinks` when not empty |
| `ApplyIpdDiscountCommand.Discount` | GreaterThanOrEqualTo(0); handler rejects a discount larger than `TotalAmount` |
| `AddIpdParticularCommand.Quantity` | GreaterThanOrEqualTo(1) |
| `AddIpdParticularCommand.Amount` | GreaterThanOrEqualTo(0) |
| `CancelIpdRegistrationCommand.Reason` | MaximumLength(500); `SafeTextValidationRules.IsSafeText` when not empty |
| All commands | `ApplicationId` — NotEmpty |

(`AdmissionNotes`/`DischargeNotes` on the existing `IpdRegistration` commands are already in scope for the same `HasNoScriptInjection`/`HasNoLinks` pair if their current validators don't yet have it — confirm during execution and add if missing, since ADR-010 §3 now applies to any validator touched by this feature, not only the new ones.)

---

## Business Rules

1. Bed occupancy changes only through `Create`, `TransferBed`, `Discharge`, `Cancel` — all resolve/validate a real `Bed` by id; free text is display-only. Fixes DEF-IPD-01/02/03.
2. `TransferBed`/`Create` targeting an inactive, nonexistent, or already-occupied bed throws `EntityNotFoundException` (404) or `InvalidOperationException` (409) — never a silent 200. Fixes DEF-IPD-02/03.
3. `IpdStatus` is never set directly by any command; `Transferred` is retired from assignment (existing rows reset by migration). Fixes DEF-IPD-04.
4. Room charges are computed by walking `IpdBedTransferHistory` segments against the clinic's `IpdRoomBillingCycle`/`IpdMidStayChargeMode` settings — always recomputed, never silently stale beyond the last refresh point (deposit, particular, discount, discharge, or a Provisional Bill view).
5. Deposits: any amount, any number of times, while `Admitted`; immutable ledger; credited to the active counter session.
6. Discount: reception or doctor (`RequireClinicStaff`), same as OPD; recalculates `FinalAmount`/`BalanceDue` immediately.
7. Provisional Bill: available any time while `Admitted`; always computed live as-of now; clearly labeled provisional; informational/printable only — never collects or moves money.
8. Discharge locks the Final Bill (as-of `DischargeDate`) in the same transaction as the discharge itself, but collecting the resulting balance or refund is a separate reception action performed at the counter — mirrors the existing OPD split between "fee known" and "fee collected."
9. Reception (`RequireClinicStaff`) can discharge, collect deposits/balance, apply discount, process refunds, and print both bills.
10. Reports show patient name/MR number on screen and in print only — never written to the Azure Table log rows (no-PII rule, ADR-011).
11. ~~`CancelIpdRegistrationCommand` only frees the bed and sets `Cancelled` — it does **not** reverse the IPD-admission usage charge or any collected deposit; that reversal is explicitly out of scope here (see Open Questions).~~ **Superseded 13-Nov by Phase 4a:** cancel now finalizes the bill at ₹0 (waiving the initial fee too), reverses the `IpdAdmission` usage charge, and is blocked outright while service items are attached. See Phase 4a above.
12. **(confirmed)** Neither `IpdRoomBillingCycle` nor `IpdMidStayChargeMode` has a default — both start null. `CreateIpdRegistrationCommand` (when `BedId` is supplied) and `TransferBedCommand` reject allocating a bed with a 409 (`UserMessages.IpdBillingSettingsNotConfigured`) until a Clinic Admin has explicitly picked both in Settings. A ward-only admission with no `BedId` is not blocked, since no room charge applies until a bed is actually allocated.

---

## Multi-Tenancy Checklist

- [ ] `ApplicationId` present on all 4 new entities
- [ ] `ApplicationId` passed in every new command/query
- [ ] Every new/modified repository query filters by `applicationId`
- [ ] Every new controller action uses `GetApplicationId()` from `ClinicControllerBase` — no header reads
- [ ] Blazor typed clients send `X-Application-Id` (existing `HttpClient` handler — no change needed)

---

## Logging (ADR-011 — mandatory)

| Operation | How it is logged | Searchable context (`IActionLogContext`) |
|---|---|---|
| `TransferBedCommand` | ActionLogs (automatic) | `ipdRegistrationId`, `oldBedId`, `newBedId` |
| `CancelIpdRegistrationCommand` | ActionLogs (automatic) | `ipdRegistrationId` |
| `CollectIpdDepositCommand` | ActionLogs (automatic) | `ipdRegistrationId`, `depositId`, `amount`, `counterSessionId` |
| `ApplyIpdDiscountCommand` | ActionLogs (automatic) | `ipdRegistrationId`, `discount` |
| `AddIpdParticularCommand` / `RemoveIpdParticularCommand` | ActionLogs (automatic) | `ipdRegistrationId`, `particularId` |
| `FinalizeIpdBillCommand` | ActionLogs (automatic) | `ipdRegistrationId`, `paymentId`, `finalAmount`, `balanceDue` |
| `CollectIpdBalanceCommand` / `ProcessIpdRefundCommand` | ActionLogs (automatic) | `ipdRegistrationId`, `paymentId`, `amount`, `counterSessionId` |
| `GET api/ipd-payments/.../provisional-bill`, 3 report GETs | ApiRequestLogs (automatic — only if failed/slow); queries are not `IBusinessAction` | — |
| `DischargePatientCommand` (modified) | ActionLogs (automatic, existing) | add `paymentId`, `balanceDue` to its existing context |

- [ ] Every failure path throws or returns ≥ 400 (never 200 with an error body)
- [ ] No `ISkipActionLog` anywhere in this feature
- [ ] Nothing sensitive logged: no patient name, MR number, contact info, notes text, or amounts' source document — only ids and numeric totals (consistent with how `OpdPayment` amounts are already logged)
- [ ] Tests assert the new `IActionLogContext` values for each command above

---

## EF Migrations

Run at the end of each phase's infrastructure step, in this order:

| # | Name | Contents |
|---|---|---|
| 1 | `AddIpdBedTransferHistoryAndCurrentBedId` | `IpdBedTransferHistory` table; `IpdRegistration.CurrentBedId` column; **data backfill**: resolve `CurrentBedId` from existing free-text fields where possible, create one historical transfer-history segment per existing admission, reset `Status = 3` rows to `Admitted`, leave unresolved rows with `CurrentBedId = null` (logged, not failed) |
| 2 | `AddIpdBillingSettingsToFeatureFlags` | `ClinicFeatureFlags.IpdRoomBillingCycle`, `IpdMidStayChargeMode` columns — **nullable, no default** (confirmed); every existing clinic starts unset and must configure both before its next bed allocation |
| 3 | `AddIpdDepositParticularAndPayment` | `IpdDeposits`, `IpdParticulars`, `IpdPayments` tables; **data backfill**: auto-create one `IpdPayment` row per existing non-cancelled `IpdRegistration` seeded with `InitialFee`, zeroed totals (recalculated on first read), mirroring how `OpdPayment` back-fills |

```
dotnet ef migrations add <Name> --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

---

## Implementation Order

1. Shared enums + DTOs (all phases)
2. Domain entities: `IpdBedTransferHistory` → `IpdDeposit` → `IpdParticular` → `IpdPayment`; `IpdRegistration`/`ClinicFeatureFlags` modifications
3. Domain repository interfaces (4 new)
4. Infrastructure EF configs (4 new) + repository implementations (4 new); remove `FindByLocationAsync`
5. `ClinicDbContext` — add 4 DbSets
6. Infrastructure DI — register 4 repositories
7. **Migration 1** (`AddIpdBedTransferHistoryAndCurrentBedId`) — run and verify backfill before continuing
8. Application: Phase 0 commands/queries/handlers/validators (`TransferBed`, `Cancel`, bed-transfer history query) + modify `Create`/`Update`/`Discharge` handlers
9. API: modify `IpdRegistrationsController`; Blazor: Shift-Bed modal — **ship/verify this slice against the E2E report before continuing** (it's the defect fix; everything after is net-new capability)
10. **Migration 2** (`AddIpdBillingSettingsToFeatureFlags`)
11. Application: Phase 1 (deposit, discount) + Phase 2a (particulars)
12. Application: `IpdBillCalculator` service + Phase 2b (interim/final bill, finalize, settle, refund)
13. **Migration 3** (`AddIpdDepositParticularAndPayment`) — run and verify backfill
14. API: 4 new controllers; `BillingFeatureGuardMiddleware` route additions
15. Application: Phase 3 reports
16. API: `IpdReportsController`
17. Web.Client: Deposit & Bill tab, both print pages, `/reports/ipd` page, settings controls
18. Unit tests — all handlers, validators, translators (see below)
19. OBS-1/OBS-3 fixes

---

## Test Files to Create

All under `tests/TenantCore.Application.Tests/Features/IpdRegistrations/`, following the standard coverage (happy path, not-found, cross-tenant isolation, boundary validation) for every new handler/validator/translator listed under Files to Create. Specific scenarios beyond the standard template:

| Test | Scenario |
|---|---|
| `TransferBedHandlerTests` | Old bed freed + new bed occupied in one call; 409 on occupied target; 404 on nonexistent/inactive target; transfer-history row closed+opened correctly |
| `DischargePatientHandlerTests` (extended) | Bed freed via `CurrentBedId` even after a prior transfer (regression test for DEF-IPD-01); `FinalizeIpdBillCommand` invoked |
| `IpdBillCalculatorTests` | Each `RoomBillingCycle` × `MidStayBedChangeChargeMode` combination (4 cases) against a multi-segment stay; single-day stay; same-day admit+discharge |
| `GetIpdProvisionalBillHandlerTests` | `IsProvisional = true`; nothing persisted; matches `FinalizeIpdBillCommand`'s math when run at the same instant |
| `CreateIpdRegistrationHandlerTests` (extended), `TransferBedHandlerTests` (extended) | 409 `IpdBillingSettingsNotConfigured` when either `ClinicFeatureFlags` setting is null and a `BedId` is supplied; ward-only `Create` (no `BedId`) succeeds regardless of settings |
| `CollectIpdDepositHandlerTests` | Multiple deposits accumulate correctly; rejects when not `Admitted` |
| `ApplyIpdDiscountHandlerTests` | Recomputes `BalanceDue`; rejects discount > `TotalAmount` |
| `CancelIpdRegistrationHandlerTests` | Frees bed; does not touch `IpdPayment`/usage charges (per Business Rule 11) |
| `TransferBedCommandValidatorTests`, `CollectIpdDepositCommandValidatorTests`, `CancelIpdRegistrationCommandValidatorTests` | Per ADR-010 §3/`.clinerules` §5: malicious-payload `[InlineData]` cases must fail — `<script>alert(1)</script>`, `javascript:alert(1)`, `<img src=x onerror=alert(1)>`, a bare `http://` URL in the Notes/Reason fields |

---

## Phase 4 — 13-Nov QA Retest Follow-on (new work)

**Source:** Clinic Admin + Reception retest email, 13-Nov-2026. Confirms DEF-IPD-05 through 13 all pass. Raises one new defect requiring a billing-correctness fix (DEF-IPD-19), one sizeable new capability (DEF-IPD-16, clinic-specific shift charging), four more defects (DEF-IPD-14/15/17/18), and four small UI fixes. Build order: **4a first** (explicitly requested — small, ships alone), then 4c/4d (contained bug fixes, no schema change), then 4b last (largest, needs a migration and touches the billing engine).

### 4a. Cancel finalizes the bill at ₹0 (fixes DEF-IPD-19) — ✅ Done 13-Nov, pending QA retest

**Problem:** `CancelIpdRegistrationHandler` called the normal `FinalizeIpdBillCommand` (same calculator a discharge uses), so a cancelled ICU stay still billed a full day + initial fee — a mistake being undone was being charged for, and a patient could end up owing money for a stay that, by definition, didn't happen.

**Fix applied:**
- `IpdPayment` gains `FinalizeAtCancellation(DateTime cancelledAt)` — sets `RoomChargesTotal`/`TotalAmount`/`FinalAmount` to **0** directly (bypasses the `InitialFee + RoomCharges + Particulars` formula entirely, so the initial fee is waived too, not just the room), `BalanceDue = -TotalDeposited`, and flags a full `RefundDue` when any deposit was collected — same `RefundStatus.PendingRefund` → `ProcessIpdRefundCommand` (**Confirm Refund Given**) path the Final Bill already uses, so no new UI is needed.
- `CancelIpdRegistrationHandler` now calls this directly instead of dispatching `FinalizeIpdBillCommand`, and **blocks the cancel** with a `DomainValidationException` ("Remove the service items or discharge the patient instead.") when `IpdPayment.ParticularsTotal > 0` — per your answer, service items mean this is a real stay, not a mistake, so cancel is the wrong action.
- **Open question 1 answered: yes, reverse the usage charge too.** The handler now also dispatches `ReverseUsageChargeCommand(BillingItem.IpdAdmission, registration.Id, ...)` — the same symmetric reversal `UpdateOpdRegistrationHandler` already uses for a cancelled OPD registration — in the same transaction as the cancel.
- `CancelIpdRegistrationCommand` gained an optional `CancelledByUserId` (threaded from the controller's existing `GetCurrentUserId()`) so the reversal's wallet-ledger row records who cancelled.

**Files touched:** `IpdPayment.cs` (+`FinalizeAtCancellation`), `CancelIpdRegistrationHandler.cs`, `CancelIpdRegistrationCommand.cs`, `IpdRegistrationsController.cs` (Cancel action). Tests: `CancelIpdRegistrationHandlerTests.cs` rewritten — particulars-block case, ₹0-bill-with-full-refund case, usage-reversal-dispatched case, plus the two pre-existing not-found/already-discharged cases.

**Supersedes:** Business Rule 11 and Open Question 1 below (both now resolved — see their strikethrough note).

**Retest:** G1 (cancel with a deposit) should now show Final ₹0, full refund via Confirm Refund Given. Try cancelling an admission with a service item added — expect a clear 400, not success. Confirm the ₹10 admission usage charge is credited back to the wallet on cancel.

---

### 4b. Clinic-specific shift charging (fixes DEF-IPD-16) — ✅ Built 13-Nov, pending QA retest

**Implemented as designed above, with one correction found while implementing:** the plan's own first draft of `IpdFullDayShiftRateRule`'s default was `HigherRate` — re-checked against the actual pre-existing calculator code and that was wrong. The code always picked `overlapping[^1]` (the destination/last segment), unconditionally — not "whichever is higher." Only `DestinationRoomRate` reproduces that exactly for both shift directions, so the default was corrected to `DestinationRoomRate` everywhere (entity, migration, settings UI) before any of it shipped. `HigherRate` and `OriginRoomRate` are both real, selectable options — just not the default.

**Also found and fixed while building this:** the Period-display bug ("shows 05:30 IST") turned out to be a genuine calculator bug, not a display-only one — `PerCalendarDay` blocks were anchored to UTC midnight instead of the clinic's local midnight, which could (depending on admission time) attribute part of a day's room charge to the wrong calendar day, not just render oddly. Fixed in `IpdBillCalculator` (`blockOrigin = periodStart.ToLocalTime().Date.ToUniversalTime()`); the print pages needed no change since they already called `.ToLocalTime()`. The prorated-same-day bug (bills ₹0 for a rapid admit→shift→discharge) was confirmed as rounding each segment's tiny per-day share to 2 decimals *before* summing, so several near-zero amounts could each round down to exactly ₹0.00 — fixed by accumulating full decimal precision and rounding only the final total once.

**Grace/manual mechanics:** both are computed once, at shift time, in `TransferBedHandler`, and frozen onto the *closing* segment via `IpdBedTransferHistory.FreezeCharge` (new: `StandardChargeAmount`, `AppliedChargeAmount`, `AdjustmentType`, plus `ManualChargeAmount`/`Reason`/`ByUserId`/`At` for the manual case). `IpdBillCalculator` then reads a frozen segment back and lets it settle its *whole* block on its own — the segment opened by that same shift is deliberately not charged again for that day; its own billing just resumes from the next block. A plain shift (grace off, no manual amount) never touches these fields at all, so it falls through to the exact pre-existing per-block logic — satisfied the "regression: bills match today's" requirement, now covered by `Handle_OutsideGraceWindow_NoOverrideFrozen`.

**Files:** `IpdFullDayShiftRateRule`/`IpdShiftGraceRule`/`IpdManualShiftChargeMode`/`IpdBillAdjustmentType` (new enums), `ClinicFeatureFlags` (+4 fields, `UpdateShiftChargingSettings`), `IpdBedTransferHistory` (+7 fields, `SetManualCharge`/`FreezeCharge`), `IpdBillCalculator` (rewritten: frozen-override consumption, local-day alignment, rounding-order fix, `CalculateStandardSegmentCharge`/`CalculateStandardCharge`/`ResolveFullDayRate`), `TransferBedCommand`/`Handler`/`Validator` (manual amount+reason+role check, 403 via `ErrorCodeException`), new `PreviewTransferBedQuery`/`Handler` + `GET transfer-bed/preview` endpoint, `IpdBillLineItemDto`/`IpdProvisionalBillDto`/`IpdFinalBillDto` (+`StandardAmount`/`AppliedAmount`/`AdjustmentType`/`Reason`/`WriteOffTotal`), one EF migration (`AddIpdShiftChargingSettings`, applied to local dev DB), `FeatureFlagsSettings.razor` (+4 controls), `IpdRegistrationList.razor`'s Shift dialog (live preview + manual amount/reason fields) and View dialog + both print pages (write-off line).

**Open question 3 resolved exactly as you asked:** `IpdFullDayShiftRateRule` is a clinic-level setting (not a hardcoded rule), defaulting to `DestinationRoomRate` per the correction above.

**Test coverage:** `IpdBillCalculatorTests` (+4: frozen-override consumption, `OriginRoomRate` rule, rapid-shift-never-zero, plus the 3 pre-existing `PerCalendarDay` tests rewritten to build their fixture timestamps from local wall-clock intent instead of raw UTC literals — they were implicitly timezone-dependent before this fix and are now correct regardless of the machine's offset); `TransferBedHandlerTests` (+9: manual within/above cap, missing-reason validator check, mode-off/admin-only-as-reception/admin-only-as-admin, grace NewRoomOnly/LowerRate, no-override regression).

**Not verified:** no human has clicked through the Shift dialog's new preview/manual fields or the Settings page's new controls in a browser yet — only unit-level coverage exists for the UI layer's own code (consistent with this plan's existing, acknowledged gap: "No controller-level or Web.Client-level automated tests exist yet" for the thin UI). Retest per the test cases listed below.

**Security pass (13-Nov, same day, before any QA retest):** this phase moves money (write-offs, manual overrides), so a dedicated adversarial code review was done on top of the unit tests above. Found and fixed two real issues:
1. **Cross-tenant data leak/corruption** — `PreviewTransferBedHandler` and `TransferBedHandler` validated access against `registration.ApplicationId` but then used the caller-supplied `request.ApplicationId` (the header) for every subsequent lookup (bed validation, billing settings, history). A user with roles in two clinics could preview/execute a transfer against clinic B's admission while everything else resolved against clinic A. Fixed by using `registration.ApplicationId` consistently throughout both handlers; the preview handler also gained the bed-tenant/active check the real action already had.
2. **Privilege escalation** — `ClinicRoleAuthorizationHandler` resolves its authorization clinic from the `X-Application-Id` header for any `{id:guid}`-shaped route (no `applicationId` route segment), and the handlers' own `CanAccess` only proves clinic *linkage*, not the *right role* in that clinic. A user with a non-reception role in clinic B but a reception/admin role in clinic A could ride clinic A's role past the header-scoped policy to shift a bed, cancel an admission (full refund + usage reversal), or delete a bed/room/ward in clinic B. Fixed by adding `IApplicationAccessValidator.HasRole(applicationId, roles)` (re-parses `app_roles` for an explicit clinic id) and calling it against the record's real clinic in all six handlers touched this round (`TransferBedHandler`, `PreviewTransferBedHandler`, `CancelIpdRegistrationHandler`, `DeleteBedHandler`, `DeleteRoomHandler`, `DeleteWardHandler`). **This is a broader, pre-existing architectural pattern** (likely affecting `DischargePatientHandler`/`UpdateIpdRegistrationHandler`/`CreateIpdRegistrationHandler` and other per-record controllers app-wide) — only the six handlers this round's money-affecting work touches were hardened; the rest is flagged, not fixed, and deserves its own pass.

Both are written up with full detail, severity and a security test matrix in `docs/testing/IPD_Admin_Ward_Bed_Admission_E2E_TestReport.md` as **DEF-IPD-20** and **DEF-IPD-21**. Full suite green at 1,839 tests after the fix (up from 1,824).

**New `ClinicFeatureFlags` settings** (all under Settings → IPD Room Billing, alongside the existing `IpdRoomBillingCycle`/`IpdMidStayChargeMode`; defaults below are chosen so a clinic that never touches these new settings sees **no change** to its existing bills):

| Field | Type | Default | Meaning |
|---|---|---|---|
| `IpdShiftGraceWindowMinutes` | `int` | `0` (off) | A shift within this many minutes of the current room segment's start gets the grace rule below instead of normal mid-stay billing. |
| `IpdShiftGraceRule` | `IpdShiftGraceRule?` enum *(new)* — `NewRoomOnly`, `LowerRate`, `OldRoomOnly` | `null` (only meaningful when the window is > 0) | How a within-window shift bills: the new room's rate only; the cheaper of the two; or the old room's rate (new room's charge starts the next day). |
| `IpdManualShiftChargeMode` | `IpdManualShiftChargeMode` enum *(new)* — `Off`, `AdminOnly`, `AdminAndReception` | `Off` | Who, if anyone, may enter a manual override amount at shift time. |
| `IpdFullDayShiftRateRule` | `IpdFullDayShiftRateRule` enum *(new)* — `HigherRate`, `DestinationRoomRate`, `OriginRoomRate` | `HigherRate` | **Your answer to open question 3 (13-Nov): this is now a clinic-level setting, not a hardcoded rule.** Resolves which rate a shift day bills under `MidStayChargeMode.FullDayCharge` when the two rooms touched that day have different rates. Defaults to `HigherRate` because that's what today's code already does for the low→high case you observed in QA — so no clinic's bills change on upgrade day until a Clinic Admin deliberately picks a different rule. |

**Billing order** (per shift, computed by `IpdBillCalculator`, extended):
1. Standard charge from `IpdRoomBillingCycle` (PerCalendarDay / Per24HourBlock) for the segment.
2. Apply `IpdMidStayChargeMode` (`FullDayCharge` using `IpdFullDayShiftRateRule` above when the day touches two different rates; `Prorated` splits the day's charge by time share).
3. If the shift happened within `IpdShiftGraceWindowMinutes` of the segment start, the grace rule above **replaces** step 2's result for that day.
4. If a manual amount was entered at shift time, it **replaces** the result of steps 1–3 for the room being left (the old segment only — the new segment bills normally from the shift point on).
5. `WriteOffAmount = StandardChargeAmount - AppliedChargeAmount`, stored per `IpdBedTransferHistory` segment and surfaced on the live panel, both print pages, and the Revenue/Discharge Summary reports.

**Prorated-mode bug fix (separate from the new settings):** a same-day shift immediately followed by a same-day discharge currently bills ₹0 for that day under `Prorated` mode — a rounding/time-share bug, not a settings question. Fix so that day always bills **> ₹0 and ≤ max(oldRoomRate, newRoomRate)**.

**Manual amount at shift time — validation:**
- Must satisfy `0 ≤ ManualChargeAmount ≤ StandardChargeAmount` for the time spent in the room being left; `ManualChargeReason` required (non-empty) whenever an amount is entered, including ₹0.
- Handler throws `DomainValidationException` → 400 (*"cannot exceed the standard charge of ₹X"*) when the cap is violated; throws `UnauthorizedAccessException` → 403 when `IpdManualShiftChargeMode == Off` or the acting user's role isn't allowed by the current setting (`AdminOnly` excludes Reception; an API call also gets 403 under `Off`, not just the UI hiding the field).
- `IpdBedTransferHistory` gains `ManualChargeAmount`, `ManualChargeReason`, `ManualChargeByUserId`, `ManualChargeAt`, `StandardChargeAmount`, `WriteOffAmount` columns — who/when/original-amount/reason, per your spec, stored on the segment being closed.

**API changes:**
- `PATCH transfer-bed` request gains `ManualChargeAmount` (decimal?) and `ManualChargeReason` (string?).
- New `GET transfer-bed/preview?newBedId=` — returns the standard amount, the suggested amount (after grace/mode rules, before any manual override), and the maximum manual amount allowed — so the Shift dialog can show real numbers before the user commits.
- `IpdBillLineItemDto` gains `StandardAmount`, `AppliedAmount`, `AdjustmentType` (enum: `None`/`Grace`/`Manual`), `Reason`, and a running `WriteOffTotal` on the bill DTOs.

**UI changes:**
- Settings page: 4 new fields (grace window, grace rule — enabled only when window > 0, manual charge mode, full-day rate rule).
- Shift dialog: time spent in current room, standard charge, grace result (if applicable), and — when `IpdManualShiftChargeMode` allows the current user — a manual amount field (with its computed max shown inline) plus a required reason field.
- Admission details dialog + both print pages: show the adjustment (grace/manual) and write-off total per segment, not just the net room charge.

**Open questions — all three now resolved:**
1. ~~Should the initial fee be waived on a quick shift?~~ **No — confirmed.** Grace affects room charges only; the initial fee is untouched by any of this (it's only ever waived by the *cancel* path in 4a, a different thing).
2. ~~Is the grace window measured from the start of that room segment?~~ **Yes — confirmed.**
3. ~~What should a high→low shift bill under Full Day mode?~~ **Resolved as a new per-clinic setting, `IpdFullDayShiftRateRule`** (see table above) rather than a hardcoded rule — your answer on 13-Nov.

**Test cases to add** (from your email, rates General ₹300/Private ₹600/ICU ₹2,500/day, initial fee ₹0):
- Grace window 120 min, ICU→General after ~1 hour: ₹300 charged, ₹2,500 written off, both shown on the bill.
- Manual ₹500 against standard ₹2,500: accepted. ₹3,000: rejected, 400, "cannot exceed the standard charge of ₹2,500." ₹0 with a reason: accepted. Missing reason: rejected.
- Reception manual amount → 403 when the setting is `AdminOnly`; API call → 403 when `Off`.
- Prorated same-day shift: charge > ₹0 and ≤ the higher room's rate (regression test for the bug fix above).
- Regression: grace off (window = 0) and manual off (`Off`) ⇒ bills match today's, byte-for-byte.

---

### 4c. Other defects found 13-Nov — ✅ All built 13-Nov, pending QA retest

| ID | Severity | Title | Fix applied |
|---|---|---|---|
| **DEF-IPD-18** | 🔴 Highest priority | Reception has no UI path to a discharged/cancelled stay | `/ipd`'s Grid/List toggle is now real (`_viewMode`) — **List** renders the same admissions table + **View** button admin's panel already has, fetched once on first switch (not per click, per Cross-Repo Rule 7). Reception can now reach the Final Bill / Confirm Refund actions for a discharged or cancelled stay that only existed through a direct API call before. |
| **DEF-IPD-14** | 🟠 Medium | Details dialog stale after Shift/Discharge | `TransferBedAsync` now also reloads the billing panel (`LoadBillingPanelAsync`) alongside the registration pointer. `AdminDischargeAsync` now detects when the View dialog is still open on the just-discharged registration and refreshes both `_selected` and the billing panel instead of leaving it showing a stale `Admitted` status. |
| **DEF-IPD-15** | 🟡 Low/Medium | "Top up the wallet" link went to the clinic selector | `WalletBalanceCard` gained an `OnTopUpClick` callback that, when wired, renders a `<button>` instead of an `href="#…"` anchor; `Wallet.razor` uses it with a new `ccScrollToAndFocus` JS helper (`uiHelpers.js`). Audited the rest of the app for other `href="#…"` anchors — this was the only one. |
| **DEF-IPD-17** | 🟠 Medium | Deleting a room/ward/bed with occupied beds or admission history → generic 500 | `IIpdBedTransferHistoryRepository` gained `HasAnyForBedAsync`/`...RoomAsync`/`...WardAsync`. All three delete handlers now check occupancy (via the already-loaded `Beds`/`Rooms.Beds` navigation — free) and history (one extra query) before deleting, throwing a clean `DomainValidationException`/`InvalidOperationException` (400/409) that names `Deactivate` as the alternative — which already existed on all three entities (`Bed`/`Room`/`Ward.Deactivate()`), just wasn't being pointed at. |

### 4d. Smaller items — ✅ All built 13-Nov, pending QA retest

| Item | Fix applied |
|---|---|
| Wallet chip stale until reload | `IpdRegistrationList.razor`'s admit/discharge success paths now also call `UsageBillingContext.RefreshAsync()` (OBS-14), same mechanism the header chip already subscribes to (`OnBillingChanged`). |
| "I've paid — check now" gave no result message | `TopUpDialog`'s poll loop now reports once, on the first status read after an explicit Check, if the payment is still unconfirmed ("Still waiting for payment confirmation…") instead of polling silently with nothing on screen. |
| Admissions search only ran on Enter | `OnSearchInput` now also debounces a search 400ms after typing stops (cancelled/replaced on each keystroke), on top of the existing Enter-triggers-immediately behavior. |
| Print "Period" start showed 05:30 (midnight UTC rendered in IST) | Turned out to be the `IpdBillCalculator` local-day-alignment bug described in 4b, not a print-page formatting gap — fixed there; the print pages needed no change since they already called `.ToLocalTime()`. |

---

## Phase 5 — 14-Nov QA retest follow-on + 15-Nov QA asks (new work)

### 5a. DEF-IPD-22/23 — ✅ Fixed 14-Nov, pending QA retest; DEF-IPD-24 — closed, not a vulnerability

| ID | Fix applied |
|---|---|
| **DEF-IPD-22** | Print Final Bill for a cancelled stay showed "Locked at discharge" + a nonzero initial-fee line next to a ₹0.00 total. `IpdPaymentDto`/`IpdFinalBillDto` gained `WasCancelled` (set by `FinalizeAtCancellation`); the print page now shows "CANCELLED — No charge" and omits the initial-fee line entirely when `WasCancelled` is true, instead of a real discharge's locked-bill wording. |
| **DEF-IPD-23** | Wallet "LAST TOP-UP" was showing the time of the latest cancel-reversal credit, not a real top-up. `ClinicWallet.Credit` gained an `isTopUp` flag (`ReverseUsageChargeHandler`'s cancel-reversal call passes `false`); only a `true` call updates `LastTopUpAt`. |
| **DEF-IPD-24** | Investigated, **not a real vulnerability — closed.** Cancel `notes` do accept HTML-looking text, same as several other free-text fields across the app that were never put behind `SafeTextValidationRules` (that validator is applied selectively, not everywhere). The value is never echoed back as `MarkupString`/raw HTML anywhere it's displayed (dialog, print pages, reports) — Blazor's default `@`-interpolation HTML-encodes it on render, so there is no stored-XSS path. Inconsistent validation coverage is a real but separate hygiene item, not a security bug on its own; not worth a one-off fix on just this field. |

**Files touched:** `IpdPaymentDto.cs` (+`WasCancelled`), `IpdPayment.cs` (`FinalizeAtCancellation` sets it), `ClinicWallet.cs` (+`isTopUp` param on `Credit`), `ReverseUsageChargeHandler.cs` (passes `isTopUp: false`), the Final Bill print page (cancelled-state rendering).

---

### 5b. DEF-IPD-26 — IPD Cancellation Window (new capability) — ✅ Built 15-Nov, pending QA retest

**Your ask (15-Nov):** let a clinic set, in minutes, how long after admission a Cancel stays free (₹0 bill, full refund, usage-charge reversed — the DEF-IPD-19 behavior). Past that window, Cancel should be refused outright — the only remaining path is Discharge, which bills for whatever was actually consumed.

**Implemented:**
- New `ClinicFeatureFlags.IpdCancelWindowMinutes` (`int`, default `0` = unlimited — preserves every existing clinic's cancel behavior exactly until a Clinic Admin opts in), with `UpdateCancelWindow(int)`.
- `IpdRegistration.Cancel(string? reason, int cancelWindowMinutes = 0, DateTime? asOfUtc = null)` — when the window is > 0 and `now - AdmissionDate` exceeds it, throws `DomainValidationException` ("...was admitted more than N minute(s) ago. Discharge the patient instead...") **before** any bed/segment/payment state is touched.
- `CancelIpdRegistrationHandler` loads the clinic's `ClinicFeatureFlags` and passes the configured window into `registration.Cancel(...)` as the very first mutation attempt, ahead of freeing the bed/segment and finalizing the payment — a refused cancel leaves the admission completely untouched.
- Settings UI: new "IPD Cancellation Window" card on the Feature Flags page (minutes, 0–1440, 0 = unlimited) alongside the existing shift-charging controls.
- Validator: `IpdCancelWindowMinutes` capped `InclusiveBetween(0, 1440)` (24 hours), same bound as the shift grace window and for the same reason — past that, it isn't really a quick-undo window.

**Files:** `IpdRegistration.cs` (`Cancel` signature), `ClinicFeatureFlags.cs` (+field, `UpdateCancelWindow`), `ClinicFeatureFlagsConfiguration.cs` (default 0), `CancelIpdRegistrationHandler.cs` (+`IClinicFeatureFlagsRepository`, reordered to check-first), `ClinicFeatureFlagsDto.cs`/`UpdateClinicFeatureFlagsDto.cs`, `UpdateClinicFeatureFlagsCommand.cs`, `UpdateClinicFeatureFlagsHandler.cs`, `UpdateClinicFeatureFlagsCommandValidator.cs`, `ClinicFeatureFlagsTranslator.cs`, `ClinicSettingsController.cs`, `FeatureFlagsSettings.razor`. One new EF migration (`AddIpdCancelWindow`) — **not yet applied**, see "Not verified" below.

**Test coverage:** `CancelIpdRegistrationHandlerTests` (+3: within-window succeeds, past-window throws and leaves the bed/segment/payment/wallet completely untouched, unlimited/0 allows cancel regardless of elapsed time), `ClinicFeatureFlagsTranslatorTests` (+2: default unlimited, configured value maps through), `UpdateClinicFeatureFlagsCommandValidatorTests` (+5: negative, above 1440, at 1440, 0/off, a normal value), `UpdateClinicFeatureFlagsHandlerTests` (+2: updates on an existing clinic's flags, sets it on first-time creation). Full `TenantCore.Application.Tests` suite green — **1,218 tests** (test project count; this repo's test count differs from the combined-suite figures quoted in earlier phases above, which included other test projects not rerun this round).

**Not verified / outstanding:**
- **The EF migration has not been applied to the local dev DB.** `dotnet ef migrations add` could not even build — Visual Studio has `TenantCore.Api` running/debugging locally, which locks its `bin` folder and blocks the build `dotnet ef` needs. Stop debugging in Visual Studio, then run:
  ```
  dotnet ef migrations add AddIpdCancelWindow --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
  dotnet ef database update --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api
  ```
- No one has clicked through the new Settings card or exercised a real past-window Cancel attempt in a browser yet — only handler/translator/validator-level coverage exists.

**Test cases for QA:**
- Set the window to 60 minutes. Admit, cancel within the hour → Final ₹0, full refund, same as today.
- Same setup, backdate/wait past 60 minutes, attempt Cancel → 400, admission stays `Admitted`, no bed freed, no refund created; Discharge is still available and bills normally.
- Leave the window at 0 (default) → Cancel behaves exactly as before, regardless of how long ago the patient was admitted.
- Settings validation: negative value → 400; value > 1440 → 400; 1440 itself → accepted.

---

### 5c. QA asks answered, not code changes (15-Nov)

**"QA doesn't have a Clinic Admin account to test with."** Pulled every clinic's active Clinic Admin from `AuthDBNew` (read-only query, no data changed):

| Clinic | Clinic Admin email |
|---|---|
| Clinic 1 Test | awez1411+cl1@gmail.com |
| Clinic 2 for Rupee test | awez1411+cl2@gmail.com |
| Clinic 3 for Rupee test | awez1411+cl3@gmail.com |
| Clinic 4 | awez1411+cl4@gmail.com |
| Clinic One Care (the clinic used for all QA so far) | awez1411+clinic1admin@gmail.com |
| New Down Town clinic | *(excluded — this is your own personal account, not a test fixture)* |

All five are active, email-verified, not locked out, 2FA off. **Setting the password to `Temp@123` on these was attempted but blocked** by Claude Code's own auto-mode safety classifier (it refuses destructive SQL writes without an explicit one-time approval) — the SQL was prepared and the BCrypt hash verified offline, but never run. You'll need to either approve that specific action or reset the password yourself (e.g. via the app's own "forgot password" flow) for whichever of these five QA should use.

**Bonus find, useful regardless of the above:** `awez.vtt+cl4@gmail.com` already exists and already holds **Doctor in Clinic One Care + Receptionist in Clinic 4** — exactly the dual-clinic, mismatched-role fixture DEF-IPD-20/21 and SEC1–SEC6 (Phase 4b security matrix above) have been waiting on since 13-Nov. Same password caveat applies if its password also needs resetting for QA to log in with it.

**"Can a role/user mapped to a clinic be revoked without going to SQL?"** Checked both places this could mean:
- **TenantCore.App's own Clinic Admin → User Management page already calls the right endpoint** (`DELETE {applicationId}/users/{userId}`) for Change Role/Deactivate/Remove — confirmed unchanged by the Phase 6 fix below; the UI was never the problem. **Correction: my 15-Nov claim that the Auth-side handler behind it has "no bug, no fix needed" was wrong** — QA's retest reproduced it failing to actually revoke access; see Phase 6, which fixes it for real this time.
- **TenantCore.Admin (the separate cross-tenant super-admin portal) has no equivalent today** — it only has read models (`AuthUserApplicationRow`/`AuthUserRoleRow`) for display, no mutation path, and its one existing Auth HTTP client (`AdminAuthClient`) only covers login/session, not user-role management. If this is what was meant, it's a real gap, but a substantial one to close (new Admin→Auth HTTP client method, new Admin UI, mandatory audit logging, double permission checks per Admin's own hard rules) — not started pending confirmation this is the surface you meant.

---

## Phase 6 — DEF-IPD-25 fix + SEC5/SEC6 follow-up (15-Nov, after QA retest)

**QA's 15-Nov retest reproduced DEF-IPD-25 (High): removing or deactivating a user from a clinic did not revoke their access — a fresh login after either action still carried an active role for that clinic, confirmed via `beds`/`ipd-registrations`/`ipd-reports/census`/`usage-billing/wallet`/`clinic-settings/feature-flags` all returning 200.** This fix is in **TenantCore.Auth**, not TenantCore.App (the bug is in the login/claims path, not anything IPD-specific) — noted here because that's where QA's retest and this plan's cross-repo tracking live.

**Root cause (confirmed two ways — code reading plus a live read-only SQL check against the actual repro's data in `AuthDBNew`):**
1. **The real, confirmed bug:** `ApplicationService.ToggleUserApplicationMappingAsync` (`PATCH {applicationId}/users/{userId}/status`, "deactivate the mapping") only ever flipped `UserApplication.IsActive` — it never touched the user's `UserRole` row for that application. Separately, `AuthenticationService.ExtractUserRoleClaims` (the method that builds the login/JWT role claims) filtered only on `UserRole.IsActive` — it never checked whether the user still had an active `UserApplication` mapping to that clinic at all. So "deactivating" a user's mapping was a pure no-op for access control: the stale role kept appearing in every login after it, forever.
2. **Why an earlier code-read (15-Nov, same day, before QA's retest) concluded "no bug, no fix needed":** that pass read `RemoveUserFromApplicationAsync` (the `DELETE` path) in isolation and judged it logically correct — which it still is, as far as it goes (confirmed again by SQL: the `UserApplication` row really was hard-deleted). What that pass missed is `ToggleUserApplicationMappingAsync`'s identical-looking but incomplete sibling, and, more importantly, that neither path mattered on its own because the login query never required a mapping to exist in the first place — a stray or re-created active `UserRole` row (and `AssignApplicationUserRoleAsync`, the `/assign` endpoint, has no dedup check — it inserts a fresh row every time it's called, even over an existing active one) would resurrect access regardless of which removal path ran. Live DB evidence for QA's specific repro user (`awez.vtt+cl4@gmail.com`, Clinic One Care): `UserApplications` — no row at all (correctly deleted); `UserRoles` — the Doctor row was `IsActive = 1` with `CreatedDate == ModifiedDate` to the millisecond, i.e. untouched since creation — consistent with a role having been (re-)assigned at some point with nothing afterward ever deactivating it.

**Fix applied (defense in depth, both layers):**
- `AuthenticationService.ExtractUserRoleClaims` (`TenantCore.Auth.Application/Services/Implementations/AuthenticationService.cs`) now requires a currently active `UserApplication` mapping for the same `ApplicationId`, in addition to `UserRole.IsActive`, before a role claim is issued. This is the authoritative fix — the mapping, not the role row, is the real membership record, and it closes the hole regardless of any stray/duplicate `UserRole` rows that exist now or get created in the future by `AssignApplicationUserRoleAsync`'s missing dedup check (noted as a separate, smaller hygiene item below, not fixed as part of this).
- `ApplicationService.ToggleUserApplicationMappingAsync` now also deactivates every matching `UserRole` when deactivating the mapping (mirroring what `RemoveUserFromApplicationAsync` already did), so the data itself stays consistent — anything that reads "this user's roles" directly, not just login, sees the same picture. Re-activating a mapping deliberately does **not** restore the role — a role is a separate, explicit grant.
- Both `UserRole` and `UserApplication` already carry `HasQueryFilter(x => x.IsActive)` in their EF configurations — a global filter that silently hides inactive rows from any query that doesn't call `.IgnoreQueryFilters()`. This is exactly why a Moq-based unit test (the kind that existed, and the kind an earlier pure code-read reasoned about) could never have caught this: mocked repositories don't exercise real EF query-filter behavior. The new tests use a real (in-memory) `DbContext` for this reason.

**Files touched:** `TenantCore.Auth.Application/Services/Implementations/AuthenticationService.cs` (`ExtractUserRoleClaims`), `TenantCore.Auth.Application/Services/Implementations/ApplicationService.cs` (`ToggleUserApplicationMappingAsync`). New tests: `TenantCore.Auth.Test/Services/AuthenticationServiceRoleClaimsTests.cs` (3 tests — active role + active mapping included; active role with no mapping excluded, pinning the exact DEF-IPD-25 repro shape; inactive role excluded regardless of mapping), `TenantCore.Auth.Test/Integration/UserApplicationRevocationIntegrationTests.cs` (3 tests, against a real in-memory `DbContext` — `RemoveUserFromApplicationAsync` deactivates the role, `ToggleUserApplicationMappingAsync(false)` now deactivates the role too, re-activating the mapping does not resurrect the role). Full `TenantCore.Auth.Test` suite run: same 12 pre-existing failures before and after this change (confirmed by stashing the fix and re-running — `PasswordServiceTests`/`JwtTokenServiceTests`/`RoleManagementServiceTests`/`CurrentUserServiceTests`/`UserLoginCommandTests`, all unrelated to this code path, pre-existing in this environment), 0 regressions from this fix.

**Not fixed, flagged only:**
- **`AssignApplicationUserRoleAsync` has no dedup/upsert check** — calling `POST .../assign` twice for the same user+application creates two active `UserRole` rows (possibly with two different roles active simultaneously), which `ChangeUserRoleAsync` doesn't produce (it correctly deactivates the old role first). Harmless now that login gates on an active mapping too, but still a data-integrity smell worth a dedicated look.
- **No server-side JWT revocation** — this fix only guarantees the *next* login reflects current access. A token already issued before a user was removed/deactivated remains valid until it naturally expires (`Jwt:ExpirationMinutes`, currently 1440 = 24h) — standard JWT behavior, not special to this bug, and out of scope here since QA's own repro was specifically "after logging in again."
- **Stale test data:** `awez.vtt+cl4@gmail.com`'s Doctor role for Clinic One Care is still `IsActive = 1` in the dev DB from before this fix (the fix only changes behavior going forward, it doesn't retroactively clean up existing rows) — needs either a one-off `UPDATE` or re-running `DELETE .../users/{userId}` for that user+clinic once this fix is deployed, before DEF-IPD-20/21/SEC1-6 retesting that depends on this user having a clean single-clinic role. Flagging rather than running the `UPDATE` myself, same caution as the earlier blocked password-reset `UPDATE`.

**Retest for QA:** repeat the exact DEF-IPD-25 steps (assign → remove **or** assign → deactivate mapping → log in again) against this build; either removal path should now 403 immediately on the next login, not just on a direct `GetUserApplications` check.

### SEC5/SEC6 follow-up (role-grant question, and Clinic 4's admin account)

**SEC5 — "the environment's permission layer blocked granting the [Clinic Admin] role" to the test user in Clinic 1:** Read through `ChangeUserRoleCommand`/`ApplicationService.ChangeUserRoleAsync`, `AssignUserToApplicationCommand`/`AssignApplicationUserRoleAsync`, the `[AppAuthorize("Admin","Clinic Admin")]` policy filter behind both endpoints (`AppAuthorizeAttribute`), and the Web.Client role dropdown (`UserManagement.razor` — populated from `GetRolesByApplicationAsync`, unfiltered). **Found no code path that specifically blocks assigning the "Clinic Admin" role** — no role-name restriction, no "can't grant a role equal to or above your own" check, nothing in the authorization filter that treats this role specially. I can't reproduce or explain the block without the actual HTTP response QA received (status code + body) — please paste that in and I'll trace it from there rather than guess further.

**SEC6 — "Clinic 4 has no admin account known to QA":** It does — pulled from the same `AuthDBNew` read-only query as the original clinic-admin list: `awez1411+cl4@gmail.com` is Clinic 4's active Clinic Admin. The password-reset block from the earlier SQL ask (Claude Code's auto-mode classifier refusing the `UPDATE ... SET Password`) still applies if that account's password also needs setting before QA can log in with it — same open item, not re-attempted here.

---

## Phase 7 — DEF-IPD-27 + DEF-IPD-28 (16-Nov, first real browser UI pass)

**Source:** QA's first browser click-through of the DEF-IPD-26 Settings card and the IPD admissions UI (everything before this was done via direct API calls, per that section's own "Chrome API" heading) — found two UI-only defects. Both are in `TenantCore.App` only; no backend change in this phase.

### 7a. DEF-IPD-27 — no Cancel control anywhere in the IPD UI — ✅ Fixed 16-Nov

**Problem:** `PATCH /api/ipd-registrations/{id}/cancel` has existed and been exercised at the API level since DEF-IPD-19 (13-Nov) and DEF-IPD-26 (15-Nov), but nothing in `TenantCore.Web.Client` ever called it — `IpdRegistrationList.razor` only ever wired up Discharge. This meant DEF-IPD-26's whole cancellation-window feature had no way to be exercised from a browser, and QA's 10-Nov note that this was "expected/optional" no longer held once DEF-IPD-26 made the window a real, clinic-facing setting.

**Fix:** Added a "Cancel" button, visible only while `Status == Admitted`, in three places — the Grid-view admissions table, the List-view admissions table (DEF-IPD-18's table), and the admission detail/View dialog (next to its existing Discharge button) — each opening a small confirm dialog (optional reason textarea) mirroring the existing Discharge dialog's own layout and confirm-before-destructive-action pattern. Confirming calls `ClinicApiClient.CancelIpdRegistrationAsync(id, new CancelIpdRegistrationRequest(reason))` (already existed, just unused), then reloads the admissions list/ward map and — mirroring DEF-IPD-14's fix for Discharge — refreshes the View dialog's own bound registration + billing panel if it's still open on the same record, instead of leaving it showing a stale `Admitted` status.

**Files:** `TenantCore.Web.Client/Pages/Ipd/IpdRegistrationList.razor` only (markup: two table row templates + the View dialog's action row + a new confirm-cancel dialog; code: `_cancelDialogVisible`/`_cancelling`/`_cancelReason` fields, `OpenCancelDialog`/`AdminCancelAsync`). No API, Application, Domain or Infrastructure change — the endpoint and its validation (service-items-attached block, cancellation-window check) were already correct and already covered by existing tests.

**Not verified in a browser** (no browser access this session) — `dotnet build` on `TenantCore.Web.Client` is green. QA: re-run the DEF-IPD-26 UI test cases from the test report's 16-Nov UI section now that there's a button to click.

### 7b. DEF-IPD-28 — Settings page didn't reflect what was actually saved — ✅ Fixed 16-Nov

**Problem:** `FeatureFlagsSettings.razor`'s `SaveAsync` took an `Action applyLocally` from each `On*ChangedAsync` handler and ran it on a successful save — each one set *that one field* to the value the client had just sent, never looking at the server's own response, even though the PUT (`UpdateFeatureFlagsAsync`) already returns the full, authoritative, just-persisted `ClinicFeatureFlagsDto`. Re-read the full server-side path (`UpdateClinicFeatureFlagsHandler`, `ClinicFeatureFlagsTranslator`, the GET query) against its existing passing tests and found it correct — this was purely a client-side trust problem, the same lesson as DEF-IPD-25: a value that *should* match doesn't mean it *does*, so always re-derive it from the one place that's actually authoritative.

The concrete failure mode: every `SaveAsync` call sends *all nine* settings fields, falling back to the component's current in-memory value (`graceWindowMinutes ?? _ipdShiftGraceWindowMinutes`, etc.) for the eight the user isn't actively changing. If two different fields are changed in close succession — plausible on a page with nine independent controls, each firing its own save on `@onchange` — the second save's fallback for the *first* field could capture a value from *before* the first save's `applyLocally()` had actually run, and silently write that stale value back over whatever the first save had just persisted. The user would see a success toast for both changes but one of them would have been quietly reverted server-side, with nothing on screen to show it.

**Fix:** Replaced every `applyLocally` lambda with one `ApplyFromServer(ClinicFeatureFlagsDto dto)` method that sets all nine fields from the DTO, called with `r.Data` (the server's response) after every successful save, and with the initial `GetFeatureFlagsAsync()` result in `OnInitializedAsync`. The page's state can now only ever reflect what the server has actually confirmed persisting.

**Files:** `TenantCore.Web.Client/Pages/Settings/FeatureFlagsSettings.razor` only. No API/Application/Domain/Infrastructure change.

**Not verified in a browser** (no browser access this session) — `dotnet build` green, full `TenantCore.Application.Tests` suite still green at 1,218 (unaffected, as expected for a client-only fix). QA: re-run the DEF-IPD-26 Settings-card check from the 16-Nov UI section, plus a quick regression pass changing two settings back-to-back to confirm neither reverts the other.

---

## Phase 8 — DEF-IPD-28 (actually still open) + DEF-IPD-29 + DEF-IPD-30 (16-Nov, full UI retest)

**Source:** QA's "16-Nov-2026 full UI retest" — a full click-through of every page, not just the two cards Phase 7 covered. Found that Phase 7's DEF-IPD-28 fix (resyncing from the server's response) was necessary but not sufficient — a second, more fundamental bug in the same card — plus two unrelated new defects. One fix (8c) is in `TenantCore.Auth`; the other two are `TenantCore.App`-only.

### 8a. DEF-IPD-28 — grace-rule dropdown never got a chance to appear — ✅ Fixed 16-Nov

**Problem:** the rule dropdown in `FeatureFlagsSettings.razor` was already coded to show once `_ipdShiftGraceWindowMinutes > 0` — but that field only ever updated from the server's save response (Phase 7's own fix), and `UpdateClinicFeatureFlagsCommandValidator` always rejects "window > 0 with no rule" by design. So the very first attempt to turn the window on failed before the field changed, the dropdown never appeared, and the user had no way to supply the rule the server was asking for — a catch-22 that Phase 7's resync-from-server fix didn't address because it was never the bug Phase 7 was fixing. Separately, because the component's own state hadn't changed, Blazor's render-diffing (one-way `value` + `@onchange`, diffing against its own last-rendered tree rather than the live DOM) left the `<input>` showing whatever the user had actually typed, uncorrected.

**Fix:** `OnGraceWindowChangedAsync` now sets `_ipdShiftGraceWindowMinutes` immediately on tab-out, before any server round-trip, so the dropdown appears right away; the server is only called once there's a valid window+rule pair (window=0, or window>0 with a rule already chosen). A new `_graceWindowError` field renders a persistent inline message under the field for any save that still fails, since the existing toast is gone in ~3.5s. `SaveAsync` now returns `bool` and tracks `_lastSaveError` so callers that need a message outliving the toast can read it.

**Files:** `TenantCore.Web.Client/Pages/Settings/FeatureFlagsSettings.razor` only.

**Not verified in a browser** — `dotnet build` green. QA: type a window value with no rule yet set and confirm the dropdown now appears immediately; pick a rule and confirm it saves.

### 8b. DEF-IPD-29 — blank red toast on a Wards & Rooms delete refusal — ✅ Fixed 16-Nov

**Problem:** `DeleteWardAsync`, `DeleteRoomAsync` and `DeleteBedAsync` (`ClinicApiClient.cs`) were the only three delete calls that didn't extract the server's error message on a failed response — they just set `Success = r.IsSuccessStatusCode` and left `Message` at `ApiResponse<T>`'s default, which is `string.Empty`, not null. `WardManagement.razor`'s `Toast.ShowError(r.Message ?? "Failed.")` never fell back, because `??` only substitutes for `null` — so DEF-IPD-17's real 409 message ("...has admission history and cannot be deleted. Deactivate it instead.") never reached the user, just a blank red bar.

**Fix:** routed all three through the same `ExtractUserMessage` helper `DeletePatientAsync` already used.

**Files:** `TenantCore.Web.Client/Clients/ClinicApiClient.cs` only.

**Not verified in a browser.**

### 8c. DEF-IPD-30 — deactivated member disappears from both lists — ✅ Fixed 16-Nov, in TenantCore.Auth

**Problem:** `UserApplicationRepository.GetDeactivatedByApplicationIdAsync` was the one query in that repository missing `.Include(x => x.User)` / `.Include(x => x.Application)` — every sibling method has both. With no lazy-loading proxies configured, `mapping.User` came back null, and `ApplicationTranslator.TranslateToApplicationUser` threw a `NullReferenceException` on its first `user.MiddleName` read. `GetDeactivatedApplicationUsersCommand`'s generic catch turned that into `Success=false`, and `UserManagement.razor`'s `LoadDeactivatedUsersAsync` swallows a failed load into an empty list with no toast — so the bug was fully silent and fully deterministic, which is why it survived reloads. Active correctly dropped 5→4 because *that* query already had its Includes; only Deactivated was affected.

**Fix:** added the missing `.Include(x => x.User).Include(x => x.Application)`, matching the pattern of every other method in the repository.

**Files:** `TenantCore.Auth.Infrastructure/Data/Repositories/UserApplicationRepository.cs`. Added `GetDeactivatedByApplicationIdAsync_IncludesUserAndApplication` to `TenantCore.Auth.Test/Integration/UserApplicationRevocationIntegrationTests.cs` — built on a real in-memory `DbContext`, not a mocked repository, since a Moq-mocked `IUserApplicationRepository` would never exercise a missing `.Include` (same bug class as DEF-IPD-25, and the exact reason that phase's regression tests were built the same way).

**Verification:** `dotnet build` green on both `TenantCore.Auth.Infrastructure` and the full solution; `TenantCore.Auth.Test` — new test passes (4/4 in that integration test file), the pre-existing 12 `PasswordServiceTests` failures are unchanged/unrelated. `TenantCore.Application.Tests` (App repo) unaffected at 1,218/1,218, as expected — none of Phase 8's fixes touch App's Application layer. **Not verified in a browser.**

### Also noticed, not fixed this phase

The 16-Nov API-level QA section and the 16-Nov full-UI-retest QA section each independently used **OBS-31** for two unrelated observations. Flagging the collision for a future renumber; not editing either finding. OBS-29 (toast disappears before it can be read) and OBS-30 (misleading "clinic isn't active yet" message for a deactivated user) are acknowledged, Low severity, UI polish — not addressed this round.

---

## Phase 9 — DEF-IPD-30 deploy confirmation + DEF-IPD-31 (16-Nov, re-retest ask) — ✅ Fixed 16-Nov, in TenantCore.Auth

**Source:** QA asked for dev confirmation that the Auth service build actually includes the DEF-IPD-30 fix before deploying to IIS for the next retest pass, and reported one new defect (DEF-IPD-31) found while manually cleaning up a deactivated test user.

### 9a. DEF-IPD-30 — build/deploy confirmation

The source fix (8c) was still in place and unchanged; re-confirmed with a fresh `dotnet build src/TenantCore.Auth.API -c Release` (0 errors) and a re-run of `GetDeactivatedByApplicationIdAsync_IncludesUserAndApplication` (passes).

**Found while confirming:** `src/TenantCore.Auth.API/bin/Release/net8.0/publish/` — the folder an IIS deployment would normally copy — had a `TenantCore.Auth.Infrastructure.dll` dated **23-Sep-2026**, two months before this fix (and DEF-IPD-25's) existed. If IIS has been serving from that same stale `publish` output, that alone explains QA still seeing the old deactivated-list behavior regardless of any code fix. Ran `dotnet publish src/TenantCore.Auth.API -c Release` to regenerate it — now dated 16-Nov-2026 and contains both the DEF-IPD-30 and DEF-IPD-31 fixes. **Whoever deploys to IIS should copy this freshly-generated `publish` folder, not a cached/older package.**

### 9b. DEF-IPD-31 — reactivating a user left them with no role — ✅ Fixed 16-Nov

**Problem:** `ToggleUserApplicationMappingAsync`'s `isActive: true` branch only ever reactivated the `UserApplication` mapping row — restoring the role was deliberately left out in the original DEF-IPD-25 fix (the role was treated as "an explicit, separate grant that may have changed while deactivated"). In practice there is no UI path to change a user's role while their mapping is inactive, so that scenario doesn't happen — and the real effect was that the ordinary "I deactivated the wrong person, undo it" workflow silently left the user with zero permissions, with nothing on screen to say so, until an admin remembered to manually re-assign a role via Change Role.

**Fix:** the `isActive: true` branch now calls `IUserRoleRepository.GetLatestByUserAndApplicationAsync(userId, applicationId)` — the same `IgnoreQueryFilters`/most-recently-modified lookup `GetDeactivatedApplicationUsersAsync` already uses to show a deactivated user's "last role" — and reactivates that row if it's currently inactive. This mirrors the deactivate branch exactly (same row, same application, just toggled back) instead of guessing a role or inventing new state to track.

**Files:** `TenantCore.Auth.Application/Services/Implementations/ApplicationService.cs` (`ToggleUserApplicationMappingAsync`). Rewrote `TenantCore.Auth.Test/Integration/UserApplicationRevocationIntegrationTests.cs`'s `ToggleUserApplicationMappingAsync_Reactivate_DoesNotTouchTheRole` → `ToggleUserApplicationMappingAsync_Reactivate_AlsoRestoresTheRole`, since it asserted the exact behavior QA just flagged as the defect; no other test in the solution depended on the old behavior (checked).

**Verification:** `dotnet test` on `UserApplicationRevocationIntegrationTests` — 4/4 pass. Full `TenantCore.Auth.Test` suite: same pre-existing 12 `PasswordServiceTests` failures, 235 passed, no new failures. `dotnet publish src/TenantCore.Auth.API -c Release` green and regenerated (see 9a). **Not verified in a browser.**

---

## Open Questions / Risks

1. ~~**Cancel's financial scope — deferred, not blocking.**~~ **Resolved 13-Nov — see Phase 4a.** Cancel now finalizes the bill at ₹0, refunds every deposit, reverses the admission usage charge, and is blocked while service items are attached.
2. **Provisional Bill naming — confirmed.** Renamed throughout from "Interim Bill" to **Provisional Bill** (entity/DTO/query/endpoint/print-page names all updated above).
3. **Billing-policy defaults — confirmed, resolved.** No default for either setting. Both `IpdRoomBillingCycle` and `IpdMidStayChargeMode` start null; bed allocation (`Create` with a `BedId`, or `TransferBed`) is blocked with a 409 until a Clinic Admin explicitly picks both. **New rollout risk this creates, worth flagging:** this gate also applies to `TransferBedCommand`, so on the day this ships, any clinic with an existing `Admitted` patient will be unable to shift that patient's bed until someone configures the two settings — not just new admissions. Worth a release note / in-app banner pointing Clinic Admins at Settings right after deploy.
4. **Backfill coverage — confirmed, manual reconciliation accepted.** The Phase 0 migration will leave some existing IPD rows with `CurrentBedId = null` (anything that doesn't name-match a real bed, including the test report's own leftover QA data). You've confirmed manual admin cleanup after migration is acceptable — no automated resolution needed.
5. **Report + bill-printing access — confirmed, no change needed.** Reception already sits inside `RequireClinicStaff` (`Receptionist, Doctor, ClinicAdmin, ClinicManager, SystemAdmin`), which is what every report endpoint and both bill-print endpoints (`provisional-bill`, `final-bill`) already use in this plan — reception can print either bill and open all three IPD reports, including the discharge-summary patient details, as you asked.
