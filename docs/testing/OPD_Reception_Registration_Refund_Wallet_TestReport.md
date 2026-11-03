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
| Re-tested | 04-Nov-2026 on `fix/def-opd-01-clinic-staff-directory` + Auth `testing/opd-reception-auth-access` — see **Re-test — 04-Nov-2026** below |

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
| DEF-OPD-01 | Critical | 🟢 Fixed | Reception cannot see any doctor — OPD can't be registered from the UI | Login as Receptionist → OPD → New OPD Entry → open "Select Doctor" | Clinic doctors listed | Only "— No Doctor —". `GET /api/clinic-settings/doctors` → 403 | `GetClinicDoctorsHandler` calls Auth `GET api/Application/{appId}/users` with the caller's token; Auth now allows that only for Admin/Clinic Admin (see DEF-AUTH-OPD-01). Fix together with Auth: call a reception-safe "clinic staff directory" (doctors/admins only, minimal fields) or a service-to-service call. | Dev – resolve (App + Auth) | |
| DEF-OPD-02 | Critical | 🟢 Fixed | Reception cannot close the counter session | Recep → Handovers → + New Handover | Doctor/Admin recipients listed; handover → accept → Close Session | "No admin or doctor users found." (`GET /api/Application/{appId}/users` → 403). Close Session stays disabled forever | Same root cause as DEF-OPD-01 (handover recipient list uses the admin-only roster) | Dev – resolve (App + Auth) | |
| DEF-OPD-03 | High | 🔴 Still Open | "Cancel Appointment" in Visit Details modal crashes the page | OPD row → ⋯ → Cancel Appointment | Visit cancelled | "Something went wrong" error page; console: `NullReferenceException at OpdRegistrationList.CancelAsync` (razor line 991, called from line 468) | `OpdRegistrationList.razor` line 468: `CloseViewModal(); await CancelAsync(_selected);` — `CloseViewModal()` clears `_selected` first. Capture `var reg = _selected;` before closing. | Dev – resolve | |
| DEF-OPD-04 | High | 🔴 Still Open | Partial refund on a visit that still happens gives the ₹1 OPD usage charge back | Register (fee 200, collected) → Discount ₹50 → Confirm Refund, visit stays Waiting | Patient gets ₹50; platform usage charge kept | Wallet +₹1 (997→998) "Refund — OpdRegistration" | `ProcessOpdRefundHandler.ReverseUsageChargeAsync` runs on **every** refund. Reverse only when the visit is Cancelled (full refund). | Dev – resolve | |
| DEF-OPD-05 | High | 🔴 Still Open | After a partial refund, a cancelled visit can never be refunded or deleted | Continue DEF-OPD-04: Cancel visit → Refund full remaining ₹150 | ₹150 refunded; Delete allowed | Refund → 409 "This action conflicts with existing data." Payment stuck in PendingRefund (₹150 owed). UI row shows Delete (fails) and no Refund button | Second `ReverseUsageChargeAsync` inserts another wallet row for the same usage record → unique index `(ApplicationId, ReferenceType, ReferenceId)` → SQL 2601 → 409, whole refund rolled back. Also `NeedsRefund()` in the razor uses `FinalAmount` (0 after discount) so it hides Refund. Fix with DEF-OPD-04 (reverse once, only on cancel; make reversal idempotent). | Dev – resolve | |
| DEF-OPD-06 | High | 🔴 Still Open | OPD accepts any doctor id / name | `POST /api/opd-registrations` with random `doctorUserId` and `doctorName:"Dr Fake"` | 400/404 "doctor not in this clinic" | 201; wallet charged; visit invisible to every doctor (orphan) | `CreateOpdRegistrationHandler` never checks the doctor belongs to the clinic, and trusts client-sent `DoctorName`. Validate against the clinic's doctors and take the name server-side. | Dev – resolve | |
| DEF-OPD-07 | Medium | 🔴 Still Open | Doctor labelled "optional" but required by API; failure is silent | New OPD Entry → patient + fee → leave "— No Doctor —" → Register | Either works, or field marked required with message | 400 validation error; modal stays open, no message | UI label vs `CreateOpdRegistrationCommandValidator`; UI ignores the 400 body | Dev – resolve | |
| DEF-OPD-08 | Medium | 🟢 Fixed | Register with no patient does nothing | New OPD Entry → Register | "Please select a patient" | No message, no request | Client-side validation missing | Dev – resolve | |
| DEF-OPD-09 | Medium | 🔴 Still Open | Cancelled ₹0 visit can't be deleted and keeps its ₹1 charge | Register fee 0 → Cancel → Delete | Deleted; ₹1 usage returned | Delete → 409 "Refund the collected amount before deleting"; refund → 409 "No refund is pending"; row shows "Paid · Refund due" | `DeleteOpdRegistrationHandler` blocks any Received payment not Refunded even when `CollectedAmount == 0`; usage is only reversed in the refund path. Allow delete when nothing was collected and reverse the usage charge on cancel/delete of an unpaid visit. | Dev – resolve | |
| DEF-OPD-10 | Medium | 🔴 Still Open | No upper limit on OPD fee | POST fee 99,999,999,999 | 400 (sane max) | 201; auto-collected; session total ₹1,00,00,00,00,549 | Add max to Create/Update validators and UI | Dev – resolve | |
| DEF-OPD-11 | Medium | 🔴 Still Open | Vitals not range-validated | POST O2 250, Temp 500, Pulse 900, Weight 9999, BP "abc" | 400 | 201, stored | Validators only check `>= 0`. Add clinical ranges and BP format `^\d{2,3}/\d{2,3}$` | Dev – resolve | |
| DEF-OPD-12 | Medium | 🔴 Still Open | Pulse 72 / O2 98 pre-filled and saved though nobody measured them | Open New OPD Entry, don't touch vitals, Register | Vitals empty | Request body carries `pulseRate:72, oxygenSaturation:98` | Default values in the OPD form; should be empty (Quick pick is enough) | Dev – resolve | |
| DEF-OPD-13 | Medium | 🔴 Still Open | OPD token number reused after delete | Delete the day's latest visit → register another | New number | Same token issued again (seen for 0001 and 0006) | `GetNextRegistrationNumberAsync` = highest existing + 1 on hard-deleted rows. Use a per-day sequence table or soft delete. Concurrent bookings can also hit the unique index → generic 409. | Dev – resolve | |
| DEF-OPD-14 | Low | 🔴 Still Open | Header wallet badge stale after refund | Refund a visit, look at header | Updated balance | Old balance until page reload (reproduced twice) | Refresh the wallet badge after refund/register | Dev – resolve | |
| DEF-OPD-15 | Low | 🔴 Still Open | Cancel appointment has no confirmation | Row → Cancel | Confirm dialog | Cancels immediately | Use `ConfirmDialog` (ADR-007 destructive-action rule) | Dev – resolve | |
| DEF-OPD-16 | Low | 🔴 Still Open | Refund endpoint ignores `{id}` in URL | `POST /api/opd-payments/<random>/refund` with valid `opdRegistrationId` | 404 / 400 mismatch | 200, refund done | Use the path id or check it matches the body | Dev – resolve | |
| DEF-OPD-17 | Low | 🔴 Still Open | Unknown `/api/*` routes return 200 HTML | `GET /api/doctors` | 404 JSON | 200 + `index.html` | Exclude `/api` from the SPA fallback | Dev – resolve | |
| DEF-OPD-18 | Low | 🟢 Fixed | Close Session disabled with no explanation | Counter page with open session, no handover | Hint "hand over cash first" | Greyed-out button only | Show the reason next to the button | Dev – resolve | |
| DEF-OPD-19 | High | 🔴 Open (new, found in re-test) | Session cannot be refreshed — users are signed out when the 60-min access token expires | Login as Receptionist → run `POST /api/auth/refresh` (what `AuthApiClient.RefreshTokenAsync` calls) | New access token issued from the refresh token; user stays signed in | 200 `{"success":false,"message":"Invalid or expired refresh token."}` immediately after a fresh login. No refresh cookie exists on `localhost:7246`. | Follow-on from DEF-AUTH-OPD-02 (access token is now 60 min). Auth `RefreshTokenCommand` reads the `refresh_token` **cookie** (+ `X-Device-Id` header). App `AuthController.ForwardAsync` only copies `Authorization` / `X-ClinicApp-Id`, never forwards the cookie and never relays Auth's `Set-Cookie` from login/refresh. Web.Client only calls `RefreshTokenAsync` from `MyClinicRequests.razor` — there is no automatic refresh before expiry or on 401. Fix: relay the refresh cookie through the App proxy (or return the refresh token and post it in the body) and refresh in the client before `tenantcore_token_expiry`. | Dev – resolve (App) | |
| DEF-OPD-20 | High | 🔴 Open (new, found in re-test) | Selecting a doctor wipes the fee typed in New OPD Entry — visit is saved at ₹0 | New OPD Entry → pick DUMMY Patient 002 → Fee `300` → Select Doctor “Clinic1 Admin” → Register | Fee typed by reception is kept (only overwritten when the doctor has a configured fee) | Request body sent `fee:null`; OPD-20261104-0001 saved with fee ₹0, collected ₹0 (wallet still charged ₹1) | Changing the doctor calls `GET /api/doctor-fee-configs/by-doctor/00000000-0000-0000-0000-000000000000` (doctor has no profile → 204) and the form resets the fee to empty. Keep the typed fee when no fee config is returned; also avoid calling with an empty profile id. Revenue loss risk. | Dev – resolve (App) | |
| DEF-OPD-21 | Low | 🔴 Open (new, found in re-test) | Delete confirmation shows a blank patient name | OPD row (cancelled, refunded) → Delete | “…cancelled OPD record for **DUMMY Patient 011**? …” | “Permanently delete this cancelled OPD record for **?** This cannot be undone.” (seen on OPD-20261104-0002 right after cancelling it in the same page session; for OPD-20261104-0001 the name showed) | Dialog text is bound to a stale/empty selected item. | Dev – resolve (App) | |

