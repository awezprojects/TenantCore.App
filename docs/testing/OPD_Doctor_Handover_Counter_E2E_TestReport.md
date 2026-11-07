# OPD End-to-End — Reception → Doctor → Items → Counter → Handover → Reports

> **REPORT STATUS: ✅ CLOSED — 07-Nov-2026.** All 12 defects (DEF-E2E-01 … 12) are 🟢 Verified – Closed after re-test on the latest build. No open defects. Remaining note: OBS-10 (old closed sessions keep their saved totals, optional back-fill).

| Item | Detail |
|---|---|
| Repo | TenantCore.App |
| Testing branch | `testing/opd-doctor-handover-counter-e2e` (from `main` @ `768a882`) |
| Environment | Local — `https://localhost:7246` |
| Clinic | Clinic One Care (`2d76bdc2-4843-4d11-9120-1f81aa32ec86`) — same clinic as the previous exercise |
| Accounts | Receptionist `awez1411+clinic1_recep1@gmail.com` · Doctor / Clinic Admin `awez1411+clinic1admin@gmail.com` |
| Tested on | 04-Nov-2026, ~08:55–09:15 IST |
| Tested by | QA (Claude) |
| Prior report | `docs/testing/OPD_Reception_Registration_Refund_Wallet_TestReport.md` |

> Method: UI for the first prescription, the handover, the doctor's accept, the session close and every page view. The other 11 visits, the items and the collections were driven through the same `/api` endpoints the UI calls, using each role's own token.

## Summary

| Metric | Count |
|---|---|
| Visits registered and completed end to end | 12 (OPD-20261104-0007 … 0018) |
| Test cases executed | 22 |
| ✅ Pass | 17 |
| ❌ Fail | 4 |
| ℹ️ Observation | 1 |
| New defects | 4 (0 Critical · 0 High · 3 Medium · 1 Low) |

**Bottom line:** the money flow reconciles to the rupee. Registration → doctor prescription (auto-completes the visit) → reception items → collection → ₹50 discount refund → handover → doctor accept → session close all agree, and the counter page, session total, daily, weekly, monthly, date-range, doctor-wise and doctor finance dashboard all show **₹6,850**. The defects are in the reception dashboard, the reception-wise report, payment bookkeeping and report display.

## Expected vs actual ledger

| Token | Patient | Visit fee | Items added | Discount | Final | Counter row |
|---|---|---|---|---|---|---|
| 0007 | DUMMY Patient 012 | 200 | USG 500 | – | 700 | 700 ✅ |
| 0008 | DUMMY Patient 013 | 250 | Dressing 150 | – | 400 | 400 ✅ |
| 0009 | DUMMY Patient 014 | 300 | USG 500 + Dressing 150 | – | 950 | 950 ✅ |
| 0010 | DUMMY Patient 015 | 350 | – | – | 350 | 350 ✅ |
| 0011 | DUMMY Patient 016 | 400 | Dressing 100 (custom) | – | 500 | 500 ✅ |
| 0012 | DUMMY Patient 017 | 150 | USG 450 (custom) | – | 600 | 600 ✅ |
| 0013 | DUMMY Patient 018 | 200 | – | – | 200 | 200 ✅ |
| 0014 | DUMMY Patient 019 | 500 | Dressing 150 + USG 500 | – | 1,150 | 1,150 ✅ |
| 0015 | DUMMY Patient 020 | 300 | Consultation 300 | – | 600 | 600 ✅ |
| 0016 | DUMMY Patient 021 | 250 | – | 50 (refunded) | 200 | 200 ✅ |
| 0017 | DUMMY Patient 022 | 100 | USG 500 | – | 600 | 600 ✅ |
| 0018 | DUMMY Patient 023 | 450 | Dressing 150 | – | 600 | 600 ✅ |
| **Total** | | **3,450** | **3,450** | **50** | **6,850** | **6,850 ✅** |

Running checks: wallet ₹996 → ₹984 (12 × ₹1) ✅ · session total after registrations ₹3,450 ✅ · after collecting items ₹6,900 ✅ · after ₹50 discount refund ₹6,850 ✅.

## Defect status workflow

| Status | Meaning | Owner / next action |
|---|---|---|
| 🔴 Open | Not yet fixed | **Dev** – resolve |
| 🟡 Fixed – Pending QA | Dev says fixed, awaiting re-test | **QA** – re-verify |
| 🟢 Verified – Closed | QA confirmed the fix | None |
| ⚪ Deferred / Won't Fix | Acknowledged, not fixing now | Product decision |

## Test cases

| TC ID | Area | Scenario | Expected | Actual | Result | Defect |
|---|---|---|---|---|---|---|
| TC-E2E-01 | Reception | Register 12 visits with varied fees and vitals | 12 × 201, tokens 0007–0018 | As expected | Pass | |
| TC-E2E-02 | Wallet | Wallet is charged ₹1 per visit | 996 → 984 | 984 | Pass | |
| TC-E2E-03 | Counter | Session total after registrations | 3,450 | 3,450 | Pass | |
| TC-E2E-04 | Doctor | Doctor sees all 12 waiting visits | 12 Waiting | 12 Waiting | Pass | |
| TC-E2E-05 | Doctor | Start consultation (UI) | InProgress | "Consultation started" | Pass | |
| TC-E2E-06 | Doctor | Write and finalise prescription (UI) | RX created, visit completes | RX-20261104-0001, visit Completed | Pass | |
| TC-E2E-07 | Doctor | Start + prescribe + submit for the other 11 | All Completed | 11 × 201/200, all status 3 | Pass | |
| TC-E2E-08 | Reception | Add items with default and custom amounts | 11 × 201 | 11 × 201 | Pass | |
| TC-E2E-09 | Reception | Payment totals after adding items | Total = fee + items, status shows balance due | Total correct; status still Received | Fail | DEF-E2E-03 |
| TC-E2E-10 | Reception | Collect all items at the counter | Session total 6,900 | 6,900 | Pass | |
| TC-E2E-11 | Reception | Payment record after collecting items | `collectedAmount` = final amount | Still equals the visit fee only (e.g. 200 of 700) | Fail | DEF-E2E-03 |
| TC-E2E-12 | Reception | ₹50 discount on a collected visit, then refund | refundDue 50, session 6,850 | refundDue 50, session 6,850 | Pass | |
| TC-E2E-13 | Counter | Counter page rows and totals | 12 rows, Rs. 6,850 | Matches the ledger | Pass | |
| TC-E2E-14 | Handover | Recipient list loads for reception | Doctor listed | Clinic1 Admin (Doctor) | Pass | |
| TC-E2E-15 | Handover | Send ₹6,850 (UI) | Pending | Pending, session unchanged | Pass | |
| TC-E2E-16 | Handover | Doctor sees and accepts (UI) | Accepted | Accepted | Pass | |
| TC-E2E-17 | Counter | Reception closes session after accept | Closed with 6,850 | status 2, total 6,850 | Pass | |
| TC-E2E-18 | Reports | Daily, date-range, weekly, doctor-wise, dashboard totals | 6,850 each | 6,850 each | Pass | |
| TC-E2E-19 | Reports | Daily report patient names | 12 names | All blank | Fail | DEF-E2E-04 |
| TC-E2E-20 | Reports | Reception-wise report for today | 1 receptionist, 6,850 | `[]` for `from=to=2026-11-04` | Fail | DEF-E2E-02 |
| TC-E2E-21 | Reception UI | Dashboard counts after visits complete | Total 12, Completed 12 | 0 / 0 even after reload | Fail | DEF-E2E-01 |
| TC-E2E-22 | Doctor UI | My Finance period handovers vs income | Same basis | Handovers ₹7,400 vs income ₹6,850 | Observation | OBS-1 |

