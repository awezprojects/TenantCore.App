# IPD — 16-Nov-2026 full UI retest: results and NEW DEFECTS for developers

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


---
## Evening re-retest (UI)
DEF-IPD-28 PASS (closed). DEF-IPD-29 PASS (closed). DEF-IPD-30 STILL OPEN: `GET /api/Application/{app}/users/deactivated` returns 409 "Object reference not set to an instance of an object." after deactivating a member; Deactivated tab shows 0. NEW DEF-IPD-31 (Medium): role lost (roleId null) after deactivate → reactivate; restored with Change Role → Receptionist.
