# OPD Reception — Registration, Cancel, Refund, Delete & Wallet — Test & Defect Report

| Item | Detail |
|---|---|
| Repo | TenantCore.App |
| Testing branch | `testing/opd-reception-refund-wallet` (from `main` @ `59f9c7c`) |
| Module | OPD (Reception) · Counter Session · OPD Payments/Refund · Usage Wallet |
| Environment | Local — `https://localhost:7246` (App running from `feature/clinic-trial-razorpay-subscriptions`, merged to `main` via PR #29) |
| Clinic | Clinic One Care (`2d76bdc2-4843-4d11-9120-1f81aa32ec86`) |
| Accounts | Receptionist `awez1411+clinic1_recep1@gmail.com` · Doctor/Clinic Admin `awez1411+clinic1admin@gmail.com` |
| Tested on | 02-Nov-2026, 01:15–01:40 IST |
| Tested by | QA (Claude) |
| Linked Auth report | TenantCore.Auth → `docs/testing/OPD_Reception_Auth_Defects.md` (branch `testing/opd-reception-auth-access`) |

## Summary

| Metric | Count |
|---|---|
| Test cases executed | 34 |
| ✅ Pass | 18 |
| ❌ Fail | 15 |
| ⛔ Blocked | 1 |
| Defects raised (App) | 18 (2 Critical · 4 High · 7 Medium · 5 Low) |
| Defects raised (Auth, separate report) | 2 (1 Critical · 1 High) |

**Bottom line:** the backend money flow works for the normal path — registering an OPD visit charges the wallet ₹1, a full refund on a cancelled visit returns the cash to the patient (session total drops) **and** restores the ₹1 to the wallet, and delete does not credit twice. But a receptionist **cannot complete the flow from the UI today**: the doctor list and the handover recipient list both fail with 403 (Auth regression), so reception cannot register a visit with a doctor or close the counter session. There are also money bugs around partial refunds and ₹0 visits.

> Because the UI was blocked by DEF-OPD-01, registrations for the flow tests were created through the same API the UI calls (`POST /api/opd-registrations`) using the receptionist's own token. Cancel, refund and delete were then done **through the UI** wherever the UI allowed it.

### Wallet movement observed (OPD usage rate = ₹1.00)

| Step | Wallet | Ledger row | Counter session collected |
|---|---|---|---|
| Baseline | ₹1,000.00 | Admin credit +₹1,000 | — (no session) |
| Register OPD (fee ₹300) | ₹999.00 | Usage — OpdRegistration −₹1 | ₹300 |
| Cancel visit | ₹999.00 | — | ₹300 (refund due) |
| Refund Full Amount ₹300 | ₹1,000.00 | Refund — OpdRegistration +₹1 | ₹0 |
| Delete visit | ₹1,000.00 | — (no double credit ✅) | ₹0 |
| Repeat with real doctor (fee ₹400): register → cancel → refund → delete | 997 → 996 → 996 → 997 → 997 | −₹1 then +₹1 | 950 → 950 → 550 → 550 |

## Defect status workflow

| Status | Meaning | Owner / next action |
|---|---|---|
| 🔴 Open | Not yet fixed | **Dev** – resolve |
| 🟡 Fixed – Pending QA | Dev says fixed, awaiting re-test | **QA** – re-verify |
| 🟢 Verified – Closed | QA confirmed the fix | None (closed) |
| ⚪ Deferred / Won't Fix | Acknowledged, not fixing now | Product decision |

> Every defect below is **🔴 Open**. When a developer fixes one on this branch, change its Status to **🟡 Fixed – Pending QA** and add the commit id in the "Fix commit" column; QA re-tests and moves it to **🟢 Verified – Closed**.

## Test cases

| TC ID | Type | Scenario | Steps | Expected | Actual | Result | Defect |
|---|---|---|---|---|---|---|---|
| TC-OPD-01 | UI | Wallet baseline & rates | Recep → Wallet page | Balance + metered rates shown | ₹1,000.00; OPD registration ₹1.00, IPD admission ₹10, IPD discharge ₹100, OT ₹100 | Pass | |
| TC-OPD-02 | UI | Register with no patient selected | New OPD Entry → Register | Inline "select a patient" error | Nothing happens, no message, no request | Fail | DEF-OPD-08 |
| TC-OPD-03 | UI/API | Doctor dropdown for Reception | Open New OPD Entry | Clinic doctors listed | Only "— No Doctor —"; `GET /api/clinic-settings/doctors` → 403 | Fail | DEF-OPD-01 |
| TC-OPD-04 | UI/API | Register with "No Doctor" (field says optional) | Select patient, fee 300, Register | Visit created (or doctor marked required) | `POST /api/opd-registrations` → 400 "Doctor User Id / Doctor Name must not be empty"; UI shows nothing | Fail | DEF-OPD-07 |
| TC-OPD-05 | API | Register with no counter session | POST registration, no session open | 409 "open a counter session" | 409 "No active counter session…" | Pass | |
| TC-OPD-06 | UI | Open counter session | Counter → Open Session | Session open, totals 0 | Session open, Rs. 0 | Pass | |
| TC-OPD-07 | API | Open a second session | `POST /api/counter-sessions/open` again | 409 | 409 "A counter session is already open." | Pass | |
| TC-OPD-08 | API | Register OPD with real doctor | POST with doctor `e2df01de…`, fee 400 | 201; prepaid fee collected | 201 OPD-20261102-0006; payment Received, collected ₹400 | Pass | |
| TC-OPD-09 | API | Wallet charged on registration | Compare wallet before/after | −₹1 + "Usage — OpdRegistration" ledger row | 1000→999 and 997→996; ledger row present | Pass | |
| TC-OPD-10 | API | Session total includes fee | GET active session | +fee | +₹300 / +₹400 | Pass | |
| TC-OPD-11 | API | Delete an active (not cancelled) visit | DELETE registration | 409 | 409 "Only a cancelled OPD registration can be deleted." | Pass | |
| TC-OPD-12 | UI | Cancel from Visit Details (⋯) modal | ⋯ → Cancel Appointment | Visit cancelled | App crashes: "Something went wrong" — NullReferenceException in `CancelAsync` | Fail | DEF-OPD-03 |
| TC-OPD-13 | UI | Cancel from row button | Row → Cancel | Confirm, then cancelled | Cancelled immediately (no confirmation); row shows "Paid · Refund due" + Refund | Pass | DEF-OPD-15 |
| TC-OPD-14 | UI | Full refund on cancelled visit | Refund → Refund Full Amount | Refunded; cash out of session; wallet +₹1 | "Refunded"; session −₹300/−₹400; wallet +₹1 with "Refund — OpdRegistration" row | Pass | |
| TC-OPD-15 | UI | Header wallet badge after refund | Look at header after refund | Shows new balance | Still old value (₹999 while actual ₹1,000; ₹996 while actual ₹997) until reload | Fail | DEF-OPD-14 |
| TC-OPD-16 | UI | Delete refunded visit | Row → Delete → confirm dialog → Delete | Confirm dialog, record removed, wallet unchanged | Confirm dialog shown; GET → 404; wallet unchanged (no double credit) | Pass | |
| TC-OPD-17 | API | Double refund | Refund again after Refunded | 409 | 409 "No refund is pending for this payment." | Pass | |
| TC-OPD-18 | API | Duplicate visit same patient/doctor/day | Register twice | 409 | 409 "…already has an OPD appointment with … today." | Pass | |
| TC-OPD-19 | API | Negative fee | fee −50 | 400 | 400 "'Fee' must be greater than or equal to '0'." | Pass | |
| TC-OPD-20 | API | Doctor not in clinic | doctorUserId random GUID, doctorName "Dr Fake" | 400/404 | 201 — visit created, wallet charged, no doctor can see it | Fail | DEF-OPD-06 |
| TC-OPD-21 | API | Absurd fee | fee 99,999,999,999 | 400 (upper limit) | 201; "collected" into session (session showed ₹1,00,00,00,00,549) | Fail | DEF-OPD-10 |
| TC-OPD-22 | API | Out-of-range vitals | O2 250, Temp 500, Pulse 900, Weight 9999, BP "abc" | 400 | 201, all stored | Fail | DEF-OPD-11 |
| TC-OPD-23 | UI | Vitals not entered | Open form, don't touch vitals, Register | Vitals saved as empty | Pulse 72 and O2 98 pre-filled and sent in the request | Fail | DEF-OPD-12 |
| TC-OPD-24 | API | Partial refund on an active visit | Discount ₹50 on collected ₹200 (visit Waiting) → Confirm refund | ₹50 back to patient; OPD usage charge stays (visit still happens) | ₹50 refunded **and** wallet +₹1 (997→998) | Fail | DEF-OPD-04 |
| TC-OPD-25 | API/UI | Cancel + full refund after a partial refund | Same visit → Cancel → Refund full remaining ₹150 | ₹150 refunded, visit deletable | Refund → 409 "This action conflicts with existing data."; stuck in PendingRefund; UI shows Delete but no Refund | Fail | DEF-OPD-05 |
| TC-OPD-26 | API/UI | Cancel + delete a ₹0 visit | Fee 0 → Cancel → Delete | Deleted; ₹1 returned | Delete → 409 "Refund the collected amount…"; refund → 409 "No refund is pending"; UI shows "Paid · Refund due" | Fail | DEF-OPD-09 |
| TC-OPD-27 | API | Token not reused | Delete the day's latest visit, register a new one | New token number | Same token re-issued twice (OPD-20261102-0001, OPD-20261102-0006) | Fail | DEF-OPD-13 |
| TC-OPD-28 | API | Refund endpoint path id | `POST /api/opd-payments/{bogus-id}/refund` with valid OPD id in body | 404 / 400 | 200 — refund processed, path id ignored | Fail | DEF-OPD-16 |
| TC-OPD-29 | UI | Close counter session | Counter → Close Session | Closes (after handover) | Button disabled with no reason; handover dialog says "No admin or doctor users found." (`GET /api/Application/{appId}/users` → 403) | Blocked | DEF-OPD-02, DEF-OPD-18 |
| TC-OPD-30 | UI | Doctor sees only own queue | Login as doctor → My OPD Queue | Only own visits | 0 rows while 5 "Dr Fake" visits existed | Pass | |
| TC-OPD-31 | API | Doctor list for Doctor/Admin | Admin/doctor → GET doctors | 200 | 200, 1 doctor (profileId empty) | Pass | |
| TC-OPD-32 | API | Unknown API route | `GET /api/doctors` | 404 JSON | 200 + SPA HTML page | Fail | DEF-OPD-17 |
| TC-OPD-33 | UI | Register patient (prerequisite) | Patient Reg. 3-step form | Created, feedback while saving | 201 after ~25 s; no spinner/disabled button during wait | Pass | Observation |
| TC-OPD-34 | API | Payment after delete | GET by-opd after delete | 404 | 204 No Content | Pass | Note |

## Defects

| Defect ID | Severity | Status | Title | Steps to Reproduce | Expected | Actual | Root cause / where to fix | Owner / Next Action | Fix commit |
|---|---|---|---|---|---|---|---|---|---|
| DEF-OPD-01 | Critical | 🔴 Open | Reception cannot see any doctor — OPD can't be registered from the UI | Login as Receptionist → OPD → New OPD Entry → open "Select Doctor" | Clinic doctors listed | Only "— No Doctor —". `GET /api/clinic-settings/doctors` → 403 | `GetClinicDoctorsHandler` calls Auth `GET api/Application/{appId}/users` with the caller's token; Auth now allows that only for Admin/Clinic Admin (see DEF-AUTH-OPD-01). Fix together with Auth: call a reception-safe "clinic staff directory" (doctors/admins only, minimal fields) or a service-to-service call. | Dev – resolve (App + Auth) | |
| DEF-OPD-02 | Critical | 🔴 Open | Reception cannot close the counter session | Recep → Handovers → + New Handover | Doctor/Admin recipients listed; handover → accept → Close Session | "No admin or doctor users found." (`GET /api/Application/{appId}/users` → 403). Close Session stays disabled forever | Same root cause as DEF-OPD-01 (handover recipient list uses the admin-only roster) | Dev – resolve (App + Auth) | |
| DEF-OPD-03 | High | 🔴 Open | "Cancel Appointment" in Visit Details modal crashes the page | OPD row → ⋯ → Cancel Appointment | Visit cancelled | "Something went wrong" error page; console: `NullReferenceException at OpdRegistrationList.CancelAsync` (razor line 991, called from line 468) | `OpdRegistrationList.razor` line 468: `CloseViewModal(); await CancelAsync(_selected);` — `CloseViewModal()` clears `_selected` first. Capture `var reg = _selected;` before closing. | Dev – resolve | |
| DEF-OPD-04 | High | 🔴 Open | Partial refund on a visit that still happens gives the ₹1 OPD usage charge back | Register (fee 200, collected) → Discount ₹50 → Confirm Refund, visit stays Waiting | Patient gets ₹50; platform usage charge kept | Wallet +₹1 (997→998) "Refund — OpdRegistration" | `ProcessOpdRefundHandler.ReverseUsageChargeAsync` runs on **every** refund. Reverse only when the visit is Cancelled (full refund). | Dev – resolve | |
| DEF-OPD-05 | High | 🔴 Open | After a partial refund, a cancelled visit can never be refunded or deleted | Continue DEF-OPD-04: Cancel visit → Refund full remaining ₹150 | ₹150 refunded; Delete allowed | Refund → 409 "This action conflicts with existing data." Payment stuck in PendingRefund (₹150 owed). UI row shows Delete (fails) and no Refund button | Second `ReverseUsageChargeAsync` inserts another wallet row for the same usage record → unique index `(ApplicationId, ReferenceType, ReferenceId)` → SQL 2601 → 409, whole refund rolled back. Also `NeedsRefund()` in the razor uses `FinalAmount` (0 after discount) so it hides Refund. Fix with DEF-OPD-04 (reverse once, only on cancel; make reversal idempotent). | Dev – resolve | |
| DEF-OPD-06 | High | 🔴 Open | OPD accepts any doctor id / name | `POST /api/opd-registrations` with random `doctorUserId` and `doctorName:"Dr Fake"` | 400/404 "doctor not in this clinic" | 201; wallet charged; visit invisible to every doctor (orphan) | `CreateOpdRegistrationHandler` never checks the doctor belongs to the clinic, and trusts client-sent `DoctorName`. Validate against the clinic's doctors and take the name server-side. | Dev – resolve | |
| DEF-OPD-07 | Medium | 🔴 Open | Doctor labelled "optional" but required by API; failure is silent | New OPD Entry → patient + fee → leave "— No Doctor —" → Register | Either works, or field marked required with message | 400 validation error; modal stays open, no message | UI label vs `CreateOpdRegistrationCommandValidator`; UI ignores the 400 body | Dev – resolve | |
| DEF-OPD-08 | Medium | 🔴 Open | Register with no patient does nothing | New OPD Entry → Register | "Please select a patient" | No message, no request | Client-side validation missing | Dev – resolve | |
| DEF-OPD-09 | Medium | 🔴 Open | Cancelled ₹0 visit can't be deleted and keeps its ₹1 charge | Register fee 0 → Cancel → Delete | Deleted; ₹1 usage returned | Delete → 409 "Refund the collected amount before deleting"; refund → 409 "No refund is pending"; row shows "Paid · Refund due" | `DeleteOpdRegistrationHandler` blocks any Received payment not Refunded even when `CollectedAmount == 0`; usage is only reversed in the refund path. Allow delete when nothing was collected and reverse the usage charge on cancel/delete of an unpaid visit. | Dev – resolve | |
| DEF-OPD-10 | Medium | 🔴 Open | No upper limit on OPD fee | POST fee 99,999,999,999 | 400 (sane max) | 201; auto-collected; session total ₹1,00,00,00,00,549 | Add max to Create/Update validators and UI | Dev – resolve | |
| DEF-OPD-11 | Medium | 🔴 Open | Vitals not range-validated | POST O2 250, Temp 500, Pulse 900, Weight 9999, BP "abc" | 400 | 201, stored | Validators only check `>= 0`. Add clinical ranges and BP format `^\d{2,3}/\d{2,3}$` | Dev – resolve | |
| DEF-OPD-12 | Medium | 🔴 Open | Pulse 72 / O2 98 pre-filled and saved though nobody measured them | Open New OPD Entry, don't touch vitals, Register | Vitals empty | Request body carries `pulseRate:72, oxygenSaturation:98` | Default values in the OPD form; should be empty (Quick pick is enough) | Dev – resolve | |
| DEF-OPD-13 | Medium | 🔴 Open | OPD token number reused after delete | Delete the day's latest visit → register another | New number | Same token issued again (seen for 0001 and 0006) | `GetNextRegistrationNumberAsync` = highest existing + 1 on hard-deleted rows. Use a per-day sequence table or soft delete. Concurrent bookings can also hit the unique index → generic 409. | Dev – resolve | |
| DEF-OPD-14 | Low | 🔴 Open | Header wallet badge stale after refund | Refund a visit, look at header | Updated balance | Old balance until page reload (reproduced twice) | Refresh the wallet badge after refund/register | Dev – resolve | |
| DEF-OPD-15 | Low | 🔴 Open | Cancel appointment has no confirmation | Row → Cancel | Confirm dialog | Cancels immediately | Use `ConfirmDialog` (ADR-007 destructive-action rule) | Dev – resolve | |
| DEF-OPD-16 | Low | 🔴 Open | Refund endpoint ignores `{id}` in URL | `POST /api/opd-payments/<random>/refund` with valid `opdRegistrationId` | 404 / 400 mismatch | 200, refund done | Use the path id or check it matches the body | Dev – resolve | |
| DEF-OPD-17 | Low | 🔴 Open | Unknown `/api/*` routes return 200 HTML | `GET /api/doctors` | 404 JSON | 200 + `index.html` | Exclude `/api` from the SPA fallback | Dev – resolve | |
| DEF-OPD-18 | Low | 🔴 Open | Close Session disabled with no explanation | Counter page with open session, no handover | Hint "hand over cash first" | Greyed-out button only | Show the reason next to the button | Dev – resolve | |

## Observations (not raised as defects yet)

| # | Observation |
|---|---|
| OBS-1 | `POST /api/patients` took ~25 s locally with no loading state on "Register Patient" (risk of double-submit). Wallet page also took ~60 s to render once. Worth profiling. |
| OBS-2 | Receptionist sees the "Top up the wallet / Create payment link" panel. Server requires Clinic Admin for top-up (per code, not exercised here) — consider hiding it for Reception. |
| OBS-3 | Page header shows "Dashboard" on the Wallet and Counter pages. |
| OBS-4 | Doctor "Clinic1 Admin" has no doctor profile (`profileId` empty), so doctor fee config can't apply. |

## Test data left in Clinic One Care

| Token | Patient | State | Why kept |
|---|---|---|---|
| OPD-20261102-0001 | DUMMY Patient 001 | Cancelled, PendingRefund ₹150 (stuck) | Evidence for DEF-OPD-05 |
| OPD-20261102-0002 | DUMMY Patient 003 | Waiting, ₹100 collected | Absurd-vitals record (DEF-OPD-11) |
| OPD-20261102-0003 | DUMMY Patient 004 | Cancelled, Refunded (₹99,999,999,999 fee) | Evidence for DEF-OPD-10 |
| OPD-20261102-0004 | DUMMY Patient 005 | Cancelled ₹0, cannot delete | Evidence for DEF-OPD-09 |
| OPD-20261102-0005 | DUMMY Patient 007 | Cancelled, ₹300 refund due | Session-boundary test (blocked by DEF-OPD-02) |
| OPD-20261102-0006 | DUMMY Patient 010 | Cancelled, Refunded ₹250 | DEF-OPD-16 evidence |
| — | QA OpdPatient One | Patient record | Created for testing |
| Counter session | opened 02-Nov 01:27 | Still **open** (can't close — DEF-OPD-02) | |

Wallet at end of testing: **₹997.00** = ₹1,000 − ₹3. Visits 0002 (still active), 0004 (₹0, stuck — DEF-OPD-09) and 0005 (refund not done yet) keep their ₹1 charge. Visit 0001 already got its ₹1 back through the partial refund (DEF-OPD-04). All other test visits were charged ₹1 and given it back on refund.

## Re-test checklist for QA (after fixes)

1. Recep → New OPD Entry lists doctors; register with a doctor from the UI (DEF-OPD-01).
2. Handover to doctor → doctor accepts → Recep closes session (DEF-OPD-02).
3. ⋯ → Cancel Appointment works (DEF-OPD-03).
4. Partial refund leaves the wallet unchanged; later cancel + full refund returns ₹1 exactly once (DEF-OPD-04/05).
5. ₹0 visit: cancel → delete works and wallet gets ₹1 back (DEF-OPD-09).
6. Re-run TC-OPD-20 to TC-OPD-23, TC-OPD-27, TC-OPD-28, TC-OPD-32.