## Re-test — 04-Nov-2026 (fix branches)

| Item | Detail |
|---|---|
| App | `fix/def-opd-01-clinic-staff-directory` (`771a56d` DEF-AUTH-OPD-01 App side, merged with `main` @ `59f9c7c`) running at `https://localhost:7246` |
| Auth | `testing/opd-reception-auth-access` (`eaa8ab4` + `9a65355` DEF-AUTH-OPD-01, `5e80e66` DEF-AUTH-OPD-02) running at `https://localhost:8081` |
| Accounts | Receptionist `awez1411+clinic1_recep1@gmail.com` · Doctor/Clinic Admin `awez1411+clinic1admin@gmail.com` |
| Tested on | 04-Nov-2026, 00:35–00:47 IST, by QA (Claude), through the UI in Chrome; API calls made from the logged-in receptionist's browser session with her own token |
| Environment notes | Before testing could start, three local config problems were fixed by the developer: (1) App `Jwt:Secret` did not match Auth's `Jwt-SecretKey` → every App call 401 `The signature key was not found`; (2) a local override kept Auth `Jwt:ExpirationMinutes` at 525,960; (3) App `ConnectionStrings:ClinicConnection` pointed at a different database than Admin's `ClinicReadOnly` → clinic showed “no active subscription” / 402. None of these are code defects on the branches, but anyone running the branches locally needs the App secret and connection string aligned with Auth/Admin. |