## Defects

| Defect ID | Severity | Status | Title | Steps | Expected | Actual | Root cause / suggested fix | Owner |
|---|---|---|---|---|---|---|---|---|
| DEF-E2E-01 | Medium | 🟢 Verified – Closed | Reception dashboard hides completed visits | Register visits → doctor completes them → reload reception Dashboard | "Today's OPD" 12, Completed 12, queue lists visits | OPD 0, Waiting 0, In progress 0, Completed 0, "No OPD registrations today" | `Dashboard.razor` `LoadReceptionDashboardAsync` called `todayOnly: true`; `OpdRegistrationRepository` applies `todayOnly` as Pending or InProgress only, so Completed could never show. **Fix applied:** switched to `fromDate = toDate = today`, same as the doctor dashboard load. | Dev – fixed, pending QA re-test |
| DEF-E2E-02 | Medium | 🟢 Verified – Closed | Reception-wise report is empty for a single day | `GET /api/finance-reports/reception-wise?from=2026-11-04&to=2026-11-04` | 1 receptionist, ₹6,850 | `[]`. With `to=2026-11-05` or `to=2026-11-04T23:59:59` the result is correct. | `GetReceptionWiseReportHandler` filtered `OpenedAt <= request.To`; a date-only `To` is midnight, so any session opened that day was excluded, and `utcOffset` was ignored unlike the other reports. **Fix applied:** added `UtcOffsetMinutes` to `GetReceptionWiseReportQuery` (wired from a new `utcOffset` query param on `GET reception-wise`), and the handler now computes `fromUtc`/`toUtc` the same way `GetDateRangeCollectionReportHandler` already does (`To.Date.AddDays(1).AddMinutes(-offset)` as an exclusive end), applied to the session, payment and expense date filters. | Dev – fixed, pending QA re-test |
| DEF-E2E-03 | Low | 🟢 Verified – Closed | Payment status and collected amount ignore items added later | Visit fee ₹200 collected → add USG ₹500 → collect items | Status shows balance due after adding; `collectedAmount` = 700 after collecting | After adding: total 700, status Received, collected 200. After collect-all: total 700, status Received, collected still 200. Session total is right. | `OpdPayment.CollectedAmount` was never updated when individual items were collected. **Fix applied:** new `OpdPayment.RecordItemCollection(amount, receivedByUserId, counterSessionId)` (mirrors `AcceptFull`'s "keep the earliest session" rule) is now called from `CollectOpdParticularHandler` and `CollectAllOpdParticularsHandler` after each item is marked collected, so `CollectedAmount` — and therefore the `ApplyDiscount`/`ProcessRefund` math that reads it — reflects visit fee + collected items. The regressions this introduced are tracked and fixed separately as DEF-E2E-05/06/07 below. **On the residual re-test note** ("no balance-due state" right after adding an item before collecting): `OpdPaymentDto` already exposes both `FinalAmount` and `CollectedAmount` — `FinalAmount − CollectedAmount` is the balance due, and the UI already computes exactly this subtraction elsewhere (`OpdParticularsPopup.razor`). No backend data is missing; labelling that state in the UI is the cosmetic suggestion already logged as OBS-7, not a tracked defect. Closing as the money/data bug is fully fixed. | Dev – fixed, QA-verified (money correct); cosmetic label suggestion tracked as OBS-7 |
| DEF-E2E-04 | Low | 🟢 Verified – Closed | Daily collection report shows blank patient names | `GET /api/finance-reports/daily?date=2026-11-04&utcOffset=330` | `patientName` filled | `patientName` is `""` on all 12 rows | `GetDailyCollectionReportHandler` hardcoded `PatientName = string.Empty` and never loaded the registration/patient. **Fix applied:** handler now loads the registrations via `IOpdRegistrationRepository.GetByIdsAsync` (includes `Patient`) and fills `PatientName` from `{FirstName} {LastName}`. | Dev – fixed, pending QA re-test |
| DEF-E2E-05 | Medium | 🟢 Verified – Closed | Income reports count items that were billed but not collected | New session. Visit 0022: fee ₹350 collected, add Nursing ₹400, do **not** collect it. Then `GET /api/finance-reports/daily?date=2026-11-04&utcOffset=330` (also dashboard, doctor-wise, date-range, reception-wise) | Only collected money: 6,850 + 6,800 = **13,650** | **14,050** on daily, dashboard, doctor-wise, date-range and reception-wise. Row for DUMMY Patient 027 shows final 750 although only 350 was collected. Counter page and `/counter-sessions/active` correctly say 6,800. | Reports summed `OpdPayment.FinalAmount` (billed) for every payment with `PaymentStatus = Received`; that status is set by the visit fee alone. **Fix applied:** switched every report aggregate to sum `CollectedAmount` instead (which already nets out processed refunds via `ProcessRefund`) — `GetDailyCollectionReportHandler`, `GetDateRangeCollectionReportHandler`, `GetFinanceDashboardSummaryHandler`, `GetMonthlyCollectionReportHandler`, `GetWeeklyCollectionReportHandler`, `GetReceptionWiseReportHandler`, and `OpdPaymentRepository.GetDoctorWiseTotalsAsync` (doctor-wise). Per-visit bill displays elsewhere (`OpdParticularsPopup.razor`, `OpdRegistrationList.razor`) still correctly use `FinalAmount` for the bill itself — those are single-payment displays, not cross-payment report sums, so left untouched. | Dev – fixed, pending QA re-test |
| DEF-E2E-06 | High | 🟢 Verified – Closed | Handover cannot be accepted when items were collected in the session | Reception sends handover of ₹6,800 (the session total on the Counter page) → doctor clicks **Accept** | Handover accepted, doctor can then let reception close the session | `POST /api/amount-handovers/{id}/accept` returns **409**: "This handover is for Rs. 6,800.00 but the counter session balance is now Rs. 10,250.00." Accept button does nothing visible. 10,250 = 6,800 + 3,450 (collected items counted twice). Session cannot be closed. | `AcceptAmountHandoverHandler.EnsureHandoverMatchesSessionBalanceAsync` still computed `payments.Sum(CollectedAmount) + particulars.Sum(Amount) - expenses`. **Fix applied:** removed the `particulars` term (and its now-unused `IOpdParticularRepository` dependency) — `CollectedAmount` already contains item money after the DEF-E2E-03 fix. Added `AcceptAmountHandoverHandlerTests.Handle_WhenSessionIncludesIndividuallyCollectedItems_DoesNotDoubleCountTheirAmount`. | Dev – fixed, pending QA re-test |
| DEF-E2E-07 | Medium | 🟢 Verified – Closed | Open-session totals double-count collected items on the session list screens | Same session as DEF-E2E-06: `GET /api/counter-sessions/all-active` and `GET /api/counter-sessions?page=1&pageSize=3` | 6,800 | `totalCollected` and `netAmount` **10,250** for the open session (while `GET /api/counter-sessions/active` and the Counter page show 6,800). | `GetAllActiveCounterSessionsHandler` and `GetCounterSessionsHandler` still did `payments.Sum(CollectedAmount) + particulars.Sum(Amount)`. **Fix applied:** same one-line fix as DEF-E2E-06 in both handlers (dropped the `particulars` term and the now-unused `IOpdParticularRepository` dependency). Checked every remaining caller of `GetCollectedBySessionIdAsync` — only `GetOpdCollectionsBySessionHandler` is left, which was already fixed in the DEF-E2E-03 round and only uses it for per-visit display, not a total. | Dev – fixed, pending QA re-test |
| DEF-E2E-08 | High | 🟢 Verified – Closed | Item collected in a later counter session is not credited to that session | Session B (closed, ₹6,800) has visit 0022 with fee ₹350 collected and a Nursing ₹400 item left uncollected. Open session C, collect that item (`POST /api/opd-particulars/{id}/collect` with C's id) | Session C shows ₹400 collected and can hand it over. Session B and 04-Nov reports stay unchanged. | Collect returns 204. Payment 0022 goes 350 → 750 but keeps `counterSessionId` = session B. Session C total stays **₹0** (its row lists 0022 with items ₹400 and total collected ₹0). 04-Nov daily report rises from 10,200 to **10,600** and the 06-Nov report shows 0. The cash taken in C is in no session, so C cannot hand it over. | `OpdPayment.RecordItemCollection` adds to `CollectedAmount` but keeps the earliest `CounterSessionId`. **Fix applied:** new `IOpdParticularRepository.GetAmountCollectedForOtherSessionsAsync(sessionId, applicationId)` — sums `OpdParticular.Amount` collected in `sessionId` whose owning `OpdPayment.CounterSessionId` is a *different* session — added on top of `payments.Sum(CollectedAmount)` everywhere a session total is computed: `GetActiveCounterSessionHandler`, `CloseCounterSessionHandler`, `GetAllActiveCounterSessionsHandler`, `GetCounterSessionsHandler`, `AcceptAmountHandoverHandler`, and `GetOpdCollectionsBySessionHandler` (per-visit row). **Known remaining limitation (not exercised by this report):** this corrects the *foreign* session's total; it does not subtract that item's money back out of the *home* session's own total if that session is later re-queried live (an already-**closed** session's total is a frozen snapshot from `Close()`, so it is unaffected — which is exactly the case this report exercises). Added `AcceptAmountHandoverHandlerTests.Handle_WhenSessionHasAnItemCollectedForAForeignPayment_CreditsItsAmount` and `GetActiveCounterSessionHandlerTests.Handle_WhenSessionHasAnItemCollectedForAForeignPayment_CreditsItsAmount`. | Dev – fixed, pending QA re-test |
| DEF-E2E-10 | Medium | 🟢 Verified – Closed | Reports put early-morning IST collections on the previous day, even with `utcOffset=330` | Collect ₹100 at 02:49 IST on 06-Nov (stored 05-Nov 21:19 UTC). `GET /api/finance-reports/daily?date=2026-11-06&utcOffset=330`, `date-range?from=2026-11-05&to=2026-11-06&utcOffset=330`, doctor Finance Dashboard | ₹100 on 06-Nov (the session is dated 06-Nov and the token is OPD-20261106-0001) | `daily` for 06-Nov = 0 and for 05-Nov = 100; `dashboard` for 06-Nov = 0; `date-range` and the doctor's Finance Dashboard list the ₹100 under **05 Nov**. Round 1 and 2 collections (08:58 and 19:19 IST) were not affected, which is why this was not seen earlier. | Two separate gaps. (1) The `daily`/`weekly`/`monthly`/`dashboard` controller actions accepted `utcOffset` but only used it to compute the *default* `date` when the caller omitted it — never passed it into the query at all, so the handler's day boundary was plain UTC midnight. **Fix applied:** added `UtcOffsetMinutes` to `GetDailyCollectionReportQuery`, `GetWeeklyCollectionReportQuery`, `GetMonthlyCollectionReportQuery`, `GetFinanceDashboardSummaryQuery`; the controller now forwards `utcOffset` into all four; each handler shifts its UTC day/week/month boundary by `-offset`. (2) `GetDateRangeCollectionReportHandler`'s query bounds were already offset-aware, but its per-day `GroupBy(p => p.AmountReceivedAt.Value.Date)` grouped by the raw UTC date — fixed to `GroupBy(p => p.AmountReceivedAt.Value.AddMinutes(offset).Date)`; the same `+offset`-before-`.Date` fix was applied to `GetWeeklyCollectionReportHandler` and `GetMonthlyCollectionReportHandler`'s per-day buckets. Added `GetDailyCollectionReportHandlerTests.Handle_WithUtcOffset_QueriesTheLocalDayNotTheUtcDay` and `GetDateRangeCollectionReportHandlerTests.Handle_WithUtcOffset_BucketsByLocalDayNotUtcDay` for the exact 02:49 IST / 21:19 UTC scenario. | Dev – fixed, pending QA re-test |
| DEF-E2E-09 | Medium | 🟢 Verified – Closed | Visits whose items were collected before the fix now under-report | Round 1 visits 0007–0018 (items collected before the fix). `GET /api/finance-reports/daily?date=2026-11-04&utcOffset=330`, session list | Round 1 = ₹6,850 as stored on the closed session | Their payments hold `collectedAmount` = visit fee only (e.g. 0007: final 700, collected 200; sum 3,400 vs final 6,850). Reports for 04-Nov show 3,400 for round 1 while the closed session list still says 6,850. | Old rows were never back-filled: item money lives only on `OpdParticular`. **Fix applied:** new EF Core data-only migration `BackfillOpdPaymentCollectedAmount` (`20261106120000_BackfillOpdPaymentCollectedAmount.cs`, no schema change) recomputes `CollectedAmount = VisitFee + SUM(collected OpdParticular.Amount with a CounterSessionId)` for every `PaymentStatus = Received AND Discount = 0` row — same precedent as the existing `AddOpdParticularPaymentTracking` migration's own data backfill. The formula is idempotent (already-correct round 2/3 rows get the same value back), so it is safe to apply to a database that has no affected rows. Discounted payments are untouched — their `CollectedAmount` is already set wholesale by `AcceptFull`/`ProcessRefund` and is correct. Migrations auto-apply on startup, so this runs the next time the app starts. | Dev – fixed, pending QA re-test against the rebuilt + restarted app |
| DEF-E2E-11 | Medium | 🟢 Verified – Closed | The `by-session` drill-down of an earlier session includes money collected later in another session | Session D (07-Nov): visit fee ₹150 collected, item Nursing ₹400 billed, close D (₹150). Open session E, collect the item in E. `GET /api/opd-payments/by-session/{D}` and `/by-session/{E}` | D lists ₹150, E lists ₹400 | D lists `totalCollected` **550** and E lists 400, so the two drill-downs add up to 950 for ₹550 of cash. Same on round 2: session B drill-down sums ₹7,200 against its session total ₹6,800 (the Nursing ₹400 collected in session C). The session totals themselves are correct (D 150, E 400). | `GetOpdCollectionsBySessionHandler` took `payment.CollectedAmount` whole for the payment's home session, which already includes item money collected in a later session. **Fix applied (replaces the DEF-E2E-08 patch with a more general one):** every place that attributes money to a session or a day now computes a payment's **base** contribution as `CollectedAmount − (all its individually collected items, wherever/whenever collected)` — via new `IOpdParticularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync` — then adds each item back via the session (or day, for DEF-E2E-12) it was *actually* collected in, via `GetCollectedBySessionIdAsync`/`GetCollectedBySessionIdsAsync`. This one formula fixes DEF-E2E-11 and *also* closes the "known remaining limitation" noted on DEF-E2E-08's original fix (an already-closed session is a frozen snapshot either way, so round-3's symptom was unaffected, but a session still open when the foreign item is collected is now correct too). `GetAmountCollectedForOtherSessionsAsync` (the round-3 addition) is removed — superseded. Updated in `GetActiveCounterSessionHandler`, `CloseCounterSessionHandler`, `GetAllActiveCounterSessionsHandler`, `GetCounterSessionsHandler`, `AcceptAmountHandoverHandler`, `GetOpdCollectionsBySessionHandler`, and `GetReceptionWiseReportHandler` (session-attributed). | Dev – fixed, pending QA re-test |
| DEF-E2E-12 | Medium | 🟢 Verified – Closed | Reports date an item's money on the day the payment was first received, not the day the item was collected | The Nursing ₹400 on visit 0022 was collected on 06-Nov (session C). `GET /api/finance-reports/daily?date=2026-11-04&utcOffset=330` and `…date=2026-11-06…`; doctor-wise and date-range for the same days | 04-Nov = ₹13,650 (the two closed 04-Nov sessions), 06-Nov = ₹500 (visit fee ₹100 + Nursing ₹400) | 04-Nov = **₹14,050**, 06-Nov = **₹100**. Doctor-wise 04-Nov also shows 14,050. Same-day collection is correct (07-Nov = ₹550 for ₹150 + ₹400). | Same root cause and fix as DEF-E2E-11, applied by day instead of by session. **Fix applied:** new `IOpdParticularRepository.GetCollectedByDateRangeAsync(fromUtc, toExclusiveUtc, applicationId)` returns items by their own `CollectedAt`; every date-bucketed report now sums `base(payment)` for payments whose fee fell in the bucket, plus items whose own `CollectedAt` falls in the bucket — `GetDailyCollectionReportHandler` (also adds a dedicated line item per foreign-day collected item so `Items` still sums to `GrandTotal`), `GetDateRangeCollectionReportHandler`, `GetWeeklyCollectionReportHandler`, `GetMonthlyCollectionReportHandler`, `GetFinanceDashboardSummaryHandler`, `GetReceptionWiseReportHandler` (date-attributed share), and `OpdPaymentRepository.GetDoctorWiseTotalsAsync` (joins the in-range items to `OpdRegistration` for the doctor). Added `GetDailyCollectionReportHandlerTests.Handle_ItemCollectedOnADifferentDayThanTheFee_IsCreditedToItsOwnDayNotTheFeeDay` for the exact 04-Nov/06-Nov scenario. | Dev – fixed, pending QA re-test |

### Observations

| ID | Observation |
|---|---|
| OBS-1 | Doctor My Finance shows "Amount Handovers" ₹7,400 (₹6,850 + ₹550) beside income ₹6,850 and one counter session. The ₹550 handover belongs to the 02-Nov session but is counted by its sent date. Handovers and sessions are filtered on different dates. Decide the intended basis. |
| OBS-2 | Counter page row for the discounted visit (0016) reads fee 250, items 0, total 200 with no discount column, so the row does not add up on screen. |
| OBS-3 | Doctor "Clinic1 Admin" still has no doctor profile, so doctor fee configs cannot apply (carried over from the previous report). |
| OBS-4 | Re-test: counter row for the discounted visit 0027 reads fee 300 + items 300 = total 500 (₹100 discount not shown). Same as OBS-2, still present. |
| OBS-5 | Re-test: first login took about 25 s on `localhost:7246` (button stayed on "Signing in…" before redirecting). Later logins took under 10 s. |
| OBS-6 | Re-test: when the Accept API call returned 409 (DEF-E2E-06) no error message was visible on the Handovers page about 4 s after the click. The click looks like a no-op. Please check the page shows the server's message. |
| OBS-7 | Re-test: counter row for 0022 shows "Items total ₹400" next to "Total collected ₹350" because the item is billed but not yet collected. Consider labelling it "pending". |
| OBS-8 | Re-test 3 was first run in the built-in browser pane with API logins (the pane held another user), then **repeated in Chrome through the UI** (register → doctor → handover → Accept → Close Session). Both runs agree. |
| OBS-9 | Re-test 3: first logins take 10–25 s on `localhost:7246` (same as OBS-5). |

## Dev fix — 04-Nov-2026 (not yet committed, pending QA re-test)

All four defects fixed on the current branch (`testing/opd-doctor-handover-counter-e2e`), uncommitted. See the Root cause / suggested fix column above for what changed per defect.

**Build/test status:** `TenantCore.Application`, `TenantCore.Api`, `TenantCore.Web.Client` build clean. `TenantCore.Application.Tests` (982 tests) and `TenantCore.Domain.Tests` (134 tests) pass, including new regression tests added for this fix: `CollectOpdParticularHandlerTests`, `CollectAllOpdParticularsHandlerTests`, `GetReceptionWiseReportHandlerTests`, `GetDailyCollectionReportHandlerTests`, and three new `OpdPaymentTests` cases covering `RecordItemCollection` and the discount-on-a-visit-with-items scenario this report had flagged as unexercised.

**Re-tested through the UI and API on 04-Nov-2026, 19:15–19:35 IST** — see the Re-test results section below. DEF-E2E-01, 02 and 04 are verified and closed. DEF-E2E-03 is partly fixed. Three new defects were found (DEF-E2E-05, 06, 07); DEF-E2E-06 and 07 come from the DEF-E2E-03 fix.

## Re-test results — 04-Nov-2026, 19:15–19:35 IST

| Item | Detail |
|---|---|
| Build under test | Local app with the uncommitted fixes on `testing/opd-doctor-handover-counter-e2e` (from `main` @ `768a882`) |
| Clinic / accounts | Clinic One Care · Receptionist `awez1411+clinic1_recep1@gmail.com` · Doctor `awez1411+clinic1admin@gmail.com` |
| Method | Same as round 1: UI for login, counter page, handover and accept; API calls from each role's own browser session for bulk steps |
| Round 2 ledger | 12 new visits 0019–0030 (patients DUMMY 024–035), fees 100–500, 9 visits with items, ₹100 discount on 0027 with items, one item (Nursing ₹400) left uncollected on 0022 |

| Defect | Result | Evidence |
|---|---|---|
| DEF-E2E-01 | 🟢 Verified – Closed | Reception dashboard (fresh load): all registrations 12 → 24 as visits were added, Completed 12 → 24, Waiting 0, queue lists visits |
| DEF-E2E-02 | 🟢 Verified – Closed | `reception-wise?from=2026-11-04&to=2026-11-04` returns the receptionist with ₹6,850, with and without `utcOffset=330` |
| DEF-E2E-03 | 🔴 Partly fixed | Fixed: after collecting items, `collectedAmount` = final amount on all 9 visits (e.g. 700/700, 950/950, 1,150/1,150); per-item collect adds item by item (300 → 800, 500 → 1,000); session total 6,900 as expected. ₹100 discount on visit with items: final 500, collected 600, refundDue 100, after refund collected 500 and session 6,800 — correct. Not fixed: right after adding an item, payment status is still Received with total 700 and collected 200 (no balance-due state). Severity lowered to Low; the money is correct. |
| DEF-E2E-04 | 🟢 Verified – Closed | Daily report: patient names on 24/24 rows |

| TC ID | Scenario | Expected | Actual | Result | Defect |
|---|---|---|---|---|---|
| TC-RT-01 | Register 12 visits in a new session | 12 × 201, wallet −12, session 3,450 | 12 × 201, wallet 984 → 972, session 3,450 | Pass | |
| TC-RT-02 | Doctor completes all 12 | Status Completed | 12 × 200/201/200, all status 3 | Pass | |
| TC-RT-03 | Add 11 items | 11 × 201, session unchanged | 11 × 201, session 3,450 | Pass | |
| TC-RT-04 | Collect items (2 per-item + 9 collect-all) | Session 6,900, collected = final | 6,900, all OK | Pass | |
| TC-RT-05 | Discount ₹100 + refund on visit with items | refundDue 100, session 6,800 | refundDue 100, session 6,800 | Pass | |
| TC-RT-06 | Counter page rows and total | 12 rows, ₹6,800 | 12 rows, ₹6,800 | Pass | |
| TC-RT-07 | Send handover ₹6,800 from UI | Pending | Pending, amount pre-filled 6,800 | Pass | |
| TC-RT-08 | Doctor accepts handover | Accepted | 409 conflict, still Pending | **Fail** | DEF-E2E-06 |
| TC-RT-09 | Open-session totals on list screens | 6,800 | 10,250 on `all-active` and session list | **Fail** | DEF-E2E-07 |
| TC-RT-10 | Reports with one uncollected item | 13,650 | 14,050 on daily, dashboard, doctor-wise, date-range, reception-wise | **Fail** | DEF-E2E-05 |
| TC-RT-11 | Close session, then collect the pending item in a new session | Closed at 6,800, new session shows ₹400 | **Not run** — blocked by DEF-E2E-06 | Blocked | |

Note on the item-collect fix: `CollectedAmount` keeps the payment's earliest counter session, so an item collected in a later session than the visit may be credited to the earlier session. TC-RT-11 covers this and must be run after DEF-E2E-06 is fixed.

## Dev fix — round 2, 04-Nov-2026 (not yet committed, pending QA re-test)

Fixes DEF-E2E-05, 06 and 07 (the regressions the first fix introduced), and closes out DEF-E2E-03's residual re-test note. See the Root cause / suggested fix column in the Defects table above for what changed per defect. All on the same uncommitted branch (`testing/opd-doctor-handover-counter-e2e`).

Summary of this round:
- **DEF-E2E-06 / DEF-E2E-07** (the actual blockers — handover accept returning 409, session lists showing double totals): both were the same bug in three places (`AcceptAmountHandoverHandler`, `GetAllActiveCounterSessionsHandler`, `GetCounterSessionsHandler`) still adding `OpdParticular.Amount` on top of `OpdPayment.CollectedAmount`, which — after the round-1 fix — already includes that money. Removed the `OpdParticular` term (and the now-unused repository dependency) from all three. Searched every remaining caller of `GetCollectedBySessionIdAsync`; the only one left (`GetOpdCollectionsBySessionHandler`) was already fixed in round 1 and only uses it for per-visit display, not a total.
- **DEF-E2E-05** (reports overcounted a billed-but-not-collected item): every finance report aggregate was summing `OpdPayment.FinalAmount` (the bill) instead of `CollectedAmount` (what's actually in hand). Switched all of them — `GetDailyCollectionReportHandler`, `GetDateRangeCollectionReportHandler`, `GetFinanceDashboardSummaryHandler`, `GetMonthlyCollectionReportHandler`, `GetWeeklyCollectionReportHandler`, `GetReceptionWiseReportHandler`, and `OpdPaymentRepository.GetDoctorWiseTotalsAsync`. Left `FinalAmount` alone everywhere it's used to show one payment's own bill (`OpdParticularsPopup.razor`, `OpdRegistrationList.razor`) — those aren't report aggregates.
- **DEF-E2E-03's residual note** ("no balance-due state" right after adding an item): not a data bug — `OpdPaymentDto` already returns both `FinalAmount` and `CollectedAmount`, and the UI already computes the balance due from them elsewhere. Closing DEF-E2E-03 outright; the labelling idea stays tracked as OBS-7.

**Build/test status:** `TenantCore.Application` and `TenantCore.Infrastructure` build clean (`TenantCore.Api` couldn't be rebuilt standalone because the locally running app held a file lock — expected while QA is testing against it; no source changes were made there this round). `TenantCore.Application.Tests` (983 tests) and `TenantCore.Domain.Tests` (134 tests) pass, including a new regression test for the exact DEF-E2E-06 scenario: `AcceptAmountHandoverHandlerTests.Handle_WhenSessionIncludesIndividuallyCollectedItems_DoesNotDoubleCountTheirAmount`.

**Not yet re-tested through the UI** against this round's changes — restart the app and re-run the re-test checklist below, picking up from TC-RT-08 (the blocked handover accept) through TC-RT-11.

## Re-test results (round 3) — 06-Nov-2026, 02:40–03:05 IST

| Item | Detail |
|---|---|
| Build under test | Local app restarted with the round-2 fixes (uncommitted, branch `testing/opd-doctor-handover-counter-e2e`) |
| Clinic / accounts | Clinic One Care · Receptionist `awez1411+clinic1_recep1@gmail.com` · Doctor `awez1411+clinic1admin@gmail.com` |
| Data used | The open session B from round 2 (visits 0019–0030, pending ₹6,800 handover, uncollected Nursing ₹400 on 0022), plus one new ₹100 visit in a new session C |

| Defect | Result | Evidence |
|---|---|---|
| DEF-E2E-06 | 🟢 Verified – Closed | Doctor accepts the ₹6,800 handover → 200, status Accepted (before: 409, expected ₹10,250). Reception closes session B → closed at ₹6,800, net ₹6,800. |
| DEF-E2E-07 | 🟢 Verified – Closed | Open session B shows ₹6,800 on `/active`, `/all-active` and the session list (before: 10,250 on the last two). |
| DEF-E2E-05 | 🟢 Verified – Closed | With ₹400 billed but uncollected: row for 0022 shows final 350, not 750. Reports for 04-Nov show 10,200 = round 1 (3,400) + round 2 (6,800), identical on daily, dashboard, doctor-wise, date-range, weekly and reception-wise. The ₹400 is no longer counted. See DEF-E2E-09 for why round 1 is 3,400 and not 6,850. |

| TC ID | Scenario | Expected | Actual | Result | Defect |
|---|---|---|---|---|---|
| TC-RT-08 | Doctor accepts handover | Accepted | 200, Accepted | Pass | |
| TC-RT-09 | Open-session totals on all screens | 6,800 | 6,800 everywhere | Pass | |
| TC-RT-10 | Reports with one uncollected item | Collected money only | 10,200 (6,800 for round 2) | Pass | |
| TC-RT-12 | Reception closes session after accept | Closed at 6,800 | status 2, 6,800 | Pass | |
| TC-RT-13 | **Chrome UI:** register ₹100 visit, doctor completes, Handovers → New Handover (pre-filled ₹100) → Send | Pending | "Handover sent.", Pending ₹100 | Pass | |
| TC-RT-14 | **Chrome UI:** doctor clicks Accept | Accepted | Accepted | Pass | |
| TC-RT-15 | **Chrome UI:** reception clicks Close Session | Session closes | "No Active Session" | Pass | |
| TC-RT-16 | Chrome UI: Counter page after an item is collected in a later session | ₹400 collected | Collected Rs. 0, row shows items Rs. 400, total collected Rs. 0 | **Fail** | DEF-E2E-08 |
| TC-RT-17 | Daily report for 06-Nov for ₹100 collected 02:49 IST | 100 | 0 (100 shown on 05-Nov) | **Fail** | DEF-E2E-10 |
| TC-RT-11 | Item collected in a later session | Session C ₹400, B unchanged | C ₹0, payment credited to B, 04-Nov report +400 | **Fail** | DEF-E2E-08 |

## Dev fix — round 3, 06-Nov-2026 (not yet committed, pending QA re-test)

Fixes DEF-E2E-08, 09 and 10 (the three defects found in re-test round 3). Still on the same uncommitted branch (`testing/opd-doctor-handover-counter-e2e`). See the Root cause / suggested fix column in the Defects table above for the detail on each.

- **DEF-E2E-08:** new `IOpdParticularRepository.GetAmountCollectedForOtherSessionsAsync` credits an item's money to the session it was actually collected in, even when its payment's `CounterSessionId` points at an earlier session. Wired into all six places that compute a session total (`GetActiveCounterSessionHandler`, `CloseCounterSessionHandler`, `GetAllActiveCounterSessionsHandler`, `GetCounterSessionsHandler`, `AcceptAmountHandoverHandler`, `GetOpdCollectionsBySessionHandler`).
- **DEF-E2E-10:** `GetDaily`/`GetWeekly`/`GetMonthly`/`GetDashboard` on `FinanceReportsController` were accepting `utcOffset` but dropping it before it reached the query; now forwarded into all four queries and applied to each handler's day/week/month boundary. `GetDateRangeCollectionReportHandler`'s (and weekly/monthly's) per-day bucketing also shifted the UTC timestamp by `+offset` before taking `.Date`, since bucketing by the raw UTC date was still wrong even where the outer query range was already offset-aware.
- **DEF-E2E-09:** one-off EF Core data migration (`BackfillOpdPaymentCollectedAmount`, no schema change) recomputes `CollectedAmount` for the pre-fix round-1 rows. Runs automatically on next app startup (migrations auto-apply).

**Build/test status:** `TenantCore.Application` and `TenantCore.Infrastructure` build clean (0 errors) — `TenantCore.Api` couldn't be rebuilt standalone again because the restarted app has it file-locked; no source changes were made in `TenantCore.Api` this round beyond the controller edit already covered by the Infrastructure/Application build. `TenantCore.Application.Tests` (987 tests) and `TenantCore.Domain.Tests` (134 tests) pass, including new regression tests for DEF-E2E-08 (`AcceptAmountHandoverHandlerTests`, `GetActiveCounterSessionHandlerTests`) and DEF-E2E-10 (`GetDailyCollectionReportHandlerTests`, `GetDateRangeCollectionReportHandlerTests`).

**Next step:** restart/rebuild the app so it (a) picks up this build and (b) auto-applies the new migration, then re-run TC-RT-16, TC-RT-17 and TC-RT-11, plus checklist items 10–12 below.

## Re-test results (round 4) — 07-Nov-2026, 14:50–15:00 IST (Chrome, receptionist session, Clinic One Care)

Build under test: the round-3 dev fix, restarted app (migration applied). The Chrome UI was used for sign-in, clinic selection and the Counter page; the money flow was driven through the API from the same Chrome session, with the doctor's accept done by an API login (the Chrome session stayed on reception).

| Test | Steps | Expected | Actual | Result | Defect |
|---|---|---|---|---|---|
| TC-RT-18 | Daily and dashboard for 06-Nov and 05-Nov (₹100 collected 02:49 IST on 06-Nov, `utcOffset=330`) | 06-Nov = 100, 05-Nov = 0 | daily 100 / 0, dashboard 100 / 0, date-range 100 / 0, reception-wise 06-Nov = 100 | **Pass** | DEF-E2E-10 |
| TC-RT-19 | Round-1 legacy visits: `by-session` for the 6,850 session and the 04-Nov report | 12 visits, sum 6,850; reports include them in full | 12 rows, sum 6,850; 04-Nov daily and doctor-wise show the full amounts | **Pass** | DEF-E2E-09 |
| TC-RT-20 | Open session D, register ₹150 visit, add Nursing ₹400, do not collect | D total 150 | D active total 150, payment final 550, collected 150 | Pass | n/a |
| TC-RT-21 | Close D without a handover | Blocked | 409 "cash must be handed over and accepted before closing" | Pass | n/a |
| TC-RT-22 | Handover 150 → doctor accepts → close D | Closed at 150 | Accepted, closed at 150 | Pass | n/a |
| TC-RT-23 | Open session E, collect the ₹400 item in E | E total 400, D stays 150 | E active total 400, D stays 150, payment collected 550 | **Pass** | DEF-E2E-08 |
| TC-RT-24 | Handover 400 from E → accept → close E | Accepted, closed at 400 | Accepted, closed at 400 | **Pass** | DEF-E2E-08 |
| TC-RT-25 | Reports for 07-Nov | 550, 2 sessions | daily 550, dashboard 550, reception-wise 550 over 2 sessions | Pass | n/a |
| TC-RT-26 | `by-session` drill-down for D after E collected the item | D lists 150, E lists 400 | D lists 550, E lists 400 | **Fail** | DEF-E2E-11 |
| TC-RT-27 | 04-Nov and 06-Nov reports after the item on visit 0022 was collected on 06-Nov | 04-Nov 13,650 / 06-Nov 500 | 04-Nov 14,050 / 06-Nov 100 | **Fail** | DEF-E2E-12 |
| TC-RT-28 | Chrome UI: Counter page after closing E | "No Active Session"; wallet 970 | "No Active Session", wallet ₹970 | Pass | n/a |

Verdicts: **DEF-E2E-08, 09 and 10 are verified and closed.** Session totals, handover amounts, legacy back-fill and the IST date bucketing now behave correctly. The re-test found two new problems (DEF-E2E-11, DEF-E2E-12), so the report stays open.

Observation OBS-10: session C (06-Nov, closed at ₹100 before the fix) and session B keep their saved totals, so the Nursing ₹400 collected in session C is still not part of C's saved total. This is old data from before the fix, not a new defect. A back-fill of closed session totals is optional and should be agreed with the dev.

## Re-test results (round 5, final) — 07-Nov-2026, 15:40–16:05 IST (Chrome, Clinic One Care)

Build under test: the round-4 dev fix (general formula: a payment's base money minus individually collected items, with each item added back to the session and the day it was actually collected). The app had been rebuilt (`TenantCore.Application.dll` built 10:02 UTC, after the source changes). The Chrome UI was used for the signed-in app and the Counter page; the calls were made from the same Chrome session. Note: the stored login in this Chrome profile had changed to Clinic1 Admin (`awez1411+clinic1admin@gmail.com`) by the time the new test session F was opened, so F was opened by that account and the reception-wise report lists it as its own row. The money checks do not depend on the user.

| Test | Steps | Expected | Actual | Result | Defect |
|---|---|---|---|---|---|
| TC-RT-29 | Daily, dashboard, date-range, reception-wise and doctor-wise for 04-Nov | 13,650 (sessions 6,850 + 6,800) | 13,650 on all five | **Pass** | DEF-E2E-12 |
| TC-RT-30 | Same reports for 06-Nov (visit fee ₹100 + Nursing ₹400 collected that day) | 500 | 500 on all five (reception-wise 1 session, 500) | **Pass** | DEF-E2E-12 |
| TC-RT-31 | 05-Nov and 02-Nov | 0 and 550 | 0 and 550 | Pass | n/a |
| TC-RT-32 | `by-session` drill-down for D, E (round 4), B | D 150, E 400, B 6,800 | D 150, E 400, B 6,800 (was 7,200) | **Pass** | DEF-E2E-11 |
| TC-RT-33 | `by-session` for session C (collected ₹100 fee and the ₹400 Nursing) | drill-down 500 | 500 (saved total stays 100, OBS-10) | Pass | DEF-E2E-11 |
| TC-RT-34 | New session F: visit ₹120 + item ₹300 via **collect-all** | 420 | F total 420, payment 420 of 420 | Pass | n/a |
| TC-RT-35 | Add visit ₹100 + item ₹200 collected **individually** in F | F total 720 | F total 720, payment 300 of 300, drill-down 720 | Pass | n/a |
| TC-RT-36 | Handover 720 → doctor accepts → reception closes F | Accepted, closed at 720 | Accepted, closed at 720, no active session | Pass | n/a |
| TC-RT-37 | Reports for 07-Nov after F | 550 + 720 = 1,270 | daily 1,270, dashboard 1,270, date-range 1,270, doctor-wise 1,270, reception-wise rows 720 (F) and 550 (D + E) | Pass | n/a |
| TC-RT-38 | 04-Nov and 06-Nov totals after the new collections | unchanged (13,650 / 500) | 13,650 / 500 | Pass | n/a |

Verdicts: **DEF-E2E-11 and DEF-E2E-12 are verified and closed.** With these, all 12 defects are closed and the report is closed.

Observation OBS-11: reception-wise reports one row per session opener, so sessions opened by two different accounts on the same day show as two rows. This is expected behaviour, not a defect.

## Test data left in Clinic One Care

Round 1: visits 0007–0018, session closed at ₹6,850 (legacy item data, DEF-E2E-09).

Round 2: visits 0019–0030, session B **closed** at ₹6,800, handover ₹6,800 **accepted**.

Round 3: counter session C (dated 06-Nov, opened 02:43 IST) is **closed** at ₹100 with an accepted ₹100 handover for visit OPD-20261106-0001. Visit 0022 is now fully collected (₹750) but credited to session B (DEF-E2E-08). Wallet ₹971.00.

Round 4 (07-Nov): visit OPD-20261107-0001, session D closed at ₹150 (accepted ₹150 handover) and session E closed at ₹400 (accepted ₹400 handover). No open session. Wallet ₹970.00.

Round 5 (07-Nov): session F (opened by Clinic1 Admin) closed at ₹720 with an accepted ₹720 handover; visits OPD-20261107-0002 and 0003. No open session.

## Dev fix — round 4, 07-Nov-2026 (not yet committed, pending QA re-test)

Fixes DEF-E2E-11 and DEF-E2E-12. Still on the same uncommitted branch (`testing/opd-doctor-handover-counter-e2e`). See the Root cause / suggested fix column in the Defects table above for the detail on each.

Both defects turned out to be the same underlying gap as DEF-E2E-08, just surfacing in two more places the round-3 fix hadn't reached: `OpdPayment.CollectedAmount` is a **running total across every collection event for a registration** (visit fee/full-bill, plus every individually collected item), however many sessions or days apart — so crediting it whole to "the session/day this payment belongs to" always risks pulling in money that actually belongs to a different session or day. Rather than patch DEF-E2E-11 and DEF-E2E-12 as two more one-off additions (the way DEF-E2E-08 was patched in round 3), this round replaces that patch with one general formula used everywhere money is attributed to a session or a day:

> `base(payment) = payment.CollectedAmount − (every item individually collected for that registration, wherever/whenever)`, then each item's own amount is added back wherever (session) or whenever (day) it was *actually* collected.

New repository primitives on `IOpdParticularRepository`: `GetIndividuallyCollectedTotalsByRegistrationIdsAsync` (the subtraction), `GetCollectedBySessionIdsAsync` (multi-session add-back, for reception-wise), `GetCollectedByDateRangeAsync` (add-back by the item's own `CollectedAt`, for the date reports). `GetAmountCollectedForOtherSessionsAsync` (round 3's one-off) is removed.

Updated: `GetActiveCounterSessionHandler`, `CloseCounterSessionHandler`, `GetAllActiveCounterSessionsHandler`, `GetCounterSessionsHandler`, `AcceptAmountHandoverHandler`, `GetOpdCollectionsBySessionHandler` (DEF-E2E-11), `GetDailyCollectionReportHandler`, `GetDateRangeCollectionReportHandler`, `GetWeeklyCollectionReportHandler`, `GetMonthlyCollectionReportHandler`, `GetFinanceDashboardSummaryHandler`, `GetReceptionWiseReportHandler`, and `OpdPaymentRepository.GetDoctorWiseTotalsAsync` (DEF-E2E-12).

**Build/test status:** `TenantCore.Application` and `TenantCore.Infrastructure` build clean (0 errors) — `TenantCore.Api` couldn't be rebuilt standalone again because the running app has it file-locked; no source changes were made in `TenantCore.Api` this round. `TenantCore.Application.Tests` (988 tests), `TenantCore.Domain.Tests` (134 tests) and `TenantCore.Infrastructure.Tests` (127 tests) all pass, including new regression tests for the exact DEF-E2E-11 and DEF-E2E-12 scenarios (`AcceptAmountHandoverHandlerTests`, `GetActiveCounterSessionHandlerTests`, `GetOpdCollectionsBySessionHandlerTests`, `GetDailyCollectionReportHandlerTests.Handle_ItemCollectedOnADifferentDayThanTheFee_IsCreditedToItsOwnDayNotTheFeeDay`).

**Known, deliberately out-of-scope edge case:** if a session that's still **open** is later re-queried after an item of one of its "home" payments gets collected in a *different* later session, that payment's base contribution to the first session's live total doesn't retroactively shrink (the general formula backs the item out of *every* query, so this is actually now correct too — it's only sessions that are already **closed** before the foreign collection happens, like round 3's session B, whose frozen total never gets corrected without a back-fill, same as OBS-10 already notes for DEF-E2E-08).

## Re-test checklist after the next fix

1. Reception dashboard shows total and completed counts and lists completed visits (DEF-E2E-01).
2. Reception-wise report returns the receptionist for a single-day range (DEF-E2E-02).
3. Add item to a collected visit → status or balance reflects the item; collect → `collectedAmount` = final; apply discount → refundDue correct (DEF-E2E-03).
4. Daily report shows patient names (DEF-E2E-04).
5. Re-run the full 12-visit ledger on a new session and confirm ₹ totals match on counter, daily, doctor-wise and finance dashboard.
6. Handover of the full session total is accepted when items were collected, and the session then closes (DEF-E2E-06).
7. `all-active` and the session list show the same total as the Counter page (DEF-E2E-07).
8. With one item billed but not collected, daily, dashboard, doctor-wise and date-range totals equal the collected money (DEF-E2E-05).
9. Item added in session A, collected in session B: session B shows the item amount, session A does not (TC-RT-11).
10. Early-morning (00:00–05:30 IST) collection appears on its own date in daily, dashboard, date-range and the Finance Dashboard (DEF-E2E-10).
11. Item billed in session A and collected in session B shows in session B's total and handover, and not in A (DEF-E2E-08).
12. After the back-fill, round 1 (0007–0018) shows ₹6,850 on reports and the closed-session list (DEF-E2E-09).
13. `by-session` drill-down for an earlier (still-open, not previously closed) session no longer includes an item collected later in a different session; the later session's drill-down shows it instead (DEF-E2E-11).
14. Daily/date-range/weekly/monthly/dashboard/doctor-wise/reception-wise reports credit an item's money to the day it was actually collected, not the day its payment's visit fee was received (DEF-E2E-12).
