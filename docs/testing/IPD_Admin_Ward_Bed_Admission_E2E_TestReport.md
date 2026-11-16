# IPD End-to-End — Ward / Room / Bed → Admission → Bed Shift → Deposit/Billing → Discharge → Reports

> **REPORT STATUS (16-Nov-2026 final UI retest): 🟢 ALL IPD DEFECTS VERIFIED AND CLOSED IN THE UI — DEF-IPD-14 … DEF-IPD-31. Only DEF-IPD-21 stays partially open because the cross-clinic cases SEC5/SEC6 were never run (see "Not run").**
> Last open items DEF-IPD-30 (Deactivated list) and DEF-IPD-31 (role lost on reactivate) passed after the latest Auth deploy. See "16-Nov-2026 final UI retest" below.


> **REPORT STATUS (16-Nov-2026, third dev response): DEF-IPD-30 build/deploy confirmed + DEF-IPD-31 (reactivate leaves user with no role) fixed — pending QA re-retest.**
> DEF-IPD-30: the Auth repository fix from the second dev response was still in place; re-confirmed with a fresh Release build and a passing regression test. **Found while confirming:** the `publish` output folder (what an IIS deployment would normally copy) had a DLL dated 23-Sep-2026 — two months before this fix existed. Regenerated it with `dotnet publish`; it's now dated 16-Nov-2026 and carries both fixes below. If IIS was serving from that stale `publish` folder, that alone would explain the fix not showing up in testing regardless of the code change — please deploy from the freshly-generated folder, not a cached package. DEF-IPD-31: reactivating a deactivated user's mapping restored the mapping but not the role it had deactivated, leaving the user with zero permissions until someone manually re-assigned one via Change Role — now the reactivate path restores that same role. Full detail in `plan/ipd-billing-deposit-completion/PLAN.md` → **Phase 9**, and in a new "Dev remarks — DEF-IPD-30 deploy confirmation + DEF-IPD-31 fix (16-Nov)" section below, right after the "16-Nov-2026 full UI retest" section's existing dev remarks. **Not verified in a running browser** — no browser tool available this session.
>
> (Status line from the second dev response, kept for context:)

> **REPORT STATUS (16-Nov-2026 full UI retest): 🟠 NOT CLOSED — 3 open front-end defects: DEF-IPD-28 (grace window can't be saved), DEF-IPD-29 (blank error toast on Wards & Rooms delete refusals), DEF-IPD-30 (deactivated member vanishes, can't be reactivated). DEF-IPD-27 closed in the UI. Details in "16-Nov-2026 full UI retest" below and in `UI_Retest_16Nov_NewDefects.md` (same folder).**


> **REPORT STATUS (16-Nov-2026 UI re-retest): 🟠 NOT CLOSED — DEF-IPD-27 (Cancel button) VERIFIED PASS in the UI and CLOSED; DEF-IPD-28 is STILL OPEN (grace window cannot be saved from the UI). SEC5/SEC6 not run.**
> Tested in Chrome as Clinic Admin on the 16-Nov-afternoon build. See "16-Nov-2026 UI re-retest" below. Gateway/manual-payment tests are out of scope for this report (tracked separately).


> **REPORT STATUS (16-Nov-2026 dev response): DEF-IPD-27 (no Cancel button in the UI) and DEF-IPD-28 (Cancellation Window setting not reflecting what was saved) both fixed — pending QA re-retest. Once those pass, the only items left open are SEC5/SEC6 (DEF-IPD-21 full closure).**
> DEF-IPD-27: added a Cancel action (with confirm dialog) to the IPD admissions table — both views — and the admission detail dialog, wired to the existing `PATCH .../cancel` endpoint. DEF-IPD-28: the Settings page was updating its fields from the value it had just *sent*, not from the value the server actually *saved* in its own response — any divergence between the two (including a race between two quick edits) would show the wrong value with no sign anything was off. Now resyncs every field from the server's response after every save, and on initial load. Full detail in `plan/ipd-billing-deposit-completion/PLAN.md` → **Phase 7**.
>
> (Status line from the 16-Nov QA retest, kept for context:)