**Result:** 4 fixed (DEF-OPD-01, 02, 08, 18) · 14 still open · 3 new (DEF-OPD-19, 20, 21). Only DEF-OPD-01/02 were targeted by the fix branch; DEF-OPD-08 and 18 are also fixed now. Every other defect reproduced with the original root cause unless noted.

| Defect ID | Status | Verified with (exact steps) / what was observed | Same root cause or new? |
|---|---|---|---|
| DEF-OPD-01 | 🟢 Fixed | Recep → Clinic One Care → OPD Queue → + New OPD Entry: “Select Doctor” lists **Clinic1 Admin** (`GET /api/clinic-settings/doctors` 200; `GET /api/Application/{appId}/staff-directory` 200). Searched “DUMMY” → DUMMY Patient 002 → doctor Clinic1 Admin → Register → toast “OPD registration OPD-20261104-0001 created.” (`POST /api/opd-registrations` 201). Wallet ₹997 → ₹996. | — |
| DEF-OPD-02 | 🟢 Fixed | Recep → Handovers → + New Handover: recipient list shows “Clinic1 Admin (Doctor)” (`staff-directory?roles=Clinic Admin,Clinic Manager,Doctor` 200) → Send Rs. 550 (201, Pending). Login as Clinic Admin → Doctor Panel → Handovers → **Accept** → Accepted. Login as Recep → Counter → **Close Session** enabled → “Session closed.” | — |
| DEF-OPD-03 | 🔴 Still Open | OPD row → ⋯ → Visit Details → **Cancel Appointment** → “Something went wrong” page. Console: `NullReferenceException at OpdRegistrationList.CancelAsync … OpdRegistrationList.razor:line 991`, called from line 468. | Same root cause (`CloseViewModal()` clears `_selected` before `CancelAsync(_selected)`) — not changed on this branch. |
| DEF-OPD-04 | 🔴 Still Open | Registered DUMMY Patient 011 with Clinic1 Admin, fee ₹200 (collected ₹200, wallet ₹996 → ₹995). Visit still Waiting → discount ₹50 → refund → ₹50 refunded, **wallet ₹995 → ₹996** (“Refund — OpdRegistration”). | Same root cause (`ReverseUsageChargeAsync` runs on every refund). |
| DEF-OPD-05 | 🔴 Still Open — **symptom changed, now worse** | Continued DEF-OPD-04: row → Cancel. Row now shows **“Refunded”** with only Delete (no Refund) although ₹150 is still collected; payment `refundDue 0`, `refundStatus Refunded`. Refund → 409 “No refund is pending for this payment.” **Delete now succeeds** (confirm dialog → 204) and the counter session total drops ₹700 → ₹550: the ₹150 the patient paid is neither refunded nor kept in the cash totals. Previously the refund failed with a 409 unique-index error and the visit was stuck; now no second refund is ever attempted, so the money silently disappears. | Same area (refund state after a partial refund — cancel does not re-open a refund for the remaining amount, and `NeedsRefund`/`CanDelete` trust `RefundStatus`), but the behaviour is new. Treat as a money-loss defect. |
| DEF-OPD-06 | 🔴 Still Open | `POST /api/opd-registrations` with a random `doctorUserId` and `doctorName:"Dr Fake"`, fee 100 → **201** (OPD-20261104-0002), wallet charged ₹1. | Same root cause. |
| DEF-OPD-07 | 🔴 Still Open (partly improved) | New OPD Entry → patient + fee 300 → leave “— No Doctor —” → Register → `POST` 400. The UI now shows a toast: “'Doctor User Id' must not be empty. 'Doctor Name' must not be empty.” (no longer silent). The field is still labelled **“Select Doctor (optional)”** and the message is raw validator text. | Same root cause (label vs validator); only the silent-failure half is fixed. |
| DEF-OPD-08 | 🟢 Fixed | New OPD Entry → Register with no patient → toast “Please select a patient.”; no API request sent. | — |
| DEF-OPD-09 | 🔴 Still Open | OPD-20261104-0001 (fee ₹0, see DEF-OPD-20) → row Cancel → “₹0 Paid · Refund due” → Delete → confirm → 409 “Refund the collected amount before deleting this OPD registration.” Refund → 409 “No refund is pending for this payment.” Wallet stays ₹996 (₹1 not returned). | Same root cause. |
| DEF-OPD-10 | 🔴 Still Open | `POST` fee 99,999,999,999 → **201** (OPD-20261104-0003), auto-collected; session total ₹1,00,00,00,00,749. | Same root cause. |
| DEF-OPD-11 | 🔴 Still Open | `POST` O2 250, Temp 500, Pulse 900, Weight 9999, BP “abc” → **201** (OPD-20261104-0004), stored as sent. | Same root cause. |
| DEF-OPD-12 | 🔴 Still Open | New OPD Entry opens with Pulse **72** and O2 **98** pre-filled; the register request body carried `pulseRate:72, oxygenSaturation:98`; Visit Details for OPD-20261104-0001 shows 72 bpm / 98 %. | Same root cause. |
| DEF-OPD-13 | 🔴 Still Open | Deleted OPD-20261104-0002 (DUMMY Patient 011) while it was the day's latest → next registration (DUMMY Patient 012) was issued **OPD-20261104-0002** again. | Same root cause. |
| DEF-OPD-14 | 🔴 Still Open | After registering: badge ₹997, actual ₹996. After a UI full refund of OPD-20261104-0004: badge ₹994, actual ₹995. Badge only updates after navigating to another page. | Same root cause. |
| DEF-OPD-15 | 🔴 Still Open | OPD row → **Cancel** → cancelled immediately (“Appointment cancelled.”), no confirmation. (Delete does use the confirm dialog.) | Same root cause. |
| DEF-OPD-16 | 🔴 Still Open | Cancelled OPD-20261104-0003 → discount = full amount → `POST /api/opd-payments/{random-guid}/refund` with `opdRegistrationId` of 0003 → **200**, refund done. `ProcessOpdRefundRequest` still only carries `OpdRegistrationId`. | Same root cause. |
| DEF-OPD-17 | 🔴 Still Open | `GET /api/doctors` → **200 `text/html`** (SPA page). | Same root cause. |
| DEF-OPD-18 | 🟢 Fixed | Counter page with an open session and a pending handover shows “⚠ Accept the pending handover before closing.” + “Go to Handovers →” next to the disabled Close Session; after the doctor accepts, Close Session is enabled. (State with no handover sent at all was not re-checked.) | — |
| DEF-OPD-19 | 🔴 Open (new) | See Defects table. | New — follow-on from DEF-AUTH-OPD-02. |
| DEF-OPD-20 | 🔴 Open (new) | See Defects table. | New. |
| DEF-OPD-21 | 🔴 Open (new) | See Defects table. | New. |