> **REPORT STATUS (16-Nov-2026 retest): 🟠 NOT FULLY CLOSED — DEF-IPD-25 (Auth fix) VERIFIED PASS and CLOSED, DEF-IPD-26 (cancel window) VERIFIED PASS and CLOSED. Remaining: SEC5/SEC6 (DEF-IPD-21 full closure) and the DEF-IPD-26 Settings-card UI check.**
> Verified closed in total: DEF-IPD-14, 15, 16, 17, 18, 19, 20, 22, 23, 24 (not-a-defect), 25, 26 and OBS-23/25/27. **Not run:** SEC5 (my attempt to grant a Clinic Admin role across clinics and delete Clinic 4 beds was refused by the QA tooling's permission layer — it was not an HTTP response from the app, so there is nothing for the app to fix; it needs a human or a throwaway clinic), SEC6 (needs the Clinic 4 admin to hold a role in Clinic One Care — same cross-clinic grant), and the DEF-IPD-26 Settings card (Clinic One Care application shows `isActive:false`, so the clinic cannot be selected in the UI; the API works). See "16-Nov-2026 QA retest" below.
>
> (Dev status line of 15-Nov, kept for context:)
> **REPORT STATUS (15-Nov-2026 dev response): DEF-IPD-25 fixed (TenantCore.Auth), pending QA re-retest. SEC5/SEC6 still blocked — see dev remarks below for what was found and what's needed.**
> Root cause and fix are in `TenantCore.Auth` (login/claims path), not TenantCore.App. Full detail in "Dev remarks — DEF-IPD-25 fix + SEC5/SEC6 follow-up (15-Nov)" below, right under QA's own 15-Nov retest section, and in `plan/ipd-billing-deposit-completion/PLAN.md` → **Phase 6**.
>
> (Status line from the 15-Nov QA retest, kept for context:)

> **REPORT STATUS (15-Nov-2026 retest): 🟠 NOT FULLY CLOSED — all fixed defects verified and CLOSED; 1 new High defect (DEF-IPD-25, access not revoked after removing a user from a clinic) and 2 security cases not run (SEC5, SEC6).**
> Retested on the 15-Nov build. **Verified PASS and closed: DEF-IPD-14, 15, 16, 17, 18, 19, 20, 22, 23, 24 (closed as not-a-defect), plus OBS-23/25/27.** DEF-IPD-21 is verified for preview/transfer/cancel (SEC3/SEC4) but stays open until SEC5/SEC6 are run. Second clinic used: "Clinic 4" (`CLC4`, user `awez.vtt+cl4@gmail.com`). See "15-Nov-2026 QA retest" below.
>
> (Status line from the 15-Nov dev update, kept for context:)

> **REPORT STATUS (15-Nov-2026 update): all 3 of the 14-Nov retest's new defects are fixed, pending QA retest.**
> DEF-IPD-22 (cancelled Final Bill wording), DEF-IPD-23 (wallet "Last top-up" picking up a cancel reversal) and
> DEF-IPD-24 (investigated — not a real vulnerability, no `notes` field exists on Cancel; explained in full
> below) are all done, plus OBS-23/25/27 picked up along the way. Build green, full suite green (**1,848 tests**).
> See "Fixes for DEF-IPD-22/23/24 (15-Nov, from the 14-Nov retest)" below for the full detail and what's still
> deferred (OBS-22/24/26/28, with reasoning each). **Still not run anywhere:** DEF-IPD-20/21 + SEC1–SEC6 (need a
> second clinic and a user linked to two clinics) — unchanged from the 14-Nov retest, nothing new here affects them.
>
> (Status line from the 14-Nov retest, kept for context:)
> 🟠 NOT CLOSED — 3 low-severity new defects (DEF-IPD-22/23/24) + items that could not be run.
> Retested on the 14-Nov build as Clinic Admin and Reception (Recep One), same clinic "Clinic One Care", Chrome + API.
> **DEF-IPD-14, 15, 16, 17, 18, 19 and the smaller items (wallet chip, search debounce) all PASS.** Not run: DEF-IPD-20/21 and SEC1–SEC6 (need a second clinic and a user linked to two clinics — none exists; I did not create one), and the "Still waiting for payment confirmation…" message (needs a real Razorpay payment, which was deliberately not made). Details in "14-Nov-2026 QA retest" below.

> **REPORT STATUS (13-Nov-2026 retest): 🟠 NOT CLOSED — 2 new defects (DEF-IPD-14, DEF-IPD-15) and a few cases still unexecuted.**
> DEF-IPD-05…13 and OBS-5/9 were retested in the browser (Clinic Admin and Reception) and **all pass**. Closing is held back only by the two new defects below and the "Not yet executed" list in the 13-Nov section.

> **REPORT STATUS (13-Nov-2026 update): DEF-IPD-05–13 all VERIFIED PASS on retest.** New work from the
> 13-Nov retest email — **DEF-IPD-19, DEF-IPD-16 (clinic-specific shift charging), DEF-IPD-14/15/17/18
> and 4 smaller items are all built, pending QA retest.** Build green, full suite green (1,824 tests).
> **None of it has been clicked through in a browser yet** — see "13-Nov QA retest results + new work
> queued" below for what to retest.
>
> **🔴 Security pass added same day (13-Nov), before any QA retest:** since this round moves money
> (shift write-offs, manual overrides, full refunds), a dedicated adversarial review was done over
> everything built for DEF-IPD-16/19 — **2 real vulnerabilities found and fixed** (DEF-IPD-20 cross-tenant
> data leak/corruption, DEF-IPD-21 privilege escalation via a role held in a different clinic), both
> now covered by regression tests, full suite green at **1,839 tests**. See "Security review —
> DEF-IPD-16/19 money-moving code paths" below for the findings and the full security/edge-case test
> matrix QA should run. DEF-IPD-21 is flagged as a **broader, still-open architectural pattern** beyond
> just this feature — read that section before assuming the rest of the app is covered.
>
> (Status line from the 12-Nov pass, kept for context:)
> 🟡 FIXES FOR DEF-IPD-05/06/07 + OBS-5/6/7/8/9 APPLIED 12-Nov-2026 — PENDING QA RETEST. The 12-Nov QA
> pass below (Clinic Admin + Reception) found 3 new defects and 9 observations on top of the 4 original ones
> (all 4 originals remain VERIFIED FIXED). All 3 new defects and 5 of the observations now have a code fix —
> see "What changed since the 12-Nov QA pass" below. **None of this has been re-walked through the UI by a
> human yet** — treat every fix description as a code-grounded claim for QA to verify, not a result.
>
> (Status line from the 12-Nov QA pass, kept for context:)
> 🟠 EXECUTED 12-Nov-2026 (Clinic Admin, UI + API) — 4 original defects VERIFIED FIXED; 3 NEW defects found
> (DEF-IPD-05/06/07). Reception run NOT executed.
> Later on 12-Nov: reception UI pass found **DEF-IPD-08, 09** and the wallet top-up button bug **DEF-IPD-10** (08/09 still open; 10 fixed and retested; follow-on **DEF-IPD-11** — see 'NEW DEFECTS').
>
> (Original status line, written before the 12-Nov execution:)
> **FIXES + NEW FEATURES IMPLEMENTED — PENDING QA RETEST.** All 4 defects from the
> 10-Nov run have a code fix (not yet re-verified by a human QA pass through the UI). Deposit collection, IPD
> patient billing (discount, particulars, Provisional/Final Bill) and IPD reports — previously gaps — are now
> built. **Sections F–M below are new and have never been executed** — that's what this update is for.

---



## 16-Nov-2026 UI re-retest (Clinic Admin `awez1411+clinic1admin@…`, Chrome UI, clinic "Clinic One Care")

| Case | Result |
|---|---|
| **DEF-IPD-27** Cancel button on an Admitted row (Grid/List table) | PASS — `Cancel` shown beside View/Discharge only on the Admitted stay |
| Cancel confirm dialog (title with patient name, ₹0 / full-refund explanation, optional reason, Back / Confirm Cancel) | PASS |
| Cancel refused outside the window (window set to 1 min via the Settings UI) | PASS — dialog stays open, stay stays Admitted, message "This admission can no longer be cancelled — it was admitted more than 1 minute(s) ago. Discharge the patient instead to settle the actual bill." (see OBS-29) |
| Cancel inside the window (window set back to 0 via the UI) with a reason | PASS — "Admission cancelled."; row shows Cancelled; bed released (36 beds / 36 available / 0 occupied); wallet chip refreshed ₹302 → ₹312 (the ₹10 admission fee reversed); final bill ₹0, deposited ₹0, balance ₹0 |
| Cancellation Window card persists across reload (1 and 0) | PASS |
| **DEF-IPD-28** Quick-shift grace window set from the UI | **FAIL — still open** |

**DEF-IPD-28 (Medium, still open) — grace window can't be saved from Settings → Feature Flags.** Typing 90/120 into "Quick-shift grace window" and tabbing away sends `PUT /api/clinic-settings/feature-flags` which returns **400** ("Select a grace rule once the grace window is turned on."). The page has **no control to choose the grace rule** (NewRoomOnly / LowerRate / OldRoomOnly), shows **no error**, and keeps displaying the typed value (120) while the server still has 0. After a reload it silently shows 0. The dev's resync-from-server fix covers the Cancellation Window card but not this: the UI needs a rule dropdown when window > 0 and a visible error when the server rejects the save.

**OBS-29 (Low):** the "can no longer be cancelled" refusal message is a transient toast; it was gone within a few seconds. Consider keeping the message inline in the dialog until dismissed.

Not run in the UI this round: Reception Shift dialog manual amount/reason, Wards & Rooms delete messages, User Management remove/deactivate screens (all API-verified earlier: DEF-IPD-16/17/18/20/25), SEC5/SEC6. Wallet top-up payment / "Still waiting for payment confirmation…" skipped — gateway test, out of scope.

Settings after testing (unchanged from original): cycle PerCalendarDay, mid-stay FullDay, rate rule Destination, grace 0/none, manual Off, cancel window 0. Test stay IPD-20261116-0001 is now Cancelled (no clean-up needed).

> **Scope note (16-Nov, per product owner):** Razorpay/payment-gateway is used only for subscription and wallet top-up. IPD deposit, balance and refund are recorded at the counter with no gateway, by design. Any test that needs the gateway or manual payment input (e.g. the "Still waiting for payment confirmation…" message, wallet top-up payment) is **out of scope here — skipped** and will be tracked in a separate test exercise, not in this report.

## 16-Nov-2026 QA retest — UI (Admin, Chrome browser)

*(This section was dropped by a dev save that overwrote it and has been restored. See the dev remarks right below the Closing Summary of the 16-Nov-API section further down for fix status on both defects raised here.)*

The 15-Nov and earlier-16-Nov passes below were run via direct API calls (`Chrome API` in that section's heading) — this is the first pass that actually clicked through the live Settings and IPD pages in a browser, once the "Clinic One Care is inactive" blocker was cleared.

### Results

| ID | Result | Evidence |
|---|---|---|
| DEF-IPD-26 Settings card (UI) | ❌ FAIL — **new defect DEF-IPD-28** | Changed the Cancellation Window field on the Settings page and saved; the page did not reflect the saved value as expected (reopening/reloading did not show what was entered). See DEF-IPD-28 below. |
| Cancel admission (UI) | ❌ FAIL — **new defect DEF-IPD-27** | Looked for a way to cancel an admitted patient from the `/ipd` page (List and the admission detail view) to actually exercise DEF-IPD-26 end-to-end in the browser. There is no Cancel control anywhere in the UI — only Discharge. See DEF-IPD-27 below. |

### New defects

| ID | Severity | Finding |
|---|---|---|
| **DEF-IPD-27** | 🟠 Medium | **No Cancel button anywhere in the IPD UI.** Cancel only ever existed as an API endpoint (`PATCH .../cancel`) — the admissions table (both Grid and List view) and the admission detail/View dialog only offer Discharge. This blocks any UI-level verification of DEF-IPD-26 (cancellation window) and of DEF-IPD-19's ₹0-cancel behavior — both have only ever been tested via direct API calls. **Correcting my own earlier note:** the original 10-Nov test case G4 ("No UI path... flag if you want one added") treated this as expected/optional at the time; now that DEF-IPD-26 makes the cancel window a real, clinic-configurable setting, a working UI path is not optional anymore — raising it as a real defect, not a nice-to-have. |
| **DEF-IPD-28** | 🟠 Medium | **Settings page's Cancellation Window field does not reliably show what was actually saved.** Set a value in the Settings card, saved, and the page did not come back showing that value as the saved/current one. |

### Closing summary (UI pass)
Open: **DEF-IPD-27 (new)**, **DEF-IPD-28 (new)**. Everything else already verified in the API-level 16-Nov section below is unaffected by this UI pass.

---

## 16-Nov-2026 QA retest (Admin `awez1411+clinic1admin@…`; `awez.vtt+cl4@gmail.com`; Chrome API)

| ID | Result | Evidence |
|---|---|---|
| DEF-IPD-25 stale role, no access | ✅ PASS — CLOSED | `awez.vtt+cl4@…` still had the stale active Doctor row for Clinic One Care. After the Auth fix: Clinic One Care `beds`, `ipd-registrations`, `usage-billing/wallet`, `ipd-reports/census`, `clinic-settings/feature-flags` → **403** (was 200). Its own clinic (Clinic 4) → 200; Clinic One Care admin and Recep One → 200. |
| DEF-IPD-25 lifecycle | ✅ PASS | `mapping` → c4 gets 200 on Clinic One Care; `PATCH …/status {isActive:false}` → **403 and the Clinic One Care role rows are gone from the login payload**; `DELETE …/users/{id}` afterwards → still 403, no rows. The stale rows from the 15-Nov test were cleaned by this sequence; c4 is linked to Clinic 4 only again. |
| OBS-31 | Observation | `POST …/assign` has no duplicate check — calling it twice leaves two Doctor rows (`Doctor,Doctor`); and `assign` alone (without `mapping`) gives no access. Dev already knows about the duplicate; harmless now that login requires an active mapping. |
| DEF-IPD-26 cancellation window (N1) | ✅ PASS — CLOSED | Window 60: admit + deposit ₹300, cancel → 200, final ₹0, refund ₹300 pending → refunded, ₹10 usage charge reversed. |
| DEF-IPD-26 (N2) | ✅ PASS | Window 1 minute, stay older than 70 s: cancel → 400 "This admission can no longer be cancelled — it was admitted more than 1 minute(s) ago. Discharge the patient instead to settle the actual bill."; stays Admitted, bed not freed, wallet unchanged; Discharge then works. |
| DEF-IPD-26 (N3) | ✅ PASS | Window 0: a 74-second-old stay cancels (200), final ₹0, `wasCancelled` true — unchanged behaviour. |
| DEF-IPD-26 (N4) | ✅ PASS | `-1` → 400; `1441` → 400; `1440` → 200; `0` → 200, reads back 0. Window restored to 0. |
| DEF-IPD-26 Settings card (UI) | ⚪ NOT RUN | The **Clinic One Care application is inactive** in the environment (`GET /api/Application/{id}` → `isActive:false`; login lists the clinic with `IsActive:false`; select-clinic shows "linked with 0 clinics"). Seen since the 15-Nov rebuild; QA did not change it. Re-activate it, then check the new card (0–1440). |
| SEC5 / SEC6 | ⚪ NOT RUN | See banner. Dev note: Clinic 4's admin is `awez1411+cl4@gmail.com`; a cross-clinic role grant to run the cases needs a human decision/approval. |

### Closing summary
**Closed:** DEF-IPD-14, 15, 16, 17, 18, 19, 20, 22, 23, 24, 25, 26. **Open (not defects, just not executed):** DEF-IPD-21 full closure (SEC5/SEC6 — SEC3/SEC4 already pass), DEF-IPD-26 Settings-card UI check, and the deferred dev items OBS-22/24/26/28.

---

## 16-Nov-2026 final UI retest (Chrome, UI only — Clinic Admin `awez1411+clinic1admin@…`, latest Auth build deployed)

| Case | Result |
|---|---|
| **DEF-IPD-30** User Management → Deactivate Recep Two → she leaves Active (5→4) and appears under Deactivated (counts 4 / 2 / total 6) with Last Role "Receptionist", Deactivated date and a "Re-Activate" button | PASS — **CLOSED** |
| Deactivated tab no longer swallows an error (list request succeeds) | PASS |
| **DEF-IPD-31** Re-Activate (confirm dialog "They will regain full access to the clinic." → "User mapping activated successfully") → back in Active (5 / 1), role restored to **Receptionist** (not "—") | PASS — **CLOSED** |

Carried over, already passed in earlier UI rounds today and not changed since: DEF-IPD-27 (Cancel button), DEF-IPD-28 (grace window + rule), DEF-IPD-29 (delete-refusal messages), DEF-IPD-16 manual shift charge as Reception, cancel / cancellation window, admit / deposit / discharge / collect balance, deactivated user loses access (DEF-IPD-25).

State left behind: Recep Two active as Receptionist. "clinic4 reception" (`awez.vtt+cl4@gmail.com`, last role Doctor) is still deactivated from earlier QA — reactivate from the Deactivated tab if needed. Settings are at their originals (cycle PerCalendarDay, mid-stay FullDay, rate rule Destination, grace 0, manual Off, cancel window 0). Wallet ₹202.00.

### Not run (open items, not defects)
- SEC5 / SEC6 cross-clinic cases → DEF-IPD-21 not fully closed until run.
- UI "Remove" of a user; IPD Reports pages; Doctor-role checks.
- Low-severity observations OBS-29/30/31 not re-checked.
- Payment-gateway / manual-payment tests: out of scope here, tracked in a separate exercise.

## 16-Nov-2026 full UI retest: results and NEW DEFECTS for developers

Tested through the browser UI only (Chrome) as Clinic Admin `awez1411+clinic1admin@…` and Reception `awez1411+clinic1_recep1@…`, clinic "Clinic One Care". Toast text was read from the DOM (`.toast-item`) because toasts vanish within ~3 s.

**Status: NOT CLOSED — 3 open front-end defects (DEF-IPD-28, 29, 30).**

## Open defects
- **DEF-IPD-28 (Medium) — grace window can't be saved from the UI (still open after the dev's 16-Nov fix).** Settings → Feature Flags → "Quick-shift grace window": typing 90/120 sends `PUT /api/clinic-settings/feature-flags` which returns 400 "Select a grace rule once the grace window is turned on." The page has no control to choose the grace rule (NewRoomOnly / LowerRate / OldRoomOnly), shows no error and keeps showing the typed value; reload shows 0. Expected: a rule dropdown when window > 0 and a visible error when the save is rejected.
- **DEF-IPD-29 (Medium) — blank error toast when Wards & Rooms delete is refused.** Deleting a bed, room or ward that has admission history returns 409 (correct, data kept) but the red toast is empty (`<div class="toast-item toast-error"><span></span></div>`). The user gets no explanation, so DEF-IPD-17's "use Deactivate instead" message never shows. Other error toasts (reason required, over cap, cancel window) do show text, so only this path loses the message. Repro: Admin → Wards & Rooms → Delete room DUMMY-G-101 → Delete. Also affects "QA Delete Ward".
- **DEF-IPD-30 (Medium) — deactivated member disappears and can't be reactivated from the UI.** User Management → Deactivate Recep Two → toast "User mapping deactivated successfully"; Active Members goes 5→4 but the Deactivated tab and counter stay 0 ("No deactivated members"), also after reload. Admin cannot see or reactivate her. (QA reactivated via the Auth status endpoint for clean-up.)

## Observations (Low)
- OBS-29: the "can no longer be cancelled" refusal is a transient toast (~3 s); consider an inline message in the dialog.
- OBS-30: a deactivated user who opens the clinic sees "This clinic isn't active yet … choose a plan" — misleading; the clinic is active, her access was removed.
- OBS-31: after "Balance collected" / "Fully settled" the Balance row still shows ₹200.00 (should be ₹0 or relabelled).

## Verified PASS in the UI (closed)
DEF-IPD-27 Cancel button (table, Admin dialog, Reception dialog) incl. confirm dialog, ₹0 bill, ₹10 reversal, refusal outside window ("This admission can no longer be cancelled — it was admitted more than 1 minute(s) ago. Discharge the patient instead…"); Cancellation Window card; DEF-IPD-16 Reception manual shift (reason required, ₹2,500 cap message, ₹300 accepted, −₹2,200 write-off line, fields hidden when override Off); admit, deposit, discharge, collect balance; DEF-IPD-17 unused ward create/delete; DEF-IPD-25 deactivated user sees 0 clinics and a locked page.

## Not run
UI "Remove" of a user; SEC5/SEC6; IPD Reports pages; Doctor-role checks; payment-gateway tests (out of scope — tracked separately).

## State left behind
Settings restored (cycle PerCalendarDay, mid-stay FullDay, rate rule Destination, grace 0/none, manual Off, cancel window 0). Stays IPD-20261116-0001 (Cancelled), -0002 (Discharged, settled), -0003 (Cancelled). Recep Two reactivated. Wallet ₹202.00.

## Dev remarks — DEF-IPD-27 + DEF-IPD-28 fix (16-Nov)

*(Response to the "16-Nov-2026 QA retest — UI" section above. Does not change or remove anything QA found. Full technical detail in `plan/ipd-billing-deposit-completion/PLAN.md` → **Phase 7**.)*

**DEF-IPD-27 — fixed.** Added a "Cancel" action next to Discharge in the admissions table (both the Grid and the List view) and in the admission detail/View dialog, each guarded to only show while the admission is `Admitted`. It opens a confirm dialog (optional reason, same pattern as Discharge's) and calls the same `PATCH .../cancel` endpoint this report's API-level tests have exercised since DEF-IPD-19/26 — no new backend behavior, just the missing front door onto it.

**DEF-IPD-28 — fixed.** Root cause: the Settings page updated its own fields from the value it had *just sent* to the server, not from the value the server's own save response said it actually persisted. The server-side save/load path itself was already correct (confirmed again by re-reading `UpdateClinicFeatureFlagsHandler` and its passing tests) — this was purely a client-side trust problem: if anything ever caused the saved value to differ even slightly from what the UI optimistically assumed (the clearest case: changing two of this page's settings in quick succession, where the second save's own `?? _field` fallback could capture the first field's *not-yet-applied* local value and silently write it back), the page would show what the user typed, not what got saved, with nothing on screen to indicate a mismatch. Fixed by always re-reading every field from the server's response (`ApplyFromServer`) after a save and on initial load, instead of trusting local state. This applies to every control on the Settings page, not just the Cancellation Window field.

**Verification:** `dotnet build` on both the Application tests and Web.Client projects green; full `TenantCore.Application.Tests` suite still green at 1,218 tests (unchanged — both fixes are Web.Client-only, no server-side behavior changed, so no new automated coverage was added, consistent with this page's existing gap noted earlier in this plan: "no controller-level or Web.Client-level automated tests exist yet"). **Not verified in an actual browser** — this session has no browser access; please re-run both repro steps from the UI section above once this build is deployed.

---

## Dev remarks — DEF-IPD-30 deploy confirmation + DEF-IPD-31 fix (16-Nov)

*(Response to QA's request for build confirmation before the next IIS deploy, plus the new DEF-IPD-31 found during manual clean-up. Full technical detail in `plan/ipd-billing-deposit-completion/PLAN.md` → **Phase 9**.)*

**DEF-IPD-30 — re-confirmed, and a likely reason it hadn't shown up fixed yet.** The `UserApplicationRepository.GetDeactivatedByApplicationIdAsync` `.Include()` fix from the earlier dev response was still in place, unchanged, and its regression test still passes. While confirming the build, `src/TenantCore.Auth.API/bin/Release/net8.0/publish/` — the folder an IIS deployment would normally copy — had a `TenantCore.Auth.Infrastructure.dll` dated **23-Sep-2026**, roughly two months before this fix (and DEF-IPD-25's) existed. If IIS has been serving from that same stale `publish` output, that alone would explain why the fix hasn't reached testing yet, independent of whether the code itself was correct. Regenerated it with `dotnet publish src/TenantCore.Auth.API -c Release`; it is now dated 16-Nov-2026 and contains both this fix and DEF-IPD-31's. **Please deploy this freshly-generated `publish` folder to IIS, not whatever package was last copied there.**

**DEF-IPD-31 — fixed.** `ToggleUserApplicationMappingAsync`'s reactivate path only ever flipped the `UserApplication` mapping back on — restoring the role was deliberately left out of the original DEF-IPD-25 fix, on the theory that the role might have been intentionally changed while the user was deactivated. In practice there's no UI path to change a role on a deactivated user, so that scenario never happens — and the actual effect was that the ordinary "I deactivated the wrong person, undo it" action silently left the user with no role and no permissions, with nothing on screen to say so, exactly as you found with Recep Two. Fixed by having the reactivate path restore the same role row the deactivate path had just turned off, using the same "last known role" lookup (`GetLatestByUserAndApplicationAsync`) the Deactivated tab already uses to display that role — so reactivating and deactivating are now symmetric.

**Verification:** `UserApplicationRevocationIntegrationTests` — 4/4 pass, including the rewritten reactivate test (it previously asserted the exact behavior this report flagged as DEF-IPD-31, so it was rewritten to assert the fixed behavior instead — no other test depended on the old behavior). Full `TenantCore.Auth.Test` suite: 235 passed, same pre-existing 12 `PasswordServiceTests` failures, no new failures. `dotnet publish src/TenantCore.Auth.API -c Release` green (see above). **Not verified in a running browser** — no browser tool available this session; please re-test the deactivate → reactivate round-trip from the UI once this build is on IIS.

---

## 15-Nov-2026 QA retest (Admin `awez1411+clinic1admin@…` in Clinic One Care; `awez.vtt+cl4@gmail.com` (Receptionist) in Clinic 4; Chrome, UI + API)

Settings were restored to the original values (cycle 1, mode 1, grace 0/null, manual Off, rate rule Destination). Wallet ended at ₹412.

### Results

| ID | Result | Evidence |
|---|---|---|
| DEF-IPD-22 cancelled Final Bill wording | ✅ PASS — CLOSED | API `wasCancelled=true`. Print page: "CANCELLED — No charge. All fees below were waived.", Initial fee (waived), Room charges (waived), Total (all waived), footer explains the refund. Live dialog: "Cancelled — no charge. All fees waived.", Final ₹0, Refund due ₹300, Confirm Refund Given → "Fully settled". |
| DEF-IPD-23 wallet "Last top-up" | ✅ PASS — CLOSED | Cancel with a ₹10 reversal: `lastTopUpAt` unchanged before/after. |
| DEF-IPD-24 cancel notes | ✅ CLOSED (not a defect) | Agreed: `CancelIpdRegistrationRequest` has only `Reason`; the extra `notes` property is ignored. `Reason` with `<script>` → 400. |
| OBS-23 grace window cap | ✅ PASS | 1441 → 400; 1440 → 200. |
| OBS-25 bed-history fields | ✅ PASS | `standardChargeAmount, appliedChargeAmount, adjustmentType, writeOffAmount, manualChargeReason` now returned (null on a plain segment). |
| OBS-27 prorated rounding | ✅ PASS | Prorated shift line amounts are rounded (0.01, 0). |
| DEF-IPD-20 cross-tenant preview/transfer (SEC1, SEC2) | ✅ PASS — CLOSED | User with Receptionist in Clinic 4 and Doctor in Clinic 1, Clinic 4 selected, called preview and transfer-bed on a Clinic 1 admission with both a Clinic 4 and a Clinic 1 bed: all **401**. Admission unchanged (1 history row, room ₹2,500, Admitted, wallet unchanged). Note: the plan expects 404; 401 is safe but differs. |
| DEF-IPD-21 role in another clinic (SEC3, SEC4) | ✅ PASS (partial) | Same user: transfer and cancel on the Clinic 1 record with Clinic 4 selected → 401; control with Clinic 1 selected (Doctor only) → 403. No refund, no wallet movement. |
| DEF-IPD-21 delete cases (SEC5) | ⚪ NOT RUN | Needed a Clinic Admin role for the test user in Clinic 1 followed by deletes on Clinic 4 beds. The environment's permission layer blocked granting the role and attempting cross-clinic deletes, so I did not retry. Run it manually or with a throwaway clinic. |
| SEC6 manual charge admin check | ⚪ NOT RUN | Needs a user who is Clinic Admin in one clinic and a plain Receptionist in a second clinic that has AdminOnly; Clinic 4 has no admin account known to QA. |
| "Still waiting for payment confirmation…" | ⚪ NOT RUN | Needs a real payment. |

### New defect

| ID | Severity | Finding |
|---|---|---|
| **DEF-IPD-25** | 🔴 High (security / Auth) | **Removing or deactivating a user in a clinic does not revoke access to that clinic's data.** Steps: Clinic One Care admin assigns the existing user `awez.vtt+cl4@…` the Doctor role (`POST /api/Application/{app}/users/{userId}/assign`), then removes the user (`DELETE /api/Application/{app}/users/{userId}?removedBy=`) and also deactivates the mapping (`PATCH …/users/{userId}/status` `{isActive:false}`) — both return 200 and the user disappears from the clinic's user list. After logging in again, with `X-Application-Id` = Clinic One Care, the user still gets **200** on `beds`, `ipd-registrations` (patient names visible), `ipd-reports/census`, `usage-billing/wallet`, `clinic-settings/feature-flags`, while a random application id gives 403. The login payload still lists an active `Doctor` user-role row for that clinic, so the role row is not removed or deactivated with the mapping. Expected: 403 for that clinic immediately after removal. Fix in the Auth service (remove/deactivate the `UserRole` rows for that application, or make the authorization handler require an active mapping). |

### Test data / cleanup needed by you
- User `awez.vtt+cl4@gmail.com` still has a stale active **Doctor** role row for Clinic One Care (and therefore read access) because of DEF-IPD-25 — please delete that `UserRole` row (or fix and re-run). Clinic 4 data was not changed (all cross-clinic deletes were skipped).
- Many cancelled/discharged DUMMY stays in Clinic One Care; ward "QD14 X" (history, deactivate it).

### Closing summary
Closed: DEF-IPD-14, 15, 16, 17, 18, 19, 20, 22, 23, 24. Open: **DEF-IPD-25 (dev: fixed 15-Nov, pending QA re-retest — see dev remarks below)**, DEF-IPD-21 pending SEC5/SEC6, and the deferred items OBS-22/24/26/28 (dev decisions, not defects).

---

## Dev remarks — DEF-IPD-25 fix + SEC5/SEC6 follow-up (15-Nov)

*(Everything in this section is the developer's response to the QA retest above — it does not change or supersede anything QA found; it reports what caused it and what was done. Full technical detail in `plan/ipd-billing-deposit-completion/PLAN.md` → **Phase 6**.)*

**DEF-IPD-25 — fixed, in TenantCore.Auth (not TenantCore.App).** You asked whether the UI's Remove uses a different endpoint than the one you called directly — **no, it's the same endpoint** (`DELETE {applicationId}/users/{userId}`), confirmed by tracing the Blazor "Remove" button through to the exact HTTP call. The actual bug was two things, both now fixed:
1. `PATCH .../users/{userId}/status` (deactivate the mapping) only ever flipped the mapping's own flag — it never touched the user's role row for that clinic, so "deactivating" a user was a complete no-op for access.
2. More fundamentally, the login code that builds the JWT only checked whether the role row itself was active — it never checked whether the user still had an active mapping to that clinic at all. So even where the `DELETE` path correctly removed the mapping, any active (or later re-created, e.g. via a repeat `assign` call — that endpoint has no duplicate check) role row for that clinic would still grant access on the next login, regardless of which removal action ran.

Fixed both: the login claims code now requires an active mapping before it will include a role for that clinic (closes the gap for good, regardless of any stray role rows), and the deactivate-mapping endpoint now also deactivates the matching role so the data itself stays consistent. New tests added against a real (in-memory) database — a plain mocked-repository test, which is what existed before, can't see this bug at all, because the EF query filters that actually caused it never run against a mock. Full suite green aside from 12 pre-existing failures confirmed unrelated to this change (verified by reverting the fix and re-running — same 12 fail either way).

**One loose end from before this fix shipped:** `awez.vtt+cl4@gmail.com` still has a stale active Doctor role for Clinic One Care in the dev DB from testing DEF-IPD-25 itself — the fix doesn't retroactively clean up rows that were already wrong, it only changes behavior going forward. Needs a one-off cleanup (re-running `DELETE .../users/{userId}` for that user+clinic now that the fix is in place) before this account is reused for DEF-IPD-20/21/SEC1-6.

**SEC5 (role-grant blocked):** read through the role-change/assign code, the authorization policy behind both endpoints, and the UI's role dropdown — found no restriction anywhere on assigning "Clinic Admin" specifically. Can't reproduce or explain the block without seeing the actual HTTP response (status code + body) you got when you tried it — happy to dig further with that in hand.

**SEC6 ("Clinic 4 has no admin account known to QA"):** it does — `awez1411+cl4@gmail.com` is Clinic 4's active Clinic Admin (same read-only query that found the other clinics' admins). The password-reset block flagged earlier (Claude Code's own safety classifier refusing the `UPDATE` to set it to a known test password) still applies if you need to log in as this account specifically.

---

## 14-Nov-2026 QA retest (Admin `awez1411+clinic1admin@…`, Reception `awez1411+clinic1_recep1@…`, Chrome; API + UI)

Clinic settings were changed during testing and **restored to the original values** (cycle 1, mid-stay mode 1, grace 0 / rule null, manual Off, full-day rate rule Destination).

### Results

| ID | Result | Evidence |
|---|---|---|
| DEF-IPD-19 cancel (Admin) | ✅ PASS | Admit ICU (fee ₹300) + deposit ₹700 → cancel → final ₹0 (room/particulars 0), refund due ₹700, status PendingRefund → `process-refund` → Refunded. Wallet 642 → 632 on admit → 642 after cancel (₹10 usage reversed). |
| DEF-IPD-19 cancel with service item | ✅ PASS | 400 "Remove the service items or discharge the patient instead."; stay stays Admitted. After removing the item, cancel succeeds. |
| DEF-IPD-19 double cancel / repeat refund (SEC16/18) | ✅ PASS | Second cancel → 400 "Only an Admitted registration can be cancelled."; repeat refund → 409 "No refund is pending"; wallet not credited twice. |
| DEF-IPD-19 cancel by Reception | ✅ PASS | Reception admit → deposit ₹200 → cancel → final ₹0, refund ₹200 confirmed by Reception; wallet net 0. Deposit after cancel → 409 "only available while the patient is admitted". |
| DEF-IPD-19 UI | ✅ PASS | Cancelled stays listed for Admin and Reception; dialog shows Final ₹0, refund due, Print Final Bill, "Fully settled" after refund. |
| DEF-IPD-16 settings validation (SEC14/15, EDGE10) | ✅ PASS | Negative window → 400; window > 0 without rule → 400; rules 1/2/3 persist; window 0 clears the rule to null. New controls present on Feature Flags page. |
| DEF-IPD-16 preview endpoint | ✅ PASS | ICU→General in window: standard 2500, suggested 300, `withinGraceWindow` true; manual allowed flag follows mode and role. UI Shift dialog shows "Time spent in current room bills at ₹2,500.00 standard." |
| DEF-IPD-16 grace rules (N6–N9) | ✅ PASS | Window 120 min, ICU→General: NewRoomOnly → room ₹300 (total ₹600 incl. fee); LowerRate → ₹300; OldRoomOnly → ₹2,500 (total ₹2,800). General→ICU with LowerRate → ₹300. |
| DEF-IPD-16 manual override | ✅ PASS | ₹1,000 with reason accepted: line standard 2500 / applied 1000 / adjustment type 2, write-off ₹1,500. Above cap (2501) → 400 "cannot exceed the standard charge of ₹2,500.00"; negative → 400; missing reason → 400; `<script>` reason → 400; exactly the cap accepted; ₹0 with reason accepted. |
| DEF-IPD-16 role matrix | ✅ PASS | Mode Off: Admin and Reception → 403 "turned off"; AdminOnly: Admin 200, Reception 403; AdminAndReception: both 200. Preview `manualChargeAllowed` matches. Reception cannot PUT feature flags (403). |
| DEF-IPD-16 cycle × mode × rate rule | ✅ PASS | Prorated (both cycles): small non-zero amounts, no negative/zero total; Full-day Origin ICU→G = ₹2,500, Higher G→ICU = ₹300 on the 300 line (ICU line 300 shown) and Higher ICU→G = ₹2,500, Destination default = destination room; 24-hour block ICU→G = ₹300. |
| EDGE11 provisional = final | ✅ PASS | Manual ₹1,000 then discharge: provisional (total 1300, write-off 1500) equals final bill exactly. Reception dialog shows "Shift charge write-off − ₹1,500.00". |
| DEF-IPD-14 dialog refresh | ✅ PASS | After Shift in the open dialog: new ward/room/bed, bill ₹600, refund due ₹400. After Discharge: status Discharged, Print Final Bill + Confirm Refund Given appear; after refund "Fully settled". |
| DEF-IPD-15 Top up button | ✅ PASS | Focuses the amount input on the wallet form; no redirect. |
| DEF-IPD-17 delete protections | ✅ PASS | Occupied bed → 400 "currently occupied"; room/ward and bed with history → 409 "…has admission history… Deactivate it instead"; unused bed/room/ward → 204; unknown id → 404; Reception → 403. |
| DEF-IPD-18 Reception list | ✅ PASS | Reception List view shows all admissions (Admitted/Discharged/Cancelled), View opens the dialog on discharged stays with Final Bill and Confirm Refund Given. |
| Wallet chip refresh | ✅ PASS | Chip was stale ₹642; after UI discharge it updated to ₹532, matching the API. |
| Search debounce | ✅ PASS | No change at 150 ms; filtered to the patient after the debounce. |
| Input validation | ✅ PASS | Deposit ≤ 0 → 400; deposit notes with `<script>` → 400; cancel reason with `<script>` → 400. |
| DEF-IPD-20/21, SEC1–SEC6 | ⚪ NOT RUN | Need a second clinic and a user in two clinics. |
| "Still waiting for payment confirmation…" | ⚪ NOT RUN | Needs a real payment; not made. |
| EDGE8, SEC20–22/24 | ⚪ NOT RUN | Code-review items / not exercised. |

### New defects

| ID | Severity | Finding |
|---|---|---|
| DEF-IPD-22 | Low | Print Final Bill for a **cancelled** stay says "FINAL BILL — Locked at discharge" and lists "Initial fee ₹100.00" while Total is ₹0.00 (does not add up). Expected: a "Cancelled — no charge" wording and the initial fee shown as waived/reversed. |
| DEF-IPD-23 | Low | Wallet page "LAST TOP-UP" shows the time of the latest **cancel reversal** (credit "Refund — IpdAdmission") — `lastTopUpAt` should only reflect real top-ups. |
| DEF-IPD-24 | Low (security hygiene) | Cancel `notes` accept HTML: `<img src=x onerror=alert(1)>` returned 200, while cancel `reason`, deposit notes and manual-charge reason reject markup. Apply the same markup validator to cancel notes (and check other free-text fields, e.g. discharge notes). Not seen echoed in list responses; stored value not located. |

### Observations

- OBS-22: Cancel with an empty reason succeeds (200); the manual-shift reason is required but cancel reason is not. Decide if it should be mandatory.
- OBS-23: Grace window has no upper limit (100000 minutes accepted).
- OBS-24: Several manual overrides on the same calendar day: the later ones (exact cap, ₹0) are accepted but the bill keeps only the first segment, so they have no effect and no write-off is recorded. Returns 200 silently; consider telling the user or rejecting.
- OBS-25: `bed-history` API does not return the new segment fields (standard/applied/adjustment/write-off); the bill endpoint does.
- OBS-26: Print Final Bill shows only totals — no room line items and no write-off line, although the dialog shows the write-off. Final bill API of a ₹0 cancelled stay still lists a room line (ICU 2500) with zero totals.
- OBS-27: Prorated room line items return unrounded values (e.g. 0.0110484) in the API while totals are rounded.
- OBS-28: IPD reports (revenue/discharge-summary) do not show write-off totals; `discharge-summary` requires both `fromUtc` and `toUtc` (400 otherwise).
- OBS-29: Login was slow (~20 s) on first call after the rebuild.
- OBS-30: First-time reception login email in the old report (`clinic1_recep1@gmail.com`) is wrong; actual is `awez1411+clinic1_recep1@gmail.com` (docs only).

### Test data left behind
Clinic wallet ended at ₹422. Many Cancelled stays for DUMMY Patient 011, discharged stays 0026/0027 (Patient 010/011), and the ward "QD14 X" (has admission history, cannot be deleted; deactivate it). All settings restored.

### To close
~~Fix/decide DEF-IPD-22/23/24~~, then run DEF-IPD-20/21 + SEC1–SEC6 once a second clinic exists. Everything else above is verified.

---

## Fixes for DEF-IPD-22/23/24 (15-Nov, from the 14-Nov retest)

| ID | Title | Status |
|---|---|---|
| **DEF-IPD-22** | Print Final Bill for a cancelled stay said "FINAL BILL — Locked at discharge" and listed "Initial fee ₹100.00" next to a ₹0.00 Total — read as the numbers not adding up | **✅ Fixed, pending QA retest.** `IpdFinalBillDto` gained a `WasCancelled` flag (`GetIpdFinalBillHandler` sets it from `registration.Status`). Both the print page and the live dialog now show a plain "CANCELLED — No charge. All fees waived." banner, and the Initial Fee/Room Charges/Total lines are tagged "(waived)"/"(all waived)" instead of looking like an arithmetic error. The underlying figures are deliberately still the real, un-waived ones (not zeroed) — that's *what* was waived, not just confirmation that something was. Retest: open a cancelled admission's Final Bill (print and live dialog) and confirm the new wording. |
| **DEF-IPD-23** | Wallet "LAST TOP-UP" showed the time of the latest **cancel usage-charge reversal**, not a real top-up | **✅ Fixed, pending QA retest.** `ClinicWallet.Credit` gained an `isTopUp` flag (default `true`, unchanged for the two genuine-money-in callers — Razorpay top-up confirmation, admin goodwill credit); `ReverseUsageChargeCommandHandler`'s reversal credit now passes `isTopUp: false`, so it moves the balance without touching `LastTopUpAt`. Retest: cancel an admission, then check the Wallet page — "Last top-up" should still show whenever a real top-up last happened (or "Never"), not the cancellation's timestamp. |
| **DEF-IPD-24** | Cancel "notes" allegedly accepted raw HTML (`<img src=x onerror=alert(1)>` → 200) while reason/deposit-notes/manual-charge-reason reject it | **Investigated — not a real vulnerability, no code change.** `CancelIpdRegistrationRequest`/`CancelIpdRegistrationCommand` has exactly one free-text field, `Reason` — there is no `notes` field anywhere in the cancel pipeline. ASP.NET Core's default JSON binding silently drops unrecognized body properties rather than erroring or storing them, so a request with an extra `"notes"` key succeeds (200) for the unrelated reason that `Reason` itself is optional and was presumably absent — not because any HTML was accepted or stored anywhere. This matches your own note on the finding ("Not seen echoed in list responses; stored value not located") — there was nothing to find, because the field doesn't exist. Re-verified `Reason`'s own validator (`SafeTextValidationRules.IsSafeText`, an allow-list regex: `^[A-Za-z0-9\s.,'\-\/()]*$`) does reject the exact payload tested; confirmed `AdmissionNotes`/`DischargeNotes` (the two real "notes" fields in this flow) are both validated too (`HtmlSanitizationRules.HasNoScriptInjection`, which matches `onerror=`). **Closing as not-a-bug** — if you want, retest by sending the same payload explicitly inside the `reason` field (not a `notes` key) to double-confirm it 400s. |

**Observations also addressed in this pass:**

| ID | Note | Status |
|---|---|---|
| OBS-23 | Grace window had no upper bound (100000 minutes accepted) | **✅ Fixed.** Capped at 0–1440 minutes (24 hours) server-side (validator) and client-side (Settings page input + clamp) — generous for a "quick shift" grace window; beyond that it would silently override the mid-stay charge mode for an admission's entire stay. |
| OBS-25 | `bed-history` API didn't return the shift-charging fields (standard/applied/adjustment/write-off) the bill endpoints already had | **✅ Fixed.** `IpdBedTransferHistoryDto` gained `StandardChargeAmount`/`AppliedChargeAmount`/`AdjustmentType`/`WriteOffAmount`/`ManualChargeReason`, all null/None on a plain or still-open segment. |
| OBS-27 | Prorated room-charge line items returned unrounded values (e.g. `0.0110484`) in the API while totals were rounded | **✅ Fixed.** The line item's displayed amount is now rounded to 2 decimals at the point it's added to the DTO; the running grand total still accumulates the *unrounded* share before its own single final rounding, so this is purely a display fix — it does not reintroduce the "rounds to ₹0" bug DEF-IPD-16 already fixed. |
| OBS-22 | Cancel `reason` is optional while manual-shift reason is mandatory — decide if cancel's should be mandatory | **Decision: left optional, no change.** Matches the existing precedent of `TransferBedCommand.Notes` (also optional) — a cancellation needs *a* record of who/when/what changed (already captured elsewhere: `CancelledByUserId`, the audit timestamp, the reversed usage-charge ledger row), not necessarily free text every time. Revisit if you want it mandatory specifically. |
| OBS-24 | Several manual overrides on the same calendar day: later ones are accepted (200) but have no effect on the bill, silently | **Deferred, not fixed.** Confirmed as a known limitation from when DEF-IPD-16 was built (documented then as an accepted edge case) — the calculator settles a block from the *first* matching frozen override it finds; a second override the same day is accepted and stored but doesn't change the bill. A real fix means teaching the calculator to combine multiple overrides within one block, which is more invasive than this round's defect-fix scope. Flagging for its own pass rather than rushing a change to money-calculation logic. |
| OBS-26 | Print Final Bill shows only totals, no room line-item table (the live dialog's write-off line is present, contradicting the "no write-off line" part of this note — re-verify against this build) | **Partially addressed, rest deferred.** The "doesn't add up" root cause is DEF-IPD-22 above (now fixed). Adding a full day-by-day line-item table to the Final Bill print page (matching the Provisional Bill print's table) is a real but separate UI feature, not a defect fix — deferred. |
| OBS-28 | IPD reports (revenue/discharge-summary) don't show write-off totals; discharge-summary requires both `fromUtc`/`toUtc` | **Deferred (feature), and by-design (date range).** Write-off totals in the reports is a feature addition, deferred alongside OBS-26. The mandatory date range on discharge-summary is intentional (every report in this family takes an explicit range) — not a bug. |
| OBS-29 | Login was slow (~20s) on first call after the rebuild | **Not a defect.** Expected ASP.NET Core / EF Core cold-start (JIT warm-up, first model build) after a fresh `dotnet build` — not something a code change fixes; subsequent logins are fast. |
| OBS-30 | Old report had the wrong reception email in some docs text | **Documentation-only, not application code.** No fix needed here. |

**Verification:** build green, full suite green — **1,848 tests passing** (up from 1,839; +9 new/updated across `ClinicWalletTests`, a new `ReverseUsageChargeCommandHandlerTests` file, `GetIpdFinalBillHandlerTests`, `IpdPaymentTranslatorTests`, `IpdBedTransferHistoryTranslatorTests`, and `UpdateClinicFeatureFlagsCommandValidatorTests`). No new EF migration — every fix in this pass is either DTO/translator-level (no new columns) or a parameter on an existing method. **None of this has been clicked through in a browser yet** — retest per the table above.

---

## 13-Nov-2026 QA retest (Clinic Admin `awez1411+clinic1admin@…` and Reception `Recep One`, same clinic "Clinic One Care", Chrome, UI-driven)

### Retest of the fixed defects

| ID | Result | Evidence |
|---|---|---|
| DEF-IPD-05 | ✅ Pass (Admin) | Bill panel/amounts render correctly in the View dialog. |
| DEF-IPD-06 | ✅ Pass (Admin) | Both print pages (provisional and final) open and show correct figures. |
| DEF-IPD-07 | ⏳ Not executed | Cancel-with-deposit has no UI; needs an API-level run. |
| DEF-IPD-08 | ✅ Pass (Reception) | Clicking an occupied bed opens the full "IPD Details" dialog: Shift Bed, Deposit & Bill, discount, service items, Print Provisional Bill, Discharge. |
| DEF-IPD-09 | ✅ Pass (Admin dialog) / ⏳ Reception admit-without-doctor not re-run | Doctor is required on the Admit dialog. |
| DEF-IPD-10 | ✅ Pass | "Create payment link" enabled, opens the confirm dialog. |
| DEF-IPD-11 | ✅ Pass | Link is created (Razorpay test link, e.g. `rzp.io/rzp/mswivI1`); no "Payments are not configured." |
| DEF-IPD-12 | ✅ Pass | Panel shows "link is being created…" and the link appears **automatically** after ~10 s via polling. |
| DEF-IPD-13 | ✅ Pass | After a full page reload the top-up panel (link, Copy link, "I've paid — check now") is still there. |
| OBS-5, OBS-9 | ✅ Pass | |
| OBS-7 | ✅ Pass | Discharged admissions show Balance ₹0.00 / Settled in the Discharge Summary after the refund. |

### Flows executed in the UI this round

**Admin** — admit, deposit, discount, add/remove service items, bed shift, discharge (`IPD-20261113-0001`, DUMMY Patient 009, final ₹550, deposit ₹1,000, refund due ₹450), **Confirm Refund Given (L4) → toast "Refund confirmed.", panel shows "Fully settled"**, Print Final Bill.

**Reception (Recep One, `awez1411+clinic1_recep1@gmail.com`)** — full end-to-end on `IPD-20261113-0002` (DUMMY Patient 010): admit (bed DUMMY-1, General ₹300/day, initial fee ₹300) → ₹500 deposit → ₹50 discount → shift to Private Ward / P-201 (₹600/day) → ₹350 deposit (balance ₹0) → Discharge → toast "Patient discharged." → IPD Reports: Census 0 occupied / 34 available, Discharge Summary lists the admission, final ₹850.00, balance ₹0.00, Settled.

**Wallet (Admin, no payment made)** — Create link → link appears by polling → Copy link ("Payment link copied") → "I've paid — check now" → reload keeps the panel.

**Usage ledger** — IPD admission −₹10 and IPD discharge −₹100 debits appear with the correct "balance after" (₹1,662 → ₹1,562 for the discharge). Wallet header chip is stale until reload (see OBS-14 below).

### NEW DEFECTS (13-Nov)

| ID | Severity | Summary | Steps / evidence | Likely cause |
|---|---|---|---|---|
| **DEF-IPD-14** | 🟠 Medium | **The IPD Details dialog does not refresh after Shift Bed or Discharge** — it keeps showing pre-action data until closed and reopened (Admin **and** Reception). | (a) Shift `IPD-20261113-0002` from General G-101 to Private P-201 (₹300→₹600/day): ward/room fields update, but Final amount/Balance stay ₹550.00/₹50.00; reopening shows the correct **₹850.00/₹350.00**. (b) Discharge: toast "Patient discharged." appears, but the open dialog still says **Admitted**, "Provisional — amount payable if discharged today", still shows the live **Discharge** and **Collect** controls (Admin run: same, plus a stale "Refund due ₹450" under a provisional banner). Reopening shows Discharged + Final Bill. Risk: a user can act on a stale balance, click Discharge on an already-discharged stay, or miss that a refund is due. | After shift/discharge the dialog reloads the registration fields but not the `ipd-payments/{id}` / provisional-bill state, nor the registration `Status`. Re-fetch the registration and the bill (or close the dialog and refresh the list) in the shift and discharge success handlers. |
| **DEF-IPD-15** | 🟠 Medium | **Wallet page: the "⬆ Top up the wallet" link does not scroll to the amount field — it throws the user out to the clinic selector.** | Clinic Admin → Billing & Usage → Wallet → click **Top up the wallet**. Browser lands on `/select-clinic` ("My Clinics"); user has to re-enter the clinic. Reproduced 3×. The anchor's DOM is `href="#topup-amount"`; the page has `<base href="/">`, so it resolves to `https://localhost:7246/#topup-amount` (the app root), which redirects to select-clinic. Works only if the form is reached by scrolling manually. | In-page anchors don't work under `<base href="/">` in Blazor. Replace with a click handler that scrolls the amount input into view (`ScrollIntoView`/JS interop, or `@onclick` + focus) or use `NavigationManager`-safe absolute-path anchor (`href="@(Nav.Uri)#topup-amount"`). Same pattern is worth grepping elsewhere. |

### Observations (13-Nov)

| ID | Note |
|---|---|
| OBS-14 (still open) | Header wallet chip (₹) is not refreshed after metered charges (₹1,662 shown after the ₹100 discharge charge; ₹1,562 only after reload). Ledger and Billing page are correct. |
| OBS-16 | **"I've paid — check now"** shows only a transient "Checking with the payment provider…" toast; no follow-up message when the link is still unpaid, so the user can't tell the result. Suggest an explicit "Not paid yet" toast. |
| OBS-17 | Seen earlier today on entering a clinic right after login: a transient toast about a missing `X-Application-Id` on the select-clinic call. Harmless but noisy; not reproduced in the later runs. |

### Not yet executed (reason the report is not closed)

- **DEF-IPD-07 / G1b / G4** — cancel-with-deposit and cancel (API only, no UI); refund then Revenue report netting (M2).
- **H5** — Billing feature flag off hides the panel (restore flag afterwards).
- **L7** — bed-less stay; **F1** settings gate; other billing-cycle / mid-stay combinations.
- **Reception**: Print Provisional/Final Bill pages, service items add/remove, admit-without-doctor message (DEF-IPD-09 reception path), refund confirmation.
- Admissions search/pagination; delete ward/room/bed constraints; full usage-ledger reconciliation over all admissions (only the latest charges were checked).

### Test data created 13-Nov
`IPD-20261113-0001` (DUMMY Patient 009, Private Ward P-201, discharged, refund ₹450 confirmed, ₹100 discharge charge) and `IPD-20261113-0002` (DUMMY Patient 010, shifted G-101 → P-201, discharged, ₹850 settled). A ₹1,000 wallet top-up payment link (`rzp.io/rzp/mswivI1`) was created and left **unpaid** (no money moved). Wallet ₹1,562.00 at the end of the run. No clean-up performed.

---

## 13-Nov QA retest results + new work queued

**DEF-IPD-05 through DEF-IPD-13 all VERIFIED PASS** in a 13-Nov retest (Clinic Admin + Reception) — no regressions carried over from the 12-Nov fixes.

That retest raised one new defect needing a billing-correctness fix, one sizeable new capability request, four more defects, and four small UI fixes. Full detail (design, settings, API/UI changes, resolved open questions) is in `plan/ipd-billing-deposit-completion/PLAN.md` → **"Phase 4 — 13-Nov QA Retest Follow-on"**. Status here is the short version:

| ID | Title | Status |
|---|---|---|
| **DEF-IPD-19** | Cancelling an admission billed the full room charge + initial fee instead of ₹0, and a cancel with service items attached silently dropped them | **✅ Fixed 13-Nov, pending QA retest.** `IpdPayment.FinalizeAtCancellation` zeroes the whole bill (not just room charges); cancel now also reverses the ₹10 admission usage charge, and is blocked with a 400 ("Remove the service items or discharge the patient instead.") while any service item is attached. Retest: G1 with a ₹700 deposit should show Final ₹0, full ₹700 refund via Confirm Refund Given; try cancelling with a particular added — expect a clean 400, not a silently-dropped item. |
| **DEF-IPD-16** | Clinic-specific shift charging (grace window, manual override, per-segment write-off tracking) | **✅ Built 13-Nov, pending QA retest.** New `ClinicFeatureFlags` settings (grace window/rule, manual-charge mode, full-day rate rule — all default to values that reproduce today's bills exactly), `IpdBillCalculator` rewritten to consume a frozen grace/manual override, new `GET transfer-bed/preview` endpoint, Shift dialog now previews the standard charge + a manual amount field. Your "make it a clinic setting" answer is in as `IpdFullDayShiftRateRule`. **Correction made while building:** the default is `DestinationRoomRate`, not `HigherRate` as the plan first said — re-checked against the actual old code, which always picked the destination room unconditionally, not "whichever costs more." `HigherRate` only happened to look the same for the one direction you'd tested. **Also fixed as part of this:** the Period-display bug turned out to be a genuine room-charge calculation bug (UTC-midnight vs. local-midnight block boundaries), and the prorated-same-day-bills-₹0 bug (fixed by not rounding each tiny segment share before summing). Full detail in the plan's Phase 4b. |
| **DEF-IPD-18** | Reception has no UI path to a discharged/cancelled stay (List toggle does nothing) — can't print Final Bill or confirm a refund | **✅ Built 13-Nov, pending QA retest.** The List toggle now renders the same admissions table + View dialog admin's panel already has. |
| **DEF-IPD-14** | Details dialog stale after Shift/Discharge — risks a double discharge | **✅ Built 13-Nov, pending QA retest.** Both Shift and Discharge now refresh the still-open View dialog's registration + billing panel. |
| **DEF-IPD-15** | "Top up the wallet" link goes to the clinic selector (`href="#topup-amount"` vs `<base href="/">`) | **✅ Built 13-Nov, pending QA retest.** Replaced with a button that scrolls to and focuses the field via JS. Audited the rest of the app — no other `href="#…"` anchors found. |
| **DEF-IPD-17** | Deleting a room/ward/bed with occupied beds or admission history returns a generic 500 | **✅ Built 13-Nov, pending QA retest.** All three delete handlers now pre-check and return a clean 400/409 pointing at the existing Deactivate action. |
| Smaller items (wallet chip stale, no "check now" result message, search only on Enter, print Period shows 05:30 IST) | **✅ Built 13-Nov, pending QA retest.** See Phase 4d in the plan. |

**Verification (before the security pass below):** build green, full suite green — **1,824 tests passing** (up from 1,799; +25 new/updated for this round across `IpdBillCalculatorTests`, `TransferBedHandlerTests`, `CancelIpdRegistrationHandlerTests`, the three delete-handler test files, `UpdateClinicFeatureFlagsHandlerTests`, `ClinicFeatureFlagsTranslatorTests`, `UpdateClinicFeatureFlagsCommandValidatorTests`, and `IpdRegistrationsControllerTests`). One new EF migration (`AddIpdShiftChargingSettings`) created and applied to the local dev DB. **None of this has been clicked through in a browser yet** — only unit/handler-level coverage exists for the new UI (Shift dialog preview/manual fields, Settings page controls, List view). **Updated to 1,839 tests after the security pass immediately below, which found and fixed 2 real vulnerabilities.** Re-run the full test cases below (and the ones in the plan's Phase 4b) as the next QA pass.

---

## Security review — DEF-IPD-16/19 money-moving code paths (13-Nov, second pass)

You asked for a closer look specifically because this round touches money (shift write-offs, manual charge overrides, full admission refunds, usage-charge reversals) across UI, API and DB — not just a feature build. This section is a dedicated security/code-review pass over everything built for DEF-IPD-16 and DEF-IPD-19, done before any human QA click-through. **Two real, exploitable issues were found and fixed** during this pass (not found by QA — found by re-reading my own code adversarially, the way an attacker would). Both are now fixed, covered by regression tests, and the full suite is green. The rest of this section is the full test matrix — security and functional edge cases — for QA to run once a browser pass is possible.

### Vulnerabilities found and fixed this pass

| ID | Severity | Where | What was wrong | Fix |
|---|---|---|---|---|
| **DEF-IPD-20** | 🔴 High — cross-tenant data leak + corruption | `PreviewTransferBedHandler`, `TransferBedHandler` | Both handlers validated the caller's access against `registration.ApplicationId` (correct) but then used `request.ApplicationId` — the client-supplied `X-Application-Id` header — for every lookup afterward: the target bed's tenant check, the clinic's billing settings, the bed-transfer-history query, and the new history row's own `ApplicationId`. A user with roles in two clinics could select clinic A, call the preview or the real transfer-bed action against a **clinic B** admission id, and have the action validate the bed and read billing settings against **clinic A** instead — for the preview, that discloses clinic A's room rates while looking at a clinic B patient; for the real transfer, it would let a clinic B admission end up pointed at a clinic A bed with clinic A's settings applied, a genuinely corrupted cross-tenant record. | Every lookup in both handlers now uses `registration.ApplicationId` (the entity's own, real clinic), never `request.ApplicationId`. `PreviewTransferBedHandler` also gained the exact same "does this bed belong to this clinic, and is it active" check `TransferBedHandler` already had — a read-only preview must never be a weaker gate than the mutating action it previews. |
| **DEF-IPD-21** | 🔴 High — privilege escalation | `TransferBedHandler`, `PreviewTransferBedHandler`, `CancelIpdRegistrationHandler`, `DeleteBedHandler`, `DeleteRoomHandler`, `DeleteWardHandler` | All six sit behind a controller policy (`RequireReception` or `RequireClinicAdmin`) that — for this route shape (`{id:guid}/...`, not `{applicationId}/...`) — resolves which clinic to check the role against from the `X-Application-Id` **header**, not from the record the route id actually points to (`ClinicRoleAuthorizationHandler`, by design, for routes with no applicationId segment). Combined with the handler's own `CanAccess` check — which only proves the caller is *linked to* the record's real clinic, not that they hold the *right role* there — a user who, say, holds only `Doctor` (non-reception) in clinic B but `Receptionist`/`ClinicAdmin` in clinic A could select clinic A's header and still shift a bed, cancel an admission (full deposit refund + usage-charge reversal), or delete a bed/room/ward belonging to clinic B, by riding clinic A's role past the header-scoped policy check. | Added `IApplicationAccessValidator.HasRole(applicationId, roles)` — re-parses the JWT's `app_roles` claim for an **explicit** clinic id, not the header's. All six handlers now call it against the record's own `ApplicationId` as a second, record-scoped gate underneath the coarser controller policy; `TransferBedHandler`'s `AdminOnly` manual-charge check was switched to it too (it previously trusted a role list threaded in from the controller's own header-scoped `GetCurrentUserRoles()`, which has the identical flaw — that plumbing has been removed). |

**DEF-IPD-21 is architectural, not local to this feature** — it affects every `{id:guid}`-shaped route guarded by a `ClinicRoleRequirement` policy, which is most of this controller (also likely `DischargePatientHandler`, `UpdateIpdRegistrationHandler`, `CreateIpdRegistrationHandler`, and the equivalent handlers in other per-record controllers app-wide). **Only the six handlers above — the ones this round's money-affecting work actually touches — were hardened.** The same `HasRole(applicationId, roles)` capability now exists on `IApplicationAccessValidator` for anyone fixing the rest; flagging the broader exposure here rather than claiming the whole app is covered, since it is not. Worth its own dedicated pass — `ClinicRoleAuthorizationHandler`'s own comments show this exact bug class has bitten this codebase before (DEF-UM-08).

**Precondition for both:** the caller must already hold *some* legitimate role in **two or more different clinics** (e.g. multi-clinic staff, or any account a Clinic Admin invited into a second clinic). Neither is exploitable by a user who only ever belongs to one clinic. That narrows the practical attack surface, but money moves either way if it is exploited, so both were treated as blocking, not deferred.

### Security test matrix (for the next QA/API pass)

| ID | Layer | Scenario | Steps | Expected |
|---|---|---|---|---|
| SEC1 | API | Cross-tenant preview (DEF-IPD-20) | As a user linked to clinics A and B, with clinic A selected (`X-Application-Id`=A), call `GET /api/ipd-registrations/{clinicB-admission-id}/transfer-bed/preview?newBedId={clinicA-bed-id}` | 404 — never a 200 with clinic A's room rate |
| SEC2 | API | Cross-tenant transfer (DEF-IPD-20) | Same setup, `PATCH .../transfer-bed` with a clinic A `NewBedId` against the clinic B admission | 404; clinic B's admission is untouched — no bed pointer change, no new history row |
| SEC3 | API | Role-in-wrong-clinic — shift (DEF-IPD-21) | A user who is `Doctor` only in clinic B (not reception-capable there) but `Receptionist` in clinic A selects clinic A, calls `transfer-bed` on a clinic B admission | 401, not 200 — clinic A's role must not authorize an action on a clinic B record |
| SEC4 | API | Role-in-wrong-clinic — cancel (DEF-IPD-21) | Same user shape, calls `cancel` on a clinic B admission while clinic A is selected | 401; no refund issued, no usage-charge reversal, no wallet movement in either clinic |
| SEC5 | API | Role-in-wrong-clinic — delete | User is `ClinicAdmin` only in clinic A, holds any role in clinic B; selects clinic A, deletes a clinic B bed/room/ward by id | 404 (matches this handler family's existing "don't confirm existence" style); record not deleted |
| SEC6 | API | Manual-charge admin check uses the right clinic | `IpdManualShiftChargeMode=AdminOnly` in clinic B only; caller is `ClinicAdmin` in clinic A, `Receptionist` (non-admin) in clinic B; selects clinic A | 403 `manual_shift_charge_not_allowed` — clinic A's admin role must not authorize a manual charge on clinic B |
| SEC7 | API | Manual amount exceeds cap | `ManualChargeAmount` > the current segment's standard charge | 400, message states the exact cap in ₹; segment's charge is **not** frozen, bed is **not** shifted |
| SEC8 | API | Manual amount negative | `ManualChargeAmount: -1` | 400 from the validator, request never reaches the handler |
| SEC9 | API | Manual amount present, reason blank/missing | `ManualChargeAmount: 500, ManualChargeReason: null` (or `""`/whitespace) | 400 — "A reason is required when entering a manual charge amount." |
| SEC10 | API | Manual reason — script injection | `ManualChargeReason: "<script>alert(1)</script>"` | 400 from `SafeTextValidationRules` — rejected before it ever reaches the DB or a bill |
| SEC11 | API | Manual reason — bare link / markup | `ManualChargeReason: "javascript:alert(1)"` or `"<img src=x onerror=alert(1)>"` | 400, same validator |
| SEC12 | API | Manual charge mode Off | Any role, `IpdManualShiftChargeMode=Off`, any `ManualChargeAmount` supplied | 403 `manual_shift_charge_not_allowed`, regardless of who's asking |
| SEC13 | API | Manual charge with no current segment | Ward-only admission allocating its **first** bed (not a shift) with a `ManualChargeAmount` supplied | 400 "There is no current room charge to override." — not silently ignored, not a 500 |
| SEC14 | API | Grace window / rule tamper via Settings | `PUT clinic-feature-flags` with `IpdShiftGraceWindowMinutes: -30` | 400 from the validator |
| SEC15 | API | Grace rule without window on | `IpdShiftGraceWindowMinutes: 45` with `IpdShiftGraceRule: null` | 400 — "Select a grace rule once the grace window is turned on." |
| SEC16 | API | Replay / double-cancel | `cancel` the same admission twice in a row | Second call: 400 "Only an Admitted registration can be cancelled." — no second refund, no second usage-charge reversal |
| SEC17 | API | Double-finalize race | Two concurrent requests that both reach `FinalizeAtCancellation`/`FinalizeAtDischarge` for the same admission (simulated: call finalize twice) | Second call throws `InvalidOperationException` ("already finalized") — the bill cannot be zeroed/refunded twice |
| SEC18 | API | Usage-charge reversal idempotency | Cancel, then (if retried, e.g. a client timeout-and-retry) send the same cancel again | Wallet is credited **once**, not twice — `ReverseUsageChargeCommandHandler`'s existing idempotency check (keyed on the wallet ledger's `WalletReferenceType.UsageRefund` row) covers this; confirmed by code review, not newly built |
| SEC19 | API | Cancel with service items — bypass attempt | Add a particular to an admission, then try to `cancel` it anyway (instead of discharging) | 400 "Remove the service items or discharge the patient instead." — can't dodge billing for real items by cancelling |
| SEC20 | DB | Over-posting / mass assignment | Inspect `TransferBedRequest`/`CancelIpdRegistrationRequest` — confirm no field lets a client set `Status`, `CurrentBedId`, `FinalizedAt`, `AppliedChargeAmount` etc. directly | Confirmed by code review: only `NewBedId`/`Notes`/`ManualChargeAmount`/`ManualChargeReason` (transfer) and `Reason` (cancel) are client-settable; every money/state field is computed server-side |
| SEC21 | DB | SQL injection | Review every new query (`IpdBillCalculator`, the three `HasAnyFor*Async` methods, `FreezeCharge`/`SetManualCharge`) | Confirmed by code review: 100% parameterized EF Core LINQ, no raw SQL anywhere in this feature |
| SEC22 | DB | Negative room rate manufacturing a negative charge | Attempt to create/update a Room with a negative `PricePerDay` | 400 — pre-existing `GreaterThanOrEqualTo(0)` validators on both Create/Update Room, confirmed still in place; this closes off the only way a grace/manual amount could end up negative upstream |
| SEC23 | UI | Manual fields hidden ≠ manual fields disabled server-side | With `IpdManualShiftChargeMode=Off`, craft a `transfer-bed` request with a `ManualChargeAmount` directly (bypassing the UI, which hides the field) | 403 — the server never trusts the UI's hiding of the field as the real gate (see SEC12) |
| SEC24 | UI | XSS via a stored manual reason | If SEC10/11 were ever bypassed, confirm the reason still renders safely wherever it's shown (Shift dialog history, print pages) | Blazor's default text interpolation (`@item.Reason`) HTML-encodes automatically; no `MarkupString`/raw HTML is used for this field anywhere it's displayed |

### Functional edge-case matrix (for the next QA/API pass)

| ID | Scenario | Steps | Expected |
|---|---|---|---|
| EDGE1 | Grace window boundary — just inside | Grace window 120 min; shift at exactly 119 min 59 s after occupying the current room | Grace rule applies |
| EDGE2 | Grace window boundary — just outside | Same setting; shift at 120 min 1 s | Grace rule does **not** apply; plain `IpdFullDayShiftRateRule` resolution instead |
| EDGE3 | Manual amount exactly equals the standard charge | `ManualChargeAmount` == standard charge (boundary, not exceeding) | Accepted — the cap is inclusive |
| EDGE4 | Manual amount of exactly ₹0, with a reason | `ManualChargeAmount: 0`, a real reason | Accepted — a deliberate full write-off is a valid manual override |
| EDGE5 | Manual amount takes precedence over an active grace window | Shift happens inside the grace window **and** a manual amount is supplied | Manual wins — grace is only evaluated in the `else` branch once no manual amount is present |
| EDGE6 | Two shifts in sequence, each inside the grace window | Shift A→B (grace applies), then immediately B→C (grace applies again) | Each segment freezes its **own** standard/applied/write-off independently — no bleed-over from the first shift into the second |
| EDGE7 | Prorated same-day rapid shift + discharge | Admit, shift, discharge all within a few seconds, `Prorated` mode | Total room charge is `> ₹0` and `≤` the higher of the two rooms' rates — regression for the exact bug reported (covered by `CalculateRoomChargesAsync_PerCalendarDay_Prorated_RapidSameDayShiftAndDischarge_NeverRoundsToZero`) |
| EDGE8 | `PerCalendarDay` local-midnight alignment | Admission spanning a local midnight boundary, under any UTC offset | Block boundaries land on local midnight, not UTC midnight — covered by `IpdBillCalculatorTests`' `Local(...)`-based fixtures, which are offset-independent by construction |
| EDGE9 | Ward delete blocked by an occupied bed in *any* room | A ward with 3 rooms; only one bed in the third room is occupied | Delete is still blocked — the check walks all rooms' beds, not just the first |
| EDGE10 | Settings round-trip: turning the grace window off clears the rule | Set window=90 + a rule, save; then set window=0, save | The saved `IpdShiftGraceRule` comes back `null`, not the stale rule — prevents a rule "reappearing" if the window is reopened later without re-picking one |
| EDGE11 | Provisional vs. Final write-off consistency | Compare `WriteOffTotal` on the live Provisional Bill (pre-discharge) against the Final Bill after discharge, for the same frozen segment | Identical — both read the same frozen `IpdBedTransferHistory` fields through the same calculator, so they cannot drift apart |
| EDGE12 | Concurrent writes to the same admission (e.g. double-click Shift) | Two near-simultaneous `transfer-bed` calls for the same admission | **Not independently tested at this layer** — relies on `RowVersion` optimistic concurrency already configured on `IpdRegistration`/`IpdBedTransferHistory`/`IpdPayment`; flagged as a known gap in automated coverage, not a known bug |

**Coverage note:** every SEC/EDGE row above that maps to a unit/handler test is already covered (see the `+25 new/updated` count above, plus this pass's additions: `PreviewTransferBedHandlerTests` — new file, 8 tests — and 3 new regression tests across `TransferBedHandlerTests`/`CancelIpdRegistrationHandlerTests`/the three delete-handler test files for DEF-IPD-21). Rows with no unit-test equivalent (SEC20–SEC22, SEC24, EDGE12) are marked above as verified by code review instead, and should still get a manual pass once DEF-IPD-16 reaches a browser.

---

## What changed since the 12-Nov QA pass

| ID | Was | Fix applied | Where to retest |
|---|---|---|---|
| **DEF-IPD-05** | Deposit & Bill panel stuck on "Loading billing details…"; Final Bill unreachable on Discharged admissions | `OpenViewDialog` was `async void` — Blazor's renderer doesn't track that as a pending render, so the UI never refreshed after the data loaded. Changed to `async Task`, and `LoadBillingPanelAsync` now explicitly calls `StateHasChanged()` when it finishes, as a backstop regardless of caller. | Re-open K1/K3 and L1–L5 in the UI (not via API this time) |
| **DEF-IPD-06** | Print Provisional Bill → "Your session has expired" (401) | Both print pages open in a brand-new browser tab, which boots a separate WASM app instance with no token until one is read back from storage — and they were doing that read in `OnInitializedAsync`, too early. Moved to `OnAfterRenderAsync(firstRender)` with an explicit `AuthState.InitializeAsync()` / `ClinicContext.InitializeAsync()` first, mirroring the same fix already used by `PrescriptionPrint.razor`. | K2, L6 |
| **DEF-IPD-07** | Cancelling an admission with deposits already collected left that money with no refund path, still counted as clinic revenue | `CancelIpdRegistrationHandler` now finalizes the bill (same `FinalizeIpdBillCommand` a normal discharge uses) at the moment of cancellation. A cancelled admission now gets a locked Final Bill in the same popup, with Collect Balance / Confirm Refund available exactly as for a discharged patient. | G1 with a deposit collected first, then re-open the admission and confirm the refund; also resolves OBS-11 |
| OBS-4 | Cancel twice / cancel a discharged admission returns 400, not the 409 this report predicted | Not a bug — `IpdRegistration.Cancel()` intentionally mirrors `Discharge()`'s exact exception pattern (`DomainValidationException` → 400) for "wrong status" preconditions; that's the established, consistent behavior. **Corrected the G2/G3 expected status below from 409 to 400** — the report's prediction was wrong, not the code. | — (documentation fix only) |
| OBS-5 | Shifting a patient into their own current bed returned "This bed is already occupied" | `TransferBedHandler` now checks `registration.CurrentBedId == NewBedId` before the generic occupied check and throws a specific "The patient is already in this bed." message. | F — try shifting into the same bed the patient is already in |
| OBS-6 | `/reports/ipd` page header read "Dashboard" | `AuthorizedLayout`'s title lookup had no entry for that route; added `"IPD Reports"`. | M1 |
| OBS-7 | After a refund is confirmed (or a balance collected), the Discharge Summary report showed the stale locked `BalanceDue` (e.g. "₹-1,200"), reading as still-outstanding money | `IpdPayment.BalanceDue` itself is unchanged (it's a deliberate locked audit snapshot, and the Final Bill print's "Refund due"/"Balance" labels already give it context there) — but the Discharge Summary report now displays **0** for any row where `Settled` is true, instead of the raw snapshot. | M3 |
| OBS-8 | Pre-feature discharges with no `IpdPayment` row showed "—" / "Outstanding", which reads as money owed rather than "no bill exists" | Added a `HasBill` flag to the discharge summary row; the report now shows **"No bill"** for those rows instead of "Outstanding". | M3 |
| OBS-9 | Deposit/discount inputs kept their value after a successful action; no discount line shown in the live panel | Discount input now resets to 0 after a successful **Apply discount**, matching how the deposit input already behaved; added a "Discount applied" line to the live Provisional panel, mirroring what the print pages already show. | H, I |

| N/A (found 13-Nov, not from a QA pass) | Wallet top-up link creation is async (a background task calls Razorpay); the dialog had no way to show the URL once it existed, and "I've paid — check now" was shown and clickable before the link existed, where it just errors. Also found while fixing this: `GetOpenLinkForClinicAsync` — the one query all six "create a payment link" handlers share to enforce "at most one open link per clinic" — only matched `Purpose == Renewal \|\| AdminAssigned`, so a wallet top-up or registration fee was *never* recognized as already open; every repeat click created a brand new payment row and a brand new Razorpay link instead of returning the existing one. | Fixed the purpose filter to cover all four `IsClinicLink` purposes. Added a `GET wallet/top-up/active` endpoint (mirrors the existing `registration-fee/active` pattern) and had `TopUpDialog.razor` poll it every 3s (capped at 40 attempts) until the link appears, instead of requiring a re-click. "I've paid — check now" is now hidden until the link actually exists, and the dialog now also recovers an in-flight top-up on page load instead of starting blank. | Create a top-up, don't touch anything — the link should appear on its own within a few seconds; reopening the Wallet page mid-creation should also pick it up |

| N/A (found 13-Nov, not from a QA pass) | After the clinic actually paid a top-up link, the link stayed visible and payable again — the panel never reflected that payment had happened, and never showed the wallet being credited. Root cause: `ConfirmPayment` (marks the payment `Paid`) and `CreditWallet` (actually moves the wallet balance) are separate, sequential background steps, and `TopUpDialog`'s polling stopped the moment the link first appeared — it never kept watching for the `Paid` transition at all. | Added `GET wallet/top-up/{id}` (unlike the "active" endpoint, this still returns a `Paid` payment instead of treating it as "no longer open"). The dialog now polls this continuously through the whole lifecycle — link not ready → link ready/payable → paid (link and Copy/Check-now disappear immediately, replaced by "✓ Payment received — crediting your wallet…") → once the wallet balance actually moves, a success toast fires, the host page's balance and nav wallet chip refresh, and the panel resets to its empty ready-for-a-new-top-up state. | Pay a top-up link for real (or via Razorpay's test-mode instant payment) and watch the panel without touching anything — it should move through all three states on its own within ~15–20 seconds of payment |

**Deliberately not changed this round:**
- **OBS-10** (first bed-history row's `transferredByUserId` is the doctor id, not a distinct "admitting staff" id) — fixing this properly means adding a new actor-id field end-to-end (command → controller → handler), since no such id currently exists on `CreateIpdRegistrationCommand`. Low impact (audit-trail label only, not a money or access-control issue) relative to that change's size — left as a known limitation for a future pass.
- **OBS-12** (admission-number local-date fix, OBS-1) still can't be verified without a post-midnight admission — unchanged, time-dependent.

| Item | Detail |
|---|---|
| Repo | TenantCore.App |
| Environment | Local — `https://localhost:7246` |
| Clinic | Clinic One Care (`2d76bdc2-4843-4d11-9120-1f81aa32ec86`) |
| Role tested (10-Nov) | Clinic Admin (`Clinic1 Admin`). Reception: not executed that session. |
| Role to test (this update) | **Both** Clinic Admin and Reception — discharge, bed-shift and billing actions are now open to `RequireClinicStaff` (Receptionist/Doctor/ClinicAdmin/ClinicManager), not just clinical/admin roles. Run every new section under a reception login at least once. |
| Last executed | 12-Nov-2026 (afternoon–evening IST) — sections F–M as **Clinic Admin** via Chrome UI plus in-page API calls where the UI was blocked. 10-Nov: sections A–E. |
| This update written | 12-Nov-2026 — adds sections F–M, retest guidance, and updated Defects/Gaps status. No new UI click-through was performed when writing this update; treat every "Expected" below as a code-grounded prediction for QA to verify, not a result. |
| Prior reports | `OPD_*_TestReport.md` in this folder |

---

## What changed since the 10-Nov run

| Area | 10-Nov | Now |
|---|---|---|
| Bed shift | API only, no UI, 3 defects (ghost bed, double-occupancy, garbage data accepted) | **Shift Bed** button in the admission popup; dedicated `TransferBedCommand` validates the target bed and moves occupancy atomically |
| Admission status | Open `PUT status=<int>` could trap an admission in `Transferred` | Status moves only through named actions (Discharge / Transfer / Cancel) |
| Discharge role | `RequireClinical` (reception excluded) | `RequireClinicStaff` — **reception can now discharge** |
| Deposit | Did not exist | **Deposit & Bill** panel in the admission popup — any amount, any number of times, while Admitted |
| Discount | Did not exist | Same panel — reception or doctor can apply a discount, capped at the total bill |
| Particulars (service items) | Did not exist | Same panel — add/remove clinic-defined service items against the admission |
| Patient bill | Did not exist | **Provisional Bill** (live, printable any time while Admitted) and **Final Bill** (locked at discharge, with balance-collect / refund-confirm actions) |
| Room billing policy | Not applicable (no billing) | Two new clinic settings (**Settings → Feature Flags → IPD Room Billing**) — both start blank, and **a clinic cannot allocate a bed until both are set** |
| Reports | None | New **IPD Reports** page (Census / Revenue / Discharge Summary) |
| Admission number date | UTC-based, could show "yesterday" for a late-night admission | Fixed — the number's date prefix now uses local calendar day |
| Room type seed data | Dummy rooms showed `"General"`, not a real dropdown option | Fixed — relabeled to `"General (4-bed)"` |

---

## ⚠️ Do this first, or everything below will 409

Both new clinic settings have **no default**. Before testing *anything* involving a bed (new admission with a bed, or a bed shift), go to **Settings → Feature Flags → IPD Room Billing** and pick a value for both **Room billing cycle** and **Mid-stay bed/room change charge** (any combination is fine for testing). Until both are set, admitting with a bed or shifting a bed returns a 409 with the message *"Configure IPD room billing settings (Clinic Settings) before allocating a bed to a patient."* A **ward-only admission with no bed** is never blocked by this — only an actual bed allocation is.

---

## What exists in the IPD module (updated)

| Area | Present | Who |
|---|---|---|
| Ward / Room / Bed CRUD (`/admin/wards`) | Yes | Clinic Admin only |
| Admit patient (`/ipd`, POST) | Yes | Reception + above |
| Edit admission (doctor/notes only — status/bed removed from this action) | API + no dedicated UI form (admit-time fields are editable via the admit dialog only) | Reception + above |
| **Shift Bed** (new — UI button in admission popup) | Yes | Reception + above |
| **Cancel admission** (new) | **API only, no UI button** | Reception + above |
| Discharge (PATCH, UI button) | Yes | **Reception + above** (was clinical-only) |
| Admission details popup | Read-only fields + new Deposit & Bill panel | All roles can view; billing actions need `RequireClinicStaff` |
| IPD dashboard widgets (admin dashboard, `/ipd` cards) | Yes | Admin |
| Metered usage charge on admit / discharge (`/billing/usage`) | Yes | Admin |
| **Deposit collection** (new) | Yes — in the Deposit & Bill panel | Reception + above |
| **Discount** (new) | Yes — same panel | Reception + above |
| **Particulars / service items** (new) | Yes — same panel, add + remove (with confirm) | Reception + above |
| **Provisional Bill** (new) | Yes — live view + print, any time while Admitted | Reception + above |
| **Final Bill, balance collection, refund confirmation** (new) | Yes — shown after discharge | Reception + above |
| **IPD Reports — Census / Revenue / Discharge Summary** (new) | Yes — `/reports/ipd` | Reception + above |
| **IPD room billing settings** (new) | Yes — `/settings/feature-flags` | Clinic Admin only |
| Doctor panel | Hidden (no Wards, IPD, or IPD Reports menu) | Doctor |

---

## Test cases — original run (10-Nov, historical)

### A. Ward / Room / Bed setup (Admin)
| # | Case | Result |
|---|---|---|
| A1 | Create ward "QA Test Ward" | ✅ "Ward created"; ward count 3 → 4 |
| A2 | Create room QA-101, Semi-Private, ₹800/day under that ward | ✅ "Room created"; shows ₹800/day |
| A3 | Add bed QA-B1 | ✅ "Bed added"; bed total 31 → 32 |
| A4 | Add duplicate bed QA-B1 in same room | ✅ Rejected: "A bed with this number already exists in this room." |
| A5 | Add bed QA-B2 (and QA-B3 via API) | ✅ |
| A6 | `/ipd` ward cards show new ward and bed counts | ✅ 34 beds total, QA Test Ward 3 beds |

### B. Admission / allocation
| # | Case | Result |
|---|---|---|
| B1 | Submit admit form empty | ✅ "Please select a patient." |
| B2 | Patient search by name | ✅ Finds "DUMMY Patient 001"; "no patients found" message links to Patient Registry |
| B3 | Admit with doctor, ward → room → bed cascade, ₹2,000 initial fee | ✅ Admission `IPD-20261109-0001`; cascade filters rooms and beds correctly |
| B4 | Bed becomes occupied; ward card 0% → 50% | ✅ |
| B5 | Admission details popup | ✅ Shows patient, doctor, ward/room/bed, date, status, initial fee ₹2,000 |
| B6 | Admit second patient into occupied bed (API) | ✅ 409 "This bed is already occupied." |
| B7 | Admit already-admitted patient again (API) | ✅ 409 "This patient is already admitted…" |
| B8 | Negative initial fee (API) | ✅ 400 validation |
| B9 | Reception creates admission | ⛔ Not executed in this session — **retest as F8/G below, now with discharge too** |

### C. Bed shifting (original API-level findings — superseded by Section F below)
| # | Case | Result |
|---|---|---|
| C1 | Shift from UI | ⛔ No UI existed — **now exists, see F** |
| C2 | Shift P1 B1 → B2 (PUT) | ✅ 200, record shows B2 |
| C3 | After shift, old bed freed and new bed marked occupied | ❌ **DEF-IPD-01 — fixed, retest F3/F6** |
| C4 | Shift into a bed occupied by another admitted patient | ❌ **DEF-IPD-02 — fixed, retest F4** |
| C5 | Shift to a ward/room/bed that does not exist | ❌ **DEF-IPD-03 — fixed, retest F5 (now id-based, not name-based, so this exact case no longer applies — see F5)** |
| C6 | Set status = Transferred via PUT, then discharge | ❌ **DEF-IPD-04 — fixed by design change, retest F7** |

### D. Discharge
| # | Case | Result |
|---|---|---|
| D1 | Discharge via UI with notes | ✅ "Patient discharged."; status Discharged; Discharge button removed |
| D2 | Discharge twice | ✅ 400 "Patient must be in Admitted status to discharge." |
| D3 | Edit a discharged admission | ✅ 400 "Cannot update a discharged or cancelled IPD registration." |
| D4 | Discharged patient's bed returns to available | ⚠️ Only for the bed the record currently points to; the original bed stayed stuck — **this was DEF-IPD-01, fixed, retest F6** |

### E. Dashboards and billing
| # | Case | Result |
|---|---|---|
| E1 | Admin dashboard totals (wards 4, rooms 16, beds 34) | ✅ |
| E2 | Admin dashboard / `/ipd` occupied count after all patients discharged | ❌ Showed **1 occupied** with 0 admitted (ghost bed from DEF-IPD-01) — **fixed; the specific QA-B1 ghost bed from this run was released by the fix's migration backfill, verified directly against the DB (0 beds occupied with nothing pointing at them). Retest with a fresh shift (F6) to confirm the live code path too, not just the one-time backfill.** |
| E3 | Usage page: IPD admission ₹10 ×2, IPD discharge ₹100 ×2 | ✅ Matches wallet drop ₹952 → ₹732 |
| E4 | Admission number date | ℹ️ OBS-1 — **fixed, retest E4-retest below** |

---

## Test cases — NEW (12-Nov build) — results in 'Execution results' below

### F. Bed Shift (UI) — retest of DEF-IPD-01/02/03
| # | Case | Steps | Expected |
|---|---|---|---|
| F1 | Settings gate blocks shift | With **IPD Room Billing** settings left blank, admit a patient to a bed, then open their details and click **↔ Shift Bed** → pick a new bed → **Shift** | 409 with the "Configure IPD room billing settings…" message; no change to either bed |
| F2 | Configure settings | Set both IPD Room Billing dropdowns (any values) and save | "Feature flag updated." toast; dropdowns keep the chosen values on reload |
| F3 | Shift to a free bed | Admit P1 into Bed A. Open details → **↔ Shift Bed** → select Bed B (different room) → optional note → **Shift** | Success toast; popup's WARD/ROOM/BED fields update to Bed B; Bed A shows free and Bed B shows occupied on the `/ipd` ward map immediately after |
| F4 | Shift into an occupied bed | Admit P2 into Bed C. Try to shift P1 into Bed C | Clear error shown in the dialog (not a silent failure); P1 stays on their current bed; Bed C still shows P2 |
| F5 | Shift to a bed that doesn't exist | Not reachable from the UI (the dropdown only lists real available beds) — if testing via API, `PATCH /api/ipd-registrations/{id}/transfer-bed` with a random `NewBedId` | 404, not 200 — confirms DEF-IPD-03 is closed (the field is now a real bed id, not a free-text name) |
| F6 | Discharge after a shift (DEF-IPD-01 regression) | Shift P1 from Bed A → Bed B, then discharge P1 | Bed B (the *current* bed) is freed; Bed A (already freed at shift time) stays free — **no ghost occupied bed**; `/ipd` occupied count drops correctly |
| F7 | No status trap (DEF-IPD-04) | Shift a patient's bed a few times, then discharge | Discharge always succeeds in one step — there is no `Transferred` status to get stuck in; the admission's Status badge reads "Admitted" the whole time between shifts, only changing to "Discharged" at the end |
| F8 | Reception can shift and discharge | Log in as reception, repeat F3 and then discharge the same patient | Both actions succeed (previously discharge would have been blocked — role fix) |
| F9 | Shift a discharged admission | Try **↔ Shift Bed** on an already-discharged admission | Button is not shown on a non-Admitted admission (check the popup) — if forced via API, 409 |

### G. Cancel Admission (API only — new capability)
| # | Case | Steps | Expected |
|---|---|---|---|
| G1 | Cancel an admitted patient | `PATCH /api/ipd-registrations/{id}/cancel` with `{"reason": "Wrong patient admitted"}` on an Admitted admission with a bed | 200; Status becomes `Cancelled`; the bed is freed; **no wallet/usage reversal happens** (by design — confirm this is still the scope you want) |
| G1b | Cancel with a deposit already collected (DEF-IPD-07 retest) | Admit, collect a deposit (e.g. ₹700), then G1 on that admission. Open its details in the UI. | Status `Cancelled`; the popup now shows a **Final Bill** (not blank) — Final amount ₹0/InitialFee-only depending on what was charged, and since the deposit exceeds it, a **Confirm Refund Given** button. Confirming it should remove the stranded-money condition — recheck the Revenue report (M2) afterward: the refunded amount should appear under "Refunded", not still sit in "Deposits" with nothing netting it out |
| G2 | Cancel twice | Repeat G1 on the same admission | **400** — "Only an Admitted registration can be cancelled." (corrected from this report's original 409 prediction — see "What changed" above; `Cancel()` intentionally matches `Discharge()`'s own 400-on-wrong-status pattern) |
| G3 | Cancel a discharged admission | Try G1 on a Discharged admission | 400, same message as G2 |
| G4 | No UI path | Confirm there is no Cancel button anywhere in `/ipd` | Expected — flag if you want one added |

### H. Deposit collection (new)
| # | Case | Steps | Expected |
|---|---|---|---|
| H1 | Collect a deposit | Open an Admitted patient's details → **Deposit & Bill** panel → enter an amount in "Deposit amount" → **Collect** | "Deposit collected." toast; amount appears under "DEPOSITS COLLECTED"; Provisional Bill's "Balance"/"Refund due" figure updates |
| H2 | Multiple deposits | Collect 2–3 more deposits of different amounts on the same admission | Each appears in the list; Balance keeps recalculating correctly (sum of all deposits) |
| H3 | Zero / blank amount | Try to collect with the field empty or 0 | **Collect** button is disabled — can't submit |
| H4 | Deposit on a discharged admission | Not reachable from the UI (panel switches to Final Bill view after discharge) — if testing via API, `POST /api/ipd-deposits/{id}` after discharge | 409 — "This action is only available while the patient is admitted." |
| H5 | Billing disabled | Turn off the clinic's **Billing** toggle (Settings → Feature Flags), then open an admission | The whole Deposit & Bill panel is hidden — matches how OPD billing hides the same way |

### I. Discount (new)
| # | Case | Steps | Expected |
|---|---|---|---|
| I1 | Apply a discount | In the Deposit & Bill panel, enter a discount amount → **Apply discount** | "Discount applied." toast; Final amount and Balance update immediately |
| I2 | Discount larger than the bill | Enter a discount bigger than Initial Fee + Room Charges + Particulars | Error — "Discount cannot exceed the total bill amount." |
| I3 | Reception can discount | Repeat I1 logged in as reception | Succeeds — same policy as OPD discount |

### J. Particulars / service items (new)
| # | Case | Steps | Expected |
|---|---|---|---|
| J1 | Add a service item | In the panel, pick an item from "Add a service item…" (populated from the clinic's active Particulars) → **Add** | "Service item added." toast; item appears in the list with its amount; Particulars Total and Final Amount increase |
| J2 | Add several | Add 2–3 different items | All listed; totals correct |
| J3 | Remove a service item | Click **✕** next to a listed item | A **confirm dialog** appears ("Remove service item? Remove <name> (₹amount) from this admission's bill?") — confirm | Item removed; totals decrease back |
| J4 | Cancel the remove | Click ✕, then **Cancel** in the confirm dialog | Nothing removed |
| J5 | No active particulars configured | If the clinic has no active Particulars, check the dropdown | Shows only the placeholder "Add a service item…" — confirm this doesn't error |

### K. Provisional Bill (new)
| # | Case | Steps | Expected |
|---|---|---|---|
| K1 | View while admitted | Open any Admitted patient's details | "Provisional — amount payable if discharged today" banner; Final Amount and Balance/Refund figures shown |
| K2 | Print | Click **🖨️ Print Provisional Bill** | Opens a new tab/window at `/ipd/provisional-bill-print?id=...`; shows clinic name, patient, admission number, the same figures, and a day-by-day room-charge breakdown table; **✕ Close** and **🖨️ Print** buttons work |
| K3 | Numbers match the panel | Compare the printed figures to the panel at the same moment | Should match exactly (same calculation, same instant) |
| K4 | Changes over time | Wait past a day boundary (or use the clinic's 24-hour-block setting) without touching the admission, then reopen | Room Charges / Final Amount increase to reflect the extra day/block — confirms the bill really is live, not cached |

### L. Discharge, Final Bill, Settle & Refund (new)
| # | Case | Steps | Expected |
|---|---|---|---|
| L1 | Discharge locks the bill | Discharge an admission that has a positive balance due (bill > deposits) | Discharge succeeds; reopening the admission now shows a **Final Bill** section instead of Provisional; a **Collect Balance** button appears |
| L2 | Collect the balance | Click **Collect Balance** | "Balance collected." toast; button disappears; status reads fully settled |
| L3 | Discharge with excess deposit | Collect a deposit larger than the eventual bill, then discharge | Final Bill shows "Refund due" with a **Confirm Refund Given** button instead of Collect Balance |
| L4 | Confirm the refund | Click **Confirm Refund Given** | "Refund confirmed." toast; button disappears; status reads fully settled |
| L5 | Discharge with nothing owed either way | Discharge an admission where deposits exactly equal the final bill | Final Bill shows no outstanding action — just "Fully settled" |
| L6 | Print the Final Bill | Click **🖨️ Print Final Bill** | Opens `/ipd/final-bill-print?id=...`; shows "FINAL BILL — Locked at discharge", the finalized figures, and settlement status |
| L7 | Ward-only admission (no bed ever allocated) | Admit without selecting a bed, then discharge | Room Charges = ₹0 throughout; discharge and billing still work normally — confirms the billing-settings gate never blocked a bed-less stay |

### M. IPD Reports (new)
| # | Case | Steps | Expected |
|---|---|---|---|
| M1 | Open the reports page | Nav menu → **IPD Reports** (under the billing section, both Admin and Reception panels) | Census tab loads automatically with current occupancy totals and a per-ward table |
| M2 | Revenue report | Switch to the **Revenue** tab, pick a date range covering today, click **Generate** | Shows Deposits / Balance Collected / Refunded / Net Revenue figures reflecting the deposits and settlements made in sections H–L |
| M3 | Discharge Summary report | Switch to **Discharge Summary** tab (same date range already applied — **do not click Generate again to confirm it reuses the cached fetch**) | Table lists each discharge in range with patient name, admission/discharge dates, final amount, balance, and Settled/Outstanding — confirms patient details appear as required |
| M4 | No re-fetch per tab switch | Switch between Revenue ↔ Discharge Summary ↔ Census repeatedly without clicking Generate again | No loading spinner / no new network calls on tab switch alone — only Census and the one Generate click fetched data |
| M5 | Empty range | Pick a date range with no activity, Generate | Tables show "No discharges in this range." / zero totals, not an error |
| M6 | Doctor cannot see this page | Log in as a doctor-only user, check the nav menu | No "IPD Reports" item |

---

## Execution results — 12-Nov-2026 (Clinic Admin)

Method: Chrome UI against `https://localhost:7246`; where the UI was blocked by a defect, the same action was called through the API (marked *API*). Reception was run afterwards as `Recep One` (see 'Reception results' below).

| Section | Result | Notes |
|---|---|---|
| F1/F2 Settings gate | ✅ Pass | Allocation/shift return 409 until both IPD Room Billing settings are set; saved values persist. |
| F3 Shift to free bed | ✅ Pass | Old bed freed, new bed occupied. |
| F4 Shift into occupied bed | ✅ Pass | 409, nothing changes. (Needs a real `bedId` — free-text ward/room/bed names are ignored by the admit API.) |
| F5 Non-existent bed | ✅ Pass | Random `BedId` → 404 (*API*). |
| F6 Discharge after shift | ✅ Pass | No ghost occupied bed. |
| F7 No status trap | ✅ Pass | Discharge works after several shifts. |
| F8 Reception shift/discharge | ✅ Pass (API) | Reception shift 200, discharge 200; bed freed on the `/ipd` map. UI click-through not repeated. |
| G1–G3 Cancel | ⚠ Partial | Cancel works and frees the bed. Repeat / discharged cancel returns **400, not 409** (OBS-4). **DEF-IPD-07** for deposits on cancelled admissions. G4: no Cancel button (as expected). |
| H Deposits | ✅ Pass (H1–H4) | Multiple deposits sum correctly; blank/0 disabled; discharged → 409. **H5 not executed** (would require toggling the Billing flag). |
| I Discount | ✅ Pass | Field is `discount`; over-total rejected. I3 reception discount ✅ (200). |
| J Particulars | ✅ Pass | Add / remove with confirm / cancel behave as expected. Particular delete constraints not tested. |
| K1/K3 Provisional bill | ⚠ Blocked in UI | Panel often stuck on "Loading billing details…" (**DEF-IPD-05**). Figures verified through API. |
| K2 Print Provisional Bill | ❌ Fail | **DEF-IPD-06** — "Your session has expired" (401). |
| L1–L5 Discharge / settle / refund | ✅ Pass via *API* | UI panel unreachable on Discharged admissions (DEF-IPD-05), so final bill, collect-balance and refund were exercised through the API. |
| L6 Print Final Bill | ⏸ Not executed | Blocked behind DEF-IPD-05/06; page not opened. |
| L7 Bed-less stay | ⏸ Not executed |  |
| M1/M2 Census, Revenue | ✅ Pass (see Revenue note) | Revenue 14-Oct→12-Nov: Deposits ₹5,700, Balance collected ₹1,400, Refunded ₹1,200, Net ₹5,900. |
| M3 Discharge Summary | ✅ Pass | Lists patient, dates, final amount, balance, status. See OBS-8. |
| M4 No re-fetch on tab switch | ✅ Pass | Switching Revenue ↔ Census ↔ Discharge Summary produced zero `/api/ipd-reports` calls; one Generate fetches all three. |
| M5 Empty range | ✅ Pass | "No discharges in this range." |
| M6 Doctor nav | ✅ Pass (code review) | Doctor panel in `NavMenu.razor` has no IPD Reports/IPD & Beds link. Reception and Admin panels do. Not executed with a doctor login. |

### Original defects — retest

DEF-IPD-01, 02, 03 and 04 are **all VERIFIED FIXED** (F3/F6, F4, F5, F7 above).

## NEW DEFECTS (12-Nov)

| ID | Severity | Title | Steps / evidence | Fix applied (12-Nov, same day) | Retest |
|---|---|---|---|---|---|
| **DEF-IPD-05** | 🔴 High | Deposit & Bill panel stuck on "Loading billing details…"; Final Bill unreachable on Discharged admissions | Open an admission's View dialog (Admitted or Discharged). Panel shows the spinner text indefinitely; network calls succeed. Re-opening/closing the Shift dialog forces a re-render and the data appears (workaround). | `OpenViewDialog` changed from `async void` to `async Task`; `LoadBillingPanelAsync` now calls `StateHasChanged()` explicitly when it finishes. | ⏳ K1/K3, L1–L5 (UI, not API) |
| **DEF-IPD-06** | 🔴 High | Print Provisional Bill page shows "Your session has expired" | Click **🖨️ Print Provisional Bill** while logged in with a valid token. Both API calls from the print page return 401. | Both Ipd print pages now hydrate `AuthStateService`/`ClinicContextService` from storage in `OnAfterRenderAsync(firstRender)` before calling the API — same fix already used by `PrescriptionPrint.razor` for the identical new-tab problem. | ⏳ K2, L6 |
| **DEF-IPD-07** | 🟠 Medium/High | Cancelling an admission with collected deposits strands the money | Admit, collect deposit (₹700 on `P006`), cancel. No final bill, no refund/settle path; the deposit remains counted in the Revenue report (Deposits ₹5,700 includes it). | `CancelIpdRegistrationHandler` now finalizes the bill at the cancellation moment (same `FinalizeIpdBillCommand` as discharge) — a cancelled admission gets a real locked Final Bill, with Collect Balance / Confirm Refund available in the same popup. | ⏳ G1b |
| **DEF-IPD-08** | 🔴 High | Reception UI has no IPD billing, shift or bed-detail actions (API allows them) | Log in as Receptionist → `/ipd` → click an occupied bed. Only "Discharge Patient" is offered. No Shift Bed, Deposit & Bill panel, discount, particulars, provisional/final bill, collect balance or refund, no print links. | **Fixed 13-Nov.** `OnBedClick` (now `OnBedClickAsync`) routes reception's bed click into the exact same `OpenViewDialog` the admin admissions table already uses — that dialog has no role-gating inside it at all (only status checks), it was simply never wired up outside the admin branch. One consequence: that dialog never had a Discharge action of its own (admin discharges from the table row, which reception has no equivalent of) — added a **Discharge** button inside the dialog itself, reusing the same confirm-dialog flow the table's button already calls. The old read-only "Bed Details" dialog is kept only as a fallback for the rare case the bed/room-name match behind it finds no admission. | ⏳ **QA retest needed.** As reception, click an occupied bed — Shift Bed, Deposit & Bill, discount, particulars, provisional/final bill, collect balance, refund, print and Discharge should all now be reachable from the same popup. Also confirm the fallback "Bed Details" dialog still behaves correctly for the no-matching-admission edge case. |
| **DEF-IPD-09** | 🟠 Medium | "Attending Doctor (optional)" is required; raw validation text shown | Admit dialog as reception, leave Doctor as "— No Doctor —" → **Confirm Admission** | **Fixed 13-Nov.** Chose "make the UI genuinely require it" over relaxing the server validator — `IpdRegistration.DoctorUserId`/`DoctorName` are non-nullable at the domain layer, and doctor-less admission has never actually been possible through this app (the validator always rejected it), so relaxing it would be new, untested scope rather than a fix. Both admit dialogs (admin and reception) now label it **"Attending Doctor \*"**, the placeholder reads "— Select Doctor —", and submitting with none selected shows a friendly "Please select an attending doctor." toast before the request is ever sent — same pattern already used for the patient-selection check. | ⏳ **QA retest needed.** Leave Doctor unselected in either admit dialog (admin and reception) and try to submit — no raw server text should ever appear, only the friendly toast. |
| **DEF-IPD-10** | 🔴 High | Wallet **Create payment link** button is always disabled (wallet can't be topped up from the UI) | Clinic Admin → Billing & Usage → Wallet → enter/pick any amount (e.g. ₹5,000). Button stays greyed out; clicking sends **no request**. DOM shows `disabled="False \|\| _amount <= 0m"`. | `Components/UsageBilling/TopUpDialog.razor` L40: `disabled="@_busy \|\| _amount <= 0m"` — Razor only evaluates `@_busy`; the rest is literal text, so the attribute is a non-empty string and the button is always disabled. Fix: `disabled="@(_busy \|\| _amount <= 0m)"` (i.e. `disabled="@(_busy \|\| _amount <= 0m)"`). The `@bind:event="oninput"` change already on the amount field does not address this. Not IPD-specific, but it blocks topping up the wallet that IPD admit/discharge charges draw from. Found 12-Nov ~19:50 IST. **Retest 12-Nov ~22:45 IST (after the dev fix): ✅ button is now enabled and opens the "Create a top-up link?" confirm dialog — see DEF-IPD-11 for what happens next.** |
| **DEF-IPD-11** | 🔴 High | Wallet top-up: **Create link** fails with *"Payments are not configured."* and the UI shows nothing — the dialog just stays open | Clinic Admin → Billing & Usage → Wallet → pick ₹1,000 → **Create payment link** → confirm dialog → **Create link**. Button flips to "Creating…" then back; dialog stays open, no payment link, no error text/toast visible (checked at ~1 s, ~3 s, ~5 s); wallet unchanged (₹172). Browser sees `POST /api/usage-billing/wallet/top-up` → **409** `{"title":"Conflict","status":409,"detail":"Payments are not configured."}` (3 attempts, same). **API console log for the same call (22:46:55 IST):** `Handling CreateWalletTopUpCommand` → `[ERR] HTTP POST /api/usage-billing/wallet/top-up responded 500 in 353 ms` → `[ERR] Unhandled exception. CorrelationId: eb9d8f25-1b37-4178-aaa6-1d119dea0a09` → `System.InvalidOperationException: Payments are not configured. at CreateWalletTopUpHandler.Handle(...) in Features\UsageBilling\Handlers\CreateWalletTopUpHandler.cs:line 38`, called from `UsageBillingController.CreateTop\Up` (`UsageBillingController.cs:line 67`) through ActionLoggingBehavior → ValidationBehavior → LoggingBehavior; pipeline: ExceptionHandlingMiddleware → Serilog request logging → Swagger → ClinicContext (L83) → SubscriptionGuard (L47) → BillingFeatureGuard (L40) → Authorization. | **Three things, status after the 13-Nov dev pass: (2) and (3) fixed; (1) needs you.**

**(1) Config — DONE, ready for retest.** Real Razorpay test keys were added to `appsettings.Development.json` (`KeyId`/`KeySecret` both now real values, not the `"rzp_test_REPLACE_ME"` / `""` placeholders). Verified the whole config chain resolves correctly: all 3 launch profiles set `ASPNETCORE_ENVIRONMENT=Development` (so this file loads), and the only other override point (`appsettings.Local.json`, loaded in `Program.cs`) doesn't exist locally, so nothing stomps the new values. `RazorpayOptions.IsConfigured` should now evaluate `true` — **please click through Create link / Copy link / Check payment in the browser to confirm**, since this can't be verified from here without driving the UI.

**(2) Log/response disagreement — fixed, but only half of it (see below):** `ExceptionHandlingMiddleware.HandleExceptionAsync` logged *every* caught exception as `logger.LogError(exception, "Unhandled exception...")`, regardless of what status it resolved to — so a correctly-working 409 business refusal looked identical to a real crash: `[ERR]` severity, full stack trace. Fixed: it now only logs at Error for an actual 5xx; every mapped 4xx (this 409 included) logs at Warning instead. Covered by two new tests in `ExceptionHandlingMiddlewareTests.cs`. **The separate "Serilog says 500, client got 409" mismatch is a different root cause and was *not* touched**: `Program.cs` registers `app.UseMiddleware<ExceptionHandlingMiddleware>()` *before* `app.UseSerilogRequestLogging()`, i.e. Serilog's request-logging middleware sits *inside* the exception handler. When the handler throws, Serilog's own middleware logs its "responded 500" fallback before the exception reaches the outer `ExceptionHandlingMiddleware`, which then sets the real 409 the client actually receives. The textbook fix is swapping those two `app.Use...` lines so Serilog becomes outer — but that is a middleware pipeline reorder, which this repo's own `CLAUDE.md` explicitly protects ("Do not change the middleware pipeline order — see ADR-005"), so it was deliberately left alone pending your sign-off.

**(3) UI swallows the error — fixed:** `TopUpDialog.razor`'s helper text promised "...the reason is shown here" but nothing populated "here". A failure now closes the confirm dialog and shows the server's message inline, persistently, in that exact spot (as well as the existing toast). |

## Additional observations (12-Nov)

| ID | Note | Status |
|---|---|---|
| OBS-4 | Cancelling twice / cancelling a discharged admission returns **400**, not the documented **409**. | **Not a bug — this report's prediction was wrong.** `Cancel()` deliberately mirrors `Discharge()`'s existing 400-on-wrong-status pattern. Corrected G2/G3 above to expect 400. |
| OBS-5 | Shifting a patient into their *own* current bed returns the message "occupied" — misleading. | **Fixed** — now throws "The patient is already in this bed." Retest via F. |
| OBS-6 | `/reports/ipd` page header reads "Dashboard" instead of "IPD Reports". | **Fixed** — added to `AuthorizedLayout`'s title map. Retest via M1. |
| OBS-7 | After a refund is confirmed, `refundDue` / `balanceDue` do not zero in the response; the status label is the only indicator. The Discharge Summary shows Balance ₹-1,200 (negative) for a refunded admission. | **Fixed in the Discharge Summary report only** — now shows ₹0 once `Settled`. The raw `IpdPayment.BalanceDue`/`RefundDue` fields and the Final Bill print are unchanged by design (locked audit snapshot; the print page's own "Refund due" label already gives it context). Retest via M3. |
| OBS-8 | Discharge Summary shows "—" / "Outstanding" for the two pre-feature admissions (no payment row) — misleading; consider "No bill". | **Fixed** — added `HasBill` to the row; shows "No bill" instead of "Outstanding" when there's no payment record. Retest via M3. |
| OBS-9 | Deposit & Bill panel lacks a labelled breakdown and a discount line; amount/discount inputs keep their value after a successful action. | **Partially fixed** — discount input now resets after a successful apply (matching the deposit input), and a "Discount applied" line now shows in the live panel. A fuller line-item breakdown (initial fee / room charges / particulars listed individually, not just the final total) was not added this round. Retest via H, I. |
| OBS-10 | First `bed-history` row has `transferredByUserId` = the doctor id, not the admitting user. | **Deliberately deferred** — no "admitting staff" id currently exists on `CreateIpdRegistrationCommand`; fixing it properly needs a new field end-to-end, which is bigger than this label-only issue warrants right now. |
| OBS-11 | Revenue "Net" = Deposits + Balance collected − Refunded; confirm this formula is intended (it includes cancelled-admission deposits — DEF-IPD-07). | **Resolved as a side effect of the DEF-IPD-07 fix** — once a cancelled admission's deposit is refunded through the new Final Bill flow, it appears under "Refunded" and nets out correctly instead of sitting only in "Deposits". Retest via G1b + M2. |
| OBS-12 | OBS-1 (admission-number local date) cannot be verified until a post-midnight admission is made. | Unchanged — time-dependent, no action taken. |
| OBS-13 | The **Grid / List** toggle on `/ipd` is not a control — "≡ List" is a plain `<div>` (no click handler) in both views. Remove it or implement it. |
| OBS-14 | The top-bar wallet balance doesn't refresh after an admission/discharge (₹282 stayed until a page reload showed ₹172: −₹10 admit, −₹100 discharge). |

### Not executed in this run (please cover)
- Reception: UI click-through of the View dialog / print pages (API-level reception checks are done).
- H5 (Billing flag off hides the panel), L6 (final-bill print), L7 (bed-less stay).
- Other billing-cycle / mid-stay-charge combinations and a mid-stay shift between rooms with *different* rates.
- Usage-ledger reconciliation on `/billing/usage` (₹10/admission, ₹100/discharge; wallet now ₹382).
- Search/pagination on the IPD list; delete ward/room/bed constraints.

### Reception results (Recep One, `RequireClinicStaff`) — 12-Nov, ~18:30 IST

Exercised on admission `IPD-20261112-0005` (DUMMY Patient 007), mostly through the same API calls the UI makes, plus a UI check of nav and the `/ipd` page.

| Check | Result |
|---|---|
| Nav menu | ✅ Dashboard, Patient Reg., EDD Overdue, OPD Queue, IPD & Beds, Counter, Expenses, Handovers, IPD Reports. **No** Wards & Rooms / User Management / Settings (B9). |
| Create ward (`POST /wards`) | ✅ **403** — admin-only as intended |
| Bed shift (F8) | ✅ 200 |
| Collect deposit (H1) | ✅ 201 |
| Discount (I3) | ✅ 200 (field `discount`; a `PUT` to the same route is 404) |
| Add particular (J1) | ✅ 201 |
| Provisional bill (K1) | ✅ 200 (room charges ₹300 for the day after shift) |
| Discharge (F8/L) | ✅ 200; bed freed — reloaded `/ipd` shows 0 occupied, 18 available; wallet ₹382 → ₹282 (₹100 discharge charge) |
| Final bill (L3) | ✅ 200 — final ₹450, deposit ₹1,000 → refund due ₹550 |
| Confirm refund (L4) | ✅ 200, but `balanceDue` stays −550 and `refundDue` stays 550 afterwards (OBS-7 confirmed) |
| Deposit after discharge (H4) | ✅ 409 |
| Reports (M1/M2/M3) | ✅ census, revenue and discharge-summary all 200 |

Not repeated under reception: the UI **View** dialog (DEF-IPD-05/06 are role-independent, caused by shared code), H5, L6, L7, F1.

#### Reception UI pass (Recep One, Chrome, 12-Nov ~18:50 IST)

| Step | Result |
|---|---|
| Open `/ipd` | ✅ Loads the ward grid (Wards › Rooms › Bed map); stats and ward occupancy correct |
| Admit via bed → **+ Admit** (new patient `DUMMY Patient 008`, `IPD-20261112-0006`) | ⚠ Works, but "Attending Doctor (optional)" is **required** — saving without one shows the raw message "'Doctor User Id' must not be empty. 'Doctor Name' must not be empty." (**DEF-IPD-09**). With Doctor One selected: admitted, bed turns occupied. |
| Bed Details dialog (click occupied bed) | ⚠ Shows patient, doctor, days, initial fee, notes and **only** a *Discharge Patient* button |
| Shift bed, deposit, discount, particulars, provisional bill, final bill, collect balance / refund, print | ❌ **Not available anywhere in the reception UI** (**DEF-IPD-08**) |
| Discharge via UI | ✅ "Patient discharged." toast; bed freed (0 occupied, 18 available) |
| IPD Reports (Census / Revenue / Discharge Summary) | ✅ All three work for reception. Revenue 14-Oct→12-Nov: Deposits ₹6,700, Balance ₹1,400, Refunded ₹1,750, Net ₹6,350. Discharge Summary lists the new admission with ₹600 final, ₹600 balance, **Outstanding** |

Consequence: a reception user can admit and discharge but **cannot collect a deposit, apply a discount, add service items, shift a bed, see the bill, or collect the balance in the UI**. The ₹600 for `IPD-20261112-0006` now sits as "Outstanding" with no screen where reception can settle it. The only way reception can do any of it today is by calling the API directly.


---

## Defects (from 10-Nov) — fix status

| ID | Severity | Title | Original finding | Fix applied | Retest |
|---|---|---|---|---|---|
| **DEF-IPD-01** | 🔴 High | Bed shift does not move occupancy; "ghost" occupied bed | Shifting via the generic edit endpoint never touched the Bed entities; discharge only freed whichever bed the (stale) free-text fields pointed to | New `TransferBedCommand`: resolves the real target `Bed`, frees the old one and occupies the new one in one transaction; discharge now frees via the occupancy record, not re-parsed text. The exact QA-B1 ghost bed from this run was also released by the fix's data migration (verified against the DB: 0 occupied beds with nothing pointing at them). | ✅ Verified 12-Nov (F3, F6) |
| **DEF-IPD-02** | 🔴 High | Shift into an occupied bed is accepted | PUT into an occupied bed returned 200 | `TransferBedCommand` checks `IsOccupied` before allowing a shift, same as admission does | ✅ Verified 12-Nov (F4) |
| **DEF-IPD-03** | 🔴 High | Shift to non-existent ward/room/bed is accepted | Free-text fields meant any string was accepted | The field is now a real `BedId`; a non-existent one returns 404. Existing admissions' free-text data was also backfilled to real bed ids where it matched. | ✅ Verified 12-Nov (F5) |
| **DEF-IPD-04** | 🟡 Medium | PUT accepts any status; "Transferred" blocks discharge | `status=3` could be set via PUT with no way back except another PUT | Status no longer settable via open PUT at all; it only moves through Discharge/Transfer/Cancel actions, none of which can produce a stuck state. Any pre-existing `Status=3` row was reset to `Admitted` by the fix's migration. | ✅ Verified 12-Nov (F7) |

---

## Gaps from 10-Nov — status

| Requested flow | 10-Nov finding | Now |
|---|---|---|
| Deposit / advance collection | Did not exist | **Built — see Section H** |
| IPD billing (room charges, particulars, final bill, payment, refund) | Did not exist | **Built — see Sections I, J, K, L.** Room charges follow two new clinic-level settings (billing cycle, mid-stay charge mode) — confirm you're happy with the behavior under both settings' combinations (see the plan's calculator logic if you need the exact day-by-day math) |
| IPD reports | None | **Built — see Section M** |
| Bed shifting UI | API existed, no button | **Built — see Section F** |
| Reception end-to-end | Reception likely couldn't discharge | **Confirmed fixed — discharge, shift, deposit, discount and billing actions are all now `RequireClinicStaff`, which includes reception** |

### Still open / deliberately out of scope
- **Cancel's financial scope** — `CancelIpdRegistrationCommand` only frees the bed; it does **not** reverse the admission's usage-wallet charge or any deposit already collected (Section G1). This was a deliberate scope decision, deferred for a later decision — flag if a cancelled admission needs money reversed too.
- **No controller-level or Web.Client-level automated tests** exist yet for the 4 new API controllers (`IpdDeposits`, `IpdParticulars`, `IpdPayments`, `IpdReports`) or the new typed client methods — everything underneath (handlers, validators, the room-charge calculator) has full unit coverage, but the thin controller/client layers don't. Not a QA gap, but worth knowing if something odd shows up only at the HTTP boundary.

---

## Observations

| ID | Note | Status |
|---|---|---|
| OBS-1 | Admission number and admission date used UTC date, showing the wrong calendar day for a night admission | **Fixed** — the number's date prefix now resolves to local calendar day. The underlying `AdmissionDate` timestamp itself deliberately stays UTC (it now feeds the room-charge calculator) — only the number's display logic changed. Retest with a post-midnight admission if you want to confirm. |
| OBS-2 | Clinic wallet charge (₹10 admit / ₹100 discharge) is separate from patient money, which wasn't tracked at all for IPD | **Superseded** — patient money is now tracked via deposits/Provisional/Final Bill (Sections H–L). The platform wallet charge is still a separate thing by design (metered platform usage vs. clinic's own patient billing) — don't expect the two to reconcile against each other. |
| OBS-3 | Dummy room types showed `"General"`, not a real dropdown option | **Fixed** — relabeled to `"General (4-bed)"` in seed data. Only affects newly-seeded dummy data; doesn't touch any clinic's real rooms. |

---

## Test data left behind (for clean-up)

- Ward **QA Test Ward** → Room **QA-101** (Semi-Private ₹800) → beds **QA-B1**, **QA-B2**, **QA-B3**. QA-B1 is **no longer stuck occupied** — released by the fix's migration.
- Admissions `IPD-20261109-0001` (DUMMY Patient 001) and `IPD-20261109-0002` (DUMMY Patient 002), both Discharged. Neither has an `IpdPayment`/deposit/particular row — they predate this feature, so nothing was backfilled for their billing (only occupancy was backfilled).
- Clinic wallet reduced by ₹220 (₹952 → ₹732) — unchanged by this update.
- **New test data you'll create:** every deposit/discount/particular/settlement in Sections H–L will leave real rows behind (`IpdDeposits`, `IpdParticulars`, `IpdPayments`). None of it auto-cleans — note what you create if it needs removing afterward.

## Next steps

0b. **DEF-IPD-10 is fixed and verified. DEF-IPD-11 is now fully addressed on the dev side:** the UI no
    longer swallows the error (dialog closes, reason shown inline), the API no longer logs an expected
    409 as an `[ERR]` "Unhandled exception" (now a Warning), and real Razorpay test keys are now in
    local config — **retest all three (Create link / Copy link / Check payment) in the browser**, since
    the config fix can't be verified from here without driving the UI. **Flagged, not changed:** Serilog's request-logging middleware sits *inside*
    `ExceptionHandlingMiddleware` in `Program.cs` (registered after it), which is why it still logs its
    own "responded 500" line even though the client correctly gets 409 — fixing that means swapping
    two middleware registration lines, which needs your explicit go-ahead since `CLAUDE.md` protects
    pipeline order changes. **DEF-IPD-08 and DEF-IPD-09 are now both fixed (13-Nov)** — reception's
    bed click opens the same rich dialog admin uses (with a new Discharge button added to it), and
    both admit dialogs now require a doctor with a friendly toast instead of a raw server error.
    Re-run the full reception UI pass next to confirm.

0. **Done 12-Nov (same day):** DEF-IPD-05/06/07 and OBS-5/6/7/8/9 all have a code fix (build green, full
   suite — now 1789 tests — passing). **None of it has been re-walked through the UI yet** — that's the
   next QA pass.
1. Re-verify K1–K3, L1–L6 **in the browser** (DEF-IPD-05/06 were both UI-only bugs the earlier API-level
   checks couldn't catch) — this is the highest-priority retest.
2. Run G1b (cancel-with-deposit) end-to-end through the UI, including confirming the refund, then check
   the Revenue report nets it correctly (M2).
3. Spot-check OBS-5/6/7/8/9's fixes (F — same-bed shift message, M1 — reports header, M3 — Discharge
   Summary wording, H/I — input reset and discount line).
4. Decide on the two still-open items under "Still open / deliberately out of scope" above (Cancel's
   usage-wallet reversal scope, and OBS-10's deferred admitting-user id) — neither was touched this round.


### Test data created 12-Nov
Admissions `IPD-20261112-0001`…`0003` (DUMMY Patient 003/004/005) discharged and settled; a cancelled admission (P006) with a ₹700 deposit; several deposits, a ₹300 discount and particulars. Wallet ₹382. No clean-up performed.

Additional test data (reception run): admission `IPD-20261112-0005` (DUMMY Patient 007) shifted, ₹1,000 deposit, ₹50 discount, one particular, discharged, refund confirmed. Wallet now ₹282.

Reception UI run also left: admission `IPD-20261112-0006` (DUMMY Patient 008, discharged, ₹600 outstanding). Wallet ₹172 (ledger reconciles: 8 admissions × ₹10, 7 discharges × ₹100).