Also confirmed working in this run: full refund through the UI on a cancelled visit (OPD-20261104-0004: Refund → “Refund Full Amount (Rs. 100.00)” → “Refund returned to patient.”, session −₹100, wallet +₹1); delete of refunded visits; double refund → 409; Receptionist cannot read the full staff roster (`GET /api/Application/{appId}/users` → 403).

**Test data after re-test:** OPD-20261104-0001 (DUMMY Patient 002, ₹0, Cancelled, cannot be deleted — evidence for DEF-OPD-09) is kept. OPD-20261104-0002/0003/0004 were cancelled, refunded and deleted. The counter session opened 02-Nov was handed over (₹550 → Clinic1 Admin, Accepted) and **closed**. Wallet at end: **₹996.00**.

## Observations (not raised as defects yet)

| # | Observation |
|---|---|
| OBS-1 | `POST /api/patients` took ~25 s locally with no loading state on "Register Patient" (risk of double-submit). Wallet page also took ~60 s to render once. Worth profiling. |
| OBS-2 | Receptionist sees the "Top up the wallet / Create payment link" panel. Server requires Clinic Admin for top-up (per code, not exercised here) — consider hiding it for Reception. |
| OBS-3 | Page header shows "Dashboard" on the Wallet and Counter pages. |
| OBS-4 | Doctor "Clinic1 Admin" has no doctor profile (`profileId` empty), so doctor fee config can't apply. |
| OBS-5 | (04-Nov) The counter session opened on 02-Nov was never closed (DEF-OPD-02), so visits registered on 04-Nov were collected into the 02-Nov session. Consider forcing a new session per day or warning when the open session is from an earlier date. Page header still shows “Dashboard” on Counter and Handovers (OBS-3). |

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
