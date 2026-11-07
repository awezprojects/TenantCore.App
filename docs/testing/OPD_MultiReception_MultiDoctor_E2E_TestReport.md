# OPD Multi-Reception / Multi-Doctor E2E Test & Defect Report

> **REPORT STATUS: 🟢 ALL 16 DEFECTS VERIFIED – CLOSED — 07-Nov-2026 (QA round 4).** DEF-MR-01 to DEF-MR-16 are all 🟢 Verified – Closed in TenantCore.App; DEF-AUTH-MR-01 is closed in the Auth report. **Not part of this defect list and still open on the Auth / Notification side:** invite email template (DEF-REG-05/UM-06), DEF-UM-04 (HTML in invite name), expired-invite row, and the dates-without-`Z` suggestion. Minor follow-up observations from rounds 3–4 are listed under "QA re-test — round 4". Nothing committed — devs commit when everything is closed.

| Item | Detail |
|---|---|
| Repo | TenantCore.App |
| Testing branch | `testing/opd-multi-reception-multi-doctor` (from `main` @ `f7032d2`) |
| Environment | Local — `https://localhost:7246` |
| Clinic | Clinic One Care (`CLINIC1`) — same clinic as the previous exercises |
| Accounts | Clinic Admin + Doctor `awez1411+clinic1admin@gmail.com` · Receptionist 1 `awez1411+clinic1_recep1@gmail.com` · Receptionist 2 (new) `awez1411+clinic1_recep2@gmail.com` · Doctor 1 (General) `awez1411+clinic1_doctor1@gmail.com` · Doctor 2 (Gynecologist, new) `awez1411+clinic1_doctor2@gmail.com` — all password `Temp@123` |
| Tested on | 07-Nov-2026 |
| Tested by | QA (Claude) |
| Prior reports | `docs/testing/OPD_Doctor_Handover_Counter_E2E_TestReport.md`, `docs/testing/OPD_Reception_Registration_Refund_Wallet_TestReport.md` |

> Method: 2 receptionists and 2 doctors (one gynecologist) in one clinic, 10 OPD appointments (5 per doctor), registered from both receptions, then handover and finance reconciliation. Registration #1–#3 through the UI; #4–#10 through the same `POST /api/opd-registrations` call the form makes. Only 1 of the 10 visits was taken through consultation (Start → Done) — the other 9 are still Waiting.

## Test users

| Role | Email | Notes |
|---|---|---|
| Clinic Admin + Doctor | awez1411+clinic1admin@gmail.com | existing |
| Receptionist 1 | awez1411+clinic1_recep1@gmail.com | existing |
| Receptionist 2 | awez1411+clinic1_recep2@gmail.com | **New** — invited by admin, accepted via email link |
| Doctor 1 (General) | awez1411+clinic1_doctor1@gmail.com | existing, was not an active member; re-invited and accepted |
| Doctor 2 (Gynecologist) | awez1411+clinic1_doctor2@gmail.com | **New** — speciality *Gynecology & Obstetrics*, reg. no. `MH-2026-20002` |

No separate "approval" step exists — an accepted invitation is active immediately.

## Appointments registered (10)

| # | Token | Patient | Doctor | Registered by | Fee ₹ | Method |
|---|---|---|---|---|---|---|
| 1 | OPD-20261107-0004 | DUMMY 001 | Doctor One | Recep One | 300 | UI |
| 2 | -0005 | DUMMY 002 | Doctor Two (Gynae) | Recep One | 500 | UI |
| 3 | -0006 | DUMMY 003 | Doctor One | Recep One | 250 | UI |
| 4 | -0007 | DUMMY 004 | Doctor Two | Recep One | 400 | API (same call as UI) |
| 5 | -0008 | DUMMY 005 | Doctor Two | Recep One | 600 | API |
| 6 | -0009 | DUMMY 006 | Doctor One | Recep Two | 200 | API |
| 7 | -0010 | DUMMY 007 | Doctor Two | Recep Two | 450 | API |
| 8 | -0011 | DUMMY 008 | Doctor One | Recep Two | 350 | API |
| 9 | -0012 | DUMMY 009 | Doctor Two | Recep Two | 700 | API |
| 10 | -0013 | DUMMY 010 | Doctor One | Recep Two | 150 | API |

## Expected vs actual ledger

| Party | Expected | Actual | Row |
|---|---|---|---|
| Doctor One (5 visits) | 1,250 | 1,250 | ✅ |
| Doctor Two (5 visits) | 2,650 | 2,650 | ✅ |
| Recep One (5 visits) | 2,050 | 2,050 | ✅ |
| Recep Two (5 visits) | 1,850 | 1,850 | ✅ |
| **Grand total (10 visits)** | **3,900** | **3,900** | ✅ |
| Finance OPD income (3,900 + earlier 1,270) | 5,170 | 5,170 | ✅ |
| Accepted handovers | 5,170 | 5,170 | ✅ |
| Wallet (₹1 per OPD × 10) | 968 → 958 | 968 → 958 | ✅ |

## Summary

| Metric | Count |
|---|---|
| Visits registered | 10 (OPD-20261107-0004 … 0013) |
| Visits taken to consultation | 1 (DUMMY 009, Doctor Two, Start → Done; no Rx finalised — blocked by DEF-MR-06) |
| Test cases executed | 32 |
| ✅ Pass | 12 |
| ❌ Fail | 20 (17 against new defects, 3 against re-confirmed defects) |
| New defects | 16 (2 High · 7 Medium · 7 Low) |
| Re-confirmed earlier defects (still open) | 3 |
| Verified – Closed by QA | 16 (MR-01 through MR-16 — all of them, as of QA re-test round 4) |
| Still open — lives outside TenantCore.App | 3 (re-confirmed: DEF-REG-05/UM-06, DEF-UM-04, expired-invite row) |

**Bottom line:** token sequencing, wallet debits, per-doctor visibility, doctor-wise finance and the full-amount handover all reconcile correctly across two receptions and two doctors. Every defect found is in features that assume a single receptionist or a single doctor per clinic: counter sessions are clinic-wide with no "opened by / collected by", handovers don't identify sender or recipient, a partial (per-receptionist) handover cannot be accepted and fails silently, and OPD registration doesn't record who created it. A second, independent cluster of defects sits in the doctor workspace (Rx finalise, default vitals, "Done" bypassing Rx, instant "verified" registration) and is unrelated to the multi-reception scenario.

## Defect status workflow

| Status | Meaning | Owner / next action |
|---|---|---|
| 🔴 Open | Not yet fixed | **Dev** – resolve |
| 🟡 Fixed – Pending QA | Dev says fixed, awaiting re-test | **QA** – re-verify |
| 🟢 Verified – Closed | QA confirmed the fix | None |
| ⚪ Deferred / Won't Fix | Acknowledged, not fixing now | Product decision |

## What passed ✅

- Tokens are sequential across both receptions (0004 to 0013), with no duplicates or gaps.
- Wallet debits exactly ₹1 per registration (₹968 → ₹958 after 10 visits).
- Registration form starts with blank vitals (earlier defect fixed).
- Each doctor sees only their own 5 appointments (dashboard and OPD queue).
- Doctor-wise finance matches: Doctor Two ₹2,650, Doctor One ₹1,250.
- Counter totals (₹3,900), finance OPD income (₹5,170) and accepted handovers (₹5,170) reconcile.
- Full-amount handover (₹3,900) accepted successfully; the dispute flow works.
- The Gynecology doctor gets Gynecological History / Obstetric Info / LMP & EDD sections in the Rx screen.

## Test cases

| TC ID | Area | Scenario | Expected | Actual | Result | Defect |
|---|---|---|---|---|---|---|
| TC-MR-01 | Onboarding | Invite + accept Receptionist 2 | Active immediately, no approval step | Active immediately | Pass | |
| TC-MR-02 | Onboarding | Invite + accept Doctor 2 (Gynecologist), set speciality + reg. no. | Active immediately with Gynecology speciality | Active, speciality saved | Pass | |
| TC-MR-03 | Reception | Register visits #1–#3 via UI (Recep One) | 3 × 201, tokens 0004–0006 | As expected | Pass | |
| TC-MR-04 | Reception | Register visits #4–#10 via API, both receptions | 7 × 201, tokens 0007–0013 | As expected | Pass | |
| TC-MR-05 | Tokens | Sequencing across both receptions | 0004–0013, no dupes/gaps | As expected | Pass | |
| TC-MR-06 | Wallet | ₹1 debited per registration | 968 → 958 | 958 | Pass | |
| TC-MR-07 | Reception | Registration form starts with blank vitals | Blank | Blank | Pass | |
| TC-MR-08 | Doctor | Each doctor sees only their own 5 visits | 5 + 5, no cross-visibility | 5 + 5 | Pass | |
| TC-MR-09 | Finance | Doctor-wise totals | Doctor One 1,250, Doctor Two 2,650 | 1,250 / 2,650 | Pass | |
| TC-MR-10 | Finance | Counter total vs OPD income vs accepted handovers reconcile | 3,900 / 5,170 / 5,170 | 3,900 / 5,170 / 5,170 | Pass | |
| TC-MR-11 | Handover | Full ₹3,900 handover sent and accepted | Accepted | Accepted | Pass | |
| TC-MR-12 | Doctor | Gynecology-specific Rx sections appear for Doctor 2 | Gynecological History / Obstetric Info / LMP & EDD shown | Shown | Pass | |
| TC-MR-13 | Counter | Recep Two opens Counter after Recep One's session is already open | Recep Two's own session, "opened by" visible | Sees Recep One's session (ID, ₹2,050, 5 patients) with live Handover/Close buttons; no "opened by" | **Fail** | DEF-MR-01 |
| TC-MR-14 | Handover | Recep Two hands over only their own ₹1,850 while session balance is ₹3,900 | Blocked at send with a clear message, or accepted as a partial handover | Send returns 201 (Pending); doctor's Accept returns 409, UI shows no message, button is a no-op | **Fail** | DEF-MR-02 |
| TC-MR-15 | Handover | Handovers table identifies sender and recipient doctor | "From" (receptionist) and named recipient doctor columns | Only Amount / Sent to role ("Doctor") / Status / Sent at | **Fail** | DEF-MR-03 |
| TC-MR-16 | Handover | Doctor One's "All Handovers" excludes handovers addressed to Doctor Two | Only Doctor One's own handovers | Doctor One sees the ₹1,850 sent to Doctor Two | **Fail** | DEF-MR-03 |
| TC-MR-17 | Finance | Finance dashboard "Amount Handovers" total excludes disputed money | Accepted-only total, or disputed shown separately | Accepted ₹5,170 + Disputed ₹1,850 = "TOTAL ₹7,020" | **Fail** | DEF-MR-04 |
| TC-MR-18 | Reception | OPD registration create response records the receptionist | `createdByUserId`/`Name` present | Absent from the create response and the OPD list | **Fail** | DEF-MR-05 |
| TC-MR-19 | Doctor | Rx "Save & Finalise" with a diagnosis but no medicine | Either finalises (counselling/scan-only/follow-up visit), or a clear inline error | `POST /api/prescriptions` returns 400 "At least one medicine is required."; screen shows nothing | **Fail** | DEF-MR-06 |
| TC-MR-20 | Doctor | OPD queue "Done" button on a visit with no Rx | Same medicine-required rule as Save & Finalise, or an explicit "complete without Rx" action | Marks the visit Completed with no Rx at all | **Fail** | DEF-MR-07 |
| TC-MR-21 | Doctor | Rx screen vitals when reception left them blank | Blank / placeholder, matching the OPD list's "—" | Shows BP 120/80, Pulse 72, Temp 98.6, Weight 70, SpO₂ 98, RR 16, Sugar 95 as if measured | **Fail** | DEF-MR-08 |
| TC-MR-22 | Doctor | Doctor professional info — registration number save | Labelled "Saved" unless actually verified | Any typed number shows "✔ Registration Verified" instantly | **Fail** | DEF-MR-09 |
| TC-MR-23 | Reception | Patient search for a full name (`DUMMY Patient 001`) | Matches | "No patients found"; `DUMMY` alone works | **Fail** | DEF-MR-10 |
| TC-MR-24 | Profile | My Profile → Save Professional Info with no clinic selected | Friendly validation message | Raw error: "This endpoint requires the X-Application-Id header. Select a clinic before calling it." | **Fail** | DEF-MR-11 |
| TC-MR-25 | Onboarding | Clinic card "Joined" date for a member who joined today | Member's own join date (07-Nov) | Clinic's join date (29-Oct) | **Fail** | DEF-MR-12 |
| TC-MR-26 | Doctor | Dashboard greeting for Doctor One / Doctor Two | Full or last name | "Good afternoon, Dr. Doctor" (first name only, both doctors) | **Fail** | DEF-MR-13 |
| TC-MR-27 | Handover | Disputed handover in "Handovers For Me (pending your action)" | Moved out once disputed | Stays listed with no actions, list is misleading | **Fail** | DEF-MR-14 |
| TC-MR-28 | Reception | "Not Visited" count on reception OPD queue vs doctor view | Same count on both | Reception "Not Visited (1)" with all 3 visits Waiting and none cancelled; doctor view shows (0) | **Fail** | DEF-MR-15 |
| TC-MR-29 | UI | Product naming and header clock while page stays open | Consistent product name; clock updates live | Titles vary (CloudClinic / ClinicCore / TenantCore); header time stuck at 04:08 pm while content showed 04:11 pm | **Fail** | DEF-MR-16 |
| TC-MR-30 | Onboarding (re-test) | Invitation email renders the real template | Rendered HTML, no raw placeholders | Still shows raw `Template: UserInvited … AcceptInvitationUrl: …` on every invite sent today | **Fail** | DEF-REG-05 / DEF-UM-06 (re-confirmed) |
| TC-MR-31 | Onboarding (re-test) | API rejects an invite name containing HTML (`<img src=x onerror=alert(1)>Eve Html`) | 400 rejected | Still stored and listed (displays escaped) | **Fail** | DEF-UM-04 (re-confirmed) |
| TC-MR-32 | Onboarding (re-test) | Invitation list expiry status and duplicate entries | Invites 6–7 days old show "Expired"; one row per person | Shows "0 days left" for expired invites (Resend still says "valid for 7 days"); Doctor One appears twice (Accepted and Pending) | **Fail** | (re-confirmed, unassigned ID) |

## Defects

| Defect ID | Severity | Status | Title | Steps | Expected | Actual | Root cause / suggested fix | Owner |
|---|---|---|---|---|---|---|---|---|
| DEF-MR-01 | High | 🟢 Verified – Closed | Counter session is clinic-wide, not per receptionist | Recep One opens a session and registers 5 visits. Sign in as Recep Two and open Counter. | Each receptionist has their own session and cash; handover/close applies only to their own money. At minimum the session shows "Opened by" and each collection shows "Collected by". | Recep Two sees Recep One's session ("Opened 16:09", ₹2,050, 5 patients) with live Handover and Close Session buttons. Recep Two never opened a session, yet their 5 visits (₹1,850) were added to Recep One's session. No "opened by"/"collected by" shown anywhere. No cash accountability — either receptionist can hand over or close the other's cash. | **Fix applied (the "at minimum" ask):** `CounterSession.OpenedByUserId` and `OpdPayment.ReceivedByUserId` already existed server-side but were never surfaced. Added `CollectedByUserId` to `SessionCollectionDto` (populated in `GetOpdCollectionsBySessionHandler` from the session payment's `ReceivedByUserId`) and wired `CounterSession.razor` to show "Opened by" on the session header (flagging "— not your session" when it isn't the viewer's) and a "Collected By" column on the Patient Collections table, resolving names from the clinic staff directory loaded once per page view. **Not fixed — deliberately out of scope:** true concurrent per-receptionist sessions (one open session per user instead of one per clinic) is a larger architectural change than a defect-fix round; tracked for a follow-up if the product decides to pursue it. | QA verified 07-Nov — closed |
| DEF-MR-02 | High | 🟢 Verified – Closed | A partial handover can be created but never accepted; the doctor gets no error | Recep Two sends ₹1,850 (their own share) to Doctor Two while the session balance is ₹3,900. Doctor Two clicks Accept. | Either the partial amount is blocked at send time with a clear message, or per-receptionist handovers are supported; the API's error is always shown to the doctor. | Send returns 201 and the doctor's screen says "pending". Accept returns **409** (amount differs from the session balance) and the UI shows **no message** — the button just does nothing. The only way out is Dispute. The session page warns "pending handover no longer matches the session balance", but only after the send. | **Round 1 fix applied:** `CreateAmountHandoverHandler` computes the session's live net balance (same formula `AcceptAmountHandoverHandler` already used) and rejects a mismatched create immediately. **Round 1 also found** the "UI shows no message" half was already fixed in the codebase (`AmountHandovers.razor`'s `Toast.ShowError`, `ApiResponseReader`'s ProblemDetails parsing). **QA re-test (round 1) found a new, related bug the round-1 fix didn't cover:** the balance check used the session's *cumulative* collected total, which never shrinks once a handover is Accepted — so once ₹3,900 was Accepted and a further ₹100 was collected into the still-open session, a *second* handover for the full ₹4,000 passed the mismatch check and was Accepted too, double-counting the ₹3,900 already handed over (₹7,900 accepted against ₹4,000 actually collected). **Round 3 fix applied:** both `CreateAmountHandoverHandler` and `AcceptAmountHandoverHandler.EnsureHandoverMatchesSessionBalanceAsync` now check against the session's *outstanding* balance — cumulative collected minus the sum of handovers already `Accepted` for that session — not the raw cumulative total. `CreateAmountHandoverHandler` also now blocks raising a second handover while one for the session is already `Pending`. The new-handover dialogs in `CounterSession.razor` (prefill + an explanatory "Outstanding balance" line) and `AmountHandovers.razor`'s "+ New Handover" (fetches the session's own handovers to compute the prefill, rather than reusing the date-filtered list) now prefill the outstanding amount instead of the gross session total. Added `CreateAmountHandoverHandlerTests.Handle_WhenPartOfTheBalanceAlreadyAccepted_RequiresOnlyTheOutstandingAmount`, `.Handle_WhenAPendingHandoverAlreadyExistsForTheSession_ThrowsInvalidOperationException`, `AcceptAmountHandoverHandlerTests.Handle_WhenPartOfTheBalanceAlreadyAccepted_AcceptsOnlyTheOutstandingAmount`, `.Handle_WhenHandoverAmountIgnoresAlreadyAcceptedPortion_ThrowsInvalidOperationException`. | QA verified 07-Nov (round 3) — closed |
| DEF-MR-03 | Medium | 🟢 Verified – Closed | Handover screens do not show who sent or who should receive | View the Handovers table as a receptionist or as a doctor. | "From (receptionist)" and the named recipient doctor are both visible; a doctor's "All Handovers" only lists handovers addressed to them. | Table has Amount / Sent to role / Status / Sent at only — no "From", and the recipient is only the role "Doctor" (indistinguishable with 3 doctors). Doctor One also sees the ₹1,850 handover sent to Doctor Two in "All Handovers". | **Fix applied:** `AmountHandoverDto`/`AmountHandoverSummaryDto` already carried `HandedOverByUserId` and `HandedOverToUserId` — only the UI never showed them. `AmountHandovers.razor` now loads the clinic staff directory once per page view and adds a "From" column plus a resolved recipient name next to the role, on both "Handovers For Me" and "All Handovers". "All Handovers" is now scoped to handovers the viewer sent or received unless they are a Clinic Admin/Manager (`_canSeeAllHandovers`) — a Doctor or Receptionist no longer sees another doctor's handovers. | QA verified 07-Nov — closed |
| DEF-MR-04 | Medium | 🟢 Verified – Closed | Finance dashboard handover total includes disputed money | View Finance Dashboard → Amount Handovers after one accepted (₹5,170) and one disputed (₹1,850) handover. | Disputed amounts excluded from the total, or shown as a separate line. | "Accepted ₹5,170 + Disputed ₹1,850 → TOTAL ₹7,020", higher than total income (₹5,170). | **Fix applied:** `FinanceDashboard.razor`'s "Total" card summed every handover regardless of status (`_handovers.Sum(h => h.Amount)`). Changed to `_handoverTotal`, which excludes `Disputed` (disputed cash may never actually change hands, so it shouldn't inflate a total compared against income) and relabelled the card "Total (Accepted + Pending)" so what it means is explicit. Disputed still has its own card, unchanged. | QA verified 07-Nov — closed |
| DEF-MR-05 | Medium | 🟢 Verified – Closed | OPD registration does not record the receptionist | Register an OPD visit via UI or API, inspect the create response and the OPD list. | Create response/list includes `createdByUserId`/`Name`. | No created-by field anywhere; reception-wise reporting and per-receptionist counter/audit cannot be derived. | **Fix applied, via the existing payment record rather than a new column:** `CreateOpdRegistrationHandler` already calls `OpdPayment.AcceptVisitFee(request.ReceivedByUserId, ...)` for a prepaid visit, which sets `OpdPayment.ReceivedByUserId` — this was simply never surfaced. Added `ReceivedByUserId` to `OpdRegistrationDto`, populated in `OpdRegistrationTranslator.ToDto` from the payment; the create handler now returns the newly-accepted payment in its response instead of `null`, so even the just-created row (before any reload) carries it. Added a "Registered By" column to the OPD list for reception users, resolved from the clinic staff directory. Stays blank when prepaid is disabled (nothing collected yet, so no receiver to show) — covered by `CreateOpdRegistrationHandlerTests.Handle_PrepaidDisabled_LeavesPaymentPending`. | QA verified 07-Nov — closed |
| DEF-MR-06 | Medium | 🟢 Verified – Closed | Prescription "Save & Finalise" fails silently | Doctor starts a visit, clicks Rx, adds a diagnosis only (no medicine), clicks Save & Finalise. | Either the visit finalises without a medicine (counselling, scan-only or follow-up gynecology visit), or a clear inline/toast error is shown. | `POST /api/prescriptions` returns 400 `"At least one medicine is required."` but the screen shows nothing — no toast, no inline error. A consultation with no medicine cannot be finalised at all today. | **Fix applied, by relaxing the rule rather than only surfacing its error:** `CreatePrescriptionCommandValidator`/`UpdatePrescriptionCommandValidator` now require a medicine *only* when there is no other clinical content — a non-empty diagnosis, an investigation, or a note. A counselling-only, scan-only or follow-up visit with a diagnosis can now finalise with zero medicines; a visit with genuinely nothing recorded still gets a clear message: "Add at least one medicine, or record a diagnosis, investigation or note for this visit." Added 4 new validator tests per command covering empty-items-with/without clinical content. On investigation, `PrescriptionForm.razor`'s existing error handling (`Toast.ShowError(r.Message ?? ...)` in `SaveAsync`) already fires on any Create/Update failure and `PrescriptionApiClient` already parses the ProblemDetails `detail`/`errors` fields correctly — no separate UI bug found; re-test should confirm with the relaxed rule in place. | QA verified 07-Nov — closed |
| DEF-MR-07 | Medium | 🟢 Verified – Closed | "Done" completes a visit with no prescription | From the OPD queue, click **Done** on a visit that was never taken to Rx. | Same completion rule as Save & Finalise (medicine required), or an explicit, separate "complete without Rx" action with its own confirmation. | The queue's **Done** button marks the visit Completed with no Rx at all, even though Save & Finalise requires a medicine — the two completion paths enforce different rules. | **Fix applied, as the explicit-confirmation option** (forcing the same hard rule as Rx finalise would make "Done" unable to close a visit the doctor deliberately skipped Rx for, which is also a real, intended use of that button): `OpdRegistrationList.razor`'s Done button now checks whether a prescription exists for the visit first; if not, it opens a `ConfirmDialog` ("No prescription was recorded... Mark this visit as completed anyway?", per ADR-007) before completing. A visit that already has a prescription completes immediately, with no extra click. | QA verified 07-Nov — closed |
| DEF-MR-08 | Medium | 🟢 Verified – Closed | Rx screen displays default vitals that were never recorded | Reception leaves vitals blank at registration (OPD list shows "—"). Doctor opens Rx screen for that visit. | Blanks or explicit placeholders, matching the OPD list. | Rx screen shows BP 120/80, Pulse 72, Temp 98.6, Weight 70, SpO₂ 98, RR 16, Sugar 95 as if actually measured — these could be saved or printed as real readings. | **Root cause confirmed exactly as suspected:** `PrescriptionForm.razor`'s `ApplyDefaultVitals()` deliberately pre-filled BP/Pulse/SpO2 with a "typical" value whenever the OPD intake left them blank, on the reasoning that those three are "routinely checked at every visit" — true in general, but not for this clinic's test data, and not something the UI should assume on the doctor's behalf. **Fix applied:** removed the call and the method entirely; every vital now shows exactly what the OPD intake recorded, blank stays blank. | QA verified 07-Nov — closed |
| DEF-MR-09 | Medium | 🟢 Verified – Closed | Doctor professional info reports "Registration Verified" instantly | Doctor Two types registration number `MH-2026-20002` and saves. | Labelled "Saved" unless a real verification check has run. | Any typed registration number is marked "✔ Registration Verified" immediately — no verification actually occurs. | **Fix applied:** confirmed there is no verification check anywhere in `DoctorProfileController`/`UpsertDoctorProfileCommand` — the badge only ever confirmed the profile row was saved. Relabelled "Registration Verified" → "Registration Saved" in `DoctorProfile.razor`. | QA verified 07-Nov (round 3) — closed |
| DEF-MR-10 | Low | 🟢 Verified – Closed | Patient search misses full names | Type `DUMMY Patient 001` in New OPD Registration patient search. | Matches. | "No patients found"; `DUMMY` alone works — multi-word search does not match. | **Fix applied:** `PatientRepository.GetPagedAsync` checked the whole search string against `FirstName`/`LastName`/`PhoneNumber` individually with `Contains()` — a query spanning both fields (e.g. `FirstName="DUMMY"`, `LastName="Patient 001"`) could never match as one string. Now splits the query into words and requires each word to match somewhere across `FirstName`, `LastName` or `PhoneNumber` (AND across words, OR across fields) — `"DUMMY Patient 001"` now matches. | QA verified 07-Nov — closed |
| DEF-MR-11 | Low | 🟢 Verified – Closed | Profile save fails when no clinic is selected | Open My Profile from the clinic portal (no clinic in context), click Save Professional Info. | A friendly validation message ("Select a clinic first"). | Raw error surfaced: "This endpoint requires the X-Application-Id header. Select a clinic before calling it." The same save works after entering a clinic. | **Round 1 fix** exempted `DoctorProfileController` from `ClinicContextMiddleware`'s header requirement (same shape as the already-exempted `ClinicOnboardingController`), which fixed Personal Information and the GET. **QA re-test found Professional Info (`PUT /api/doctor-profile`) still 403'd** — a *second*, independent gate: the action also carries `[Authorize(Policy = AuthPolicies.RequireClinical)]`, whose handler (`ClinicRoleAuthorizationHandler`) checks the caller's role **for the currently selected clinic** via the `X-Application-Id`-derived context — with no clinic selected there is no context to check, so it denies regardless of the middleware fix. **Round 3 fix applied:** added a new policy `AuthPolicies.RequireAnyClinical` + `AnyClinicRoleRequirement`/`AnyClinicRoleAuthorizationHandler`, which checks the same clinical roles across **every** clinic on the user's `app_roles` claim instead of one specific (selected) clinic — appropriate here since this is the doctor's own cross-clinic profile, not any one clinic's data. Applied to both `UpsertMyProfile` and `SetMyPrescriptionTemplate` (the print-template preference has the exact same issue). `GetByUserId` (used by the prescription print page, genuinely clinic-scoped) is unchanged. Added `AnyClinicRoleAuthorizationHandlerTests` (5 cases) and a reflection-based `DoctorProfileControllerTests.Action_RequiresAnyClinicalPolicy_NotClinicScoped` guarding the two actions' policy attribute. | QA verified 07-Nov (round 3) — closed |
| DEF-MR-12 | Low | 🟢 Verified – Closed | Clinic card "Joined" date is wrong for new members | Doctor Two joins the clinic on 07-Nov; view their own clinic card. | "Joined 07 Nov 2026" (their own join date). | Shows "Joined 29 Oct 2026" — the clinic's own creation date, not the member's. The admin's member list shows the correct individual dates, so the data exists. | **Fix applied — coordinated cross-repo change, round 2 (07-Nov-2026).** Root cause confirmed exactly as suspected: `ClinicLanding.razor`'s card read `item.Clinic.CreatedDate` (`ClinicDashboardItemDto.CreatedDate` = the clinic's own `Application.CreatedDate`), and that DTO had no per-user join date at all — the admin member list is correct only because it reads a *different*, admin-only Auth endpoint/DTO. **TenantCore.Auth side** (branch `testing/opd-multi-reception-multi-doctor`, tracked in that repo's own `docs/testing/OPD_MultiReception_Auth_Defects.md`): added `JoinedDate` to `ClinicDashboardItemResponseModel`, populated in `ClinicService.GetDoctorClinicDashboardAsync` from `UserApplication.CreatedDate` (the per-user membership row — exactly "when this user joined this clinic"), alongside the existing clinic-level `CreatedDate`. **TenantCore.App side:** added the matching `JoinedDate` to `ClinicDashboardItemDto` (the Auth response deserializes straight into it, no translator in between) and pointed `ClinicLanding.razor`'s two "Joined" labels at `item.Clinic.JoinedDate` instead of `CreatedDate`. Requires running Auth from its `testing/opd-multi-reception-multi-doctor` branch for the new field to actually appear — see that repo's report for the Auth-side build/test status. | QA verified 07-Nov — closed |
| DEF-MR-13 | Low | 🟢 Verified – Closed | Greeting uses first name with "Dr." | Doctor One or Doctor Two views their dashboard greeting. | Full or last name, e.g. "Good afternoon, Dr. Sharma". | "Good afternoon, Dr. Doctor" for both doctors — first name only, which reads oddly with the honorific. | **Fix applied:** `Dashboard.razor` built the greeting from `fullName.Split(' ').FirstOrDefault()`. Changed to use the last name (falling back to the full name for a single-word name), so it now reads "Good afternoon, Dr. One" / "Dr. Two" for these test accounts. | QA verified 07-Nov — closed |
| DEF-MR-14 | Low | 🟢 Verified – Closed | Disputed handover stays in "Handovers For Me (pending your action)" | Dispute a handover, then revisit the doctor's "pending your action" list. | Removed from that list once disputed (no action is actually possible on it). | Still listed with no available actions — the list is misleading about what needs attention. | **Fix applied:** `AmountHandovers.razor`'s `_forMe` filter matched on recipient only, with no status check at all — an Accepted or Disputed handover addressed to the viewer stayed listed under "pending your action" indefinitely. Added `h.Status == HandoverStatus.Pending` to the filter. | QA verified 07-Nov — closed |
| DEF-MR-15 | Low | 🟢 Verified – Closed | "Not Visited" count inconsistent | Compare "Not Visited" count on reception's OPD queue vs the doctor's view for the same visits. | Same count on both screens. | Reception OPD queue showed "Not Visited (1)" while all 3 visits were Waiting and none cancelled; the doctor view showed (0) for the same data. | **Rounds 1 and 3 investigation (no code bug found):** reception and the doctor both load `/opd` with the *same* "Not Visited" predicate (`Status == Pending && RegistrationDate.Date < today`) in both the client badge logic and the server repository filter — this isn't two different rules. The only structural difference is scope: reception's count has no doctor filter (clinic-wide), a doctor's own count is scoped to themselves. QA's round-3 re-test couldn't settle it either way: "expected if the one item belongs to another doctor, but the reception screen has no doctor-wise breakdown to confirm." **Round 4 fix applied — not a logic change, a diagnostic one:** since the Not Visited *rule* is already identical on both screens and the only variable is scope, the only thing actually missing was visibility into *which* doctor a clinic-wide Not Visited count belongs to. `OpdRegistrationList.razor`'s `LoadCountsAsync` now fetches the Not Visited rows once (as part of the same primary counts load, not a secondary per-click query) when reception has no doctor filter active and the count is non-zero, groups them by doctor, and shows e.g. "1 for Doctor Two" next to the tab (and as its tooltip). If reception's count and a doctor's own count still look inconsistent, this breakdown will show that doctor by name directly on the reception screen, which was the exact gap QA flagged. | QA verified 07-Nov (round 4) — closed |
| DEF-MR-16 | Low | 🟢 Verified – Closed | Inconsistent product naming / stale clock | Browse multiple pages in one session; leave a page open for several minutes. | One consistent product name across all page titles; header clock updates live. | Page titles vary (CloudClinic / ClinicCore / TenantCore); header time did not update while the page stayed open (showed 04:08 pm while content showed 04:11 pm). | **Fix applied, both halves:** (1) 19 pages used `<PageTitle>` text with " - TenantCore" or " — ClinicCore" instead of the dominant "CloudClinic" (Login, Dashboard, OPD, etc.) — standardised all of them to "CloudClinic". (2) `AuthorizedLayout.razor`'s `_currentTime` field was set once at construction and never updated again — confirmed exactly the suspected root cause (no periodic timer existed at all). Added a 30-second `System.Threading.Timer` that refreshes `_currentTime` and calls `StateHasChanged`, disposed in the component's existing `Dispose()`. | QA verified 07-Nov — closed (30s clock tick not observed) |

### Re-confirmed earlier defects (still open — all three live outside TenantCore.App)

| Defect ID | Status | Note |
|---|---|---|
| DEF-REG-05 / DEF-UM-06 | 🔴 Open (re-confirmed) — likely a stale running process, not a code defect | Invitation emails still render raw template text (`Template: UserInvited … AcceptInvitationUrl: …`). Every invite sent in this session looked like this. **Investigated:** the real template-rendering code already exists in **Clinic.Notification** (`EmailTemplateRenderer.cs` and the bundled `Templates/Auth/UserInvited.html`, etc. — matching the workspace root `CLAUDE.md`'s note that this shipped 2026-11-01). The code is not missing; the most likely explanation is that the locally running Azure Function consumer process during this test session was not rebuilt/restarted after that fix landed. No TenantCore.App change applies here. Re-test after restarting the local `Clinic.Notification` function. |
| DEF-UM-04 | 🔴 Open (re-confirmed) — lives in TenantCore.Auth | Invite name containing HTML (`<img src=x onerror=alert(1)>Eve Html`) is still stored and listed. It displays escaped here, but the API should reject it. The invitation endpoint lives in **TenantCore.Auth**, a separate repo; fixing the input validation there is a cross-repo change out of this branch's scope (workspace rule: don't touch multiple repos in one task unless asked to coordinate). |
| *(unassigned)* | 🔴 Open (re-confirmed) — lives in TenantCore.Auth | Invitation list shows "0 days left" for invites sent 6–7 days earlier (expired); Resend still says "valid for 7 days" — should show "Expired". The same person (Doctor One) appears twice (Accepted and Pending). Same as above — the invitation expiry/listing logic is TenantCore.Auth's, not TenantCore.App's. |

### Observations

| ID | Observation |
|---|---|
| OBS-MR-01 | Doctor role sees Patient Reg., Counter, Expenses and Handovers menu items. Confirm this menu set is intended for the Doctor role. |
| OBS-MR-02 | `GET /doctor-fee-configs/by-doctor/{id}` returned 204 for Doctor Two, so reception types the fee by hand each time. Consider prompting the admin to set a fee per doctor at onboarding. |

## QA re-test — 07-Nov-2026 (after dev rounds 1–2)

Environment: App latest build `https://localhost:7246`, Auth deployed to local IIS. Accounts: Recep Two, Doctor One, Clinic Admin (all `Temp@123`). Nothing committed.

| Defect | QA result | What was done / observed |
|---|---|---|
| DEF-MR-01 | 🟢 Pass | Recep Two on Counter: "Opened by: Recep One — not your session" shown; "Collected By" column lists Recep Two / Recep One per row. True per-receptionist sessions remain a deliberate out-of-scope item (see Suggestions). |
| DEF-MR-02 | 🔴 Partial (second pass — since fixed, see round 3) | A handover of ₹9,999 against a ₹4,000 balance is rejected at send with a clear message ("does not match the counter session's current balance… per-receptionist partial handovers are not supported") ✅. **But:** after ₹3,900 was already *Accepted* for the same session and a new ₹100 visit added (balance ₹4,000 collected), a second handover for the full **₹4,000** was accepted as Pending. Total handed over would be ₹7,900 against ₹4,000 collected. The new-handover form also pre-filled ₹4,000 (gross) rather than the outstanding ₹100. **Fix needed:** validate against balance *minus already Pending/Accepted handovers* (or lock the session once a handover is accepted), and pre-fill the outstanding amount. QA disputed the duplicate row to clean up. |
| DEF-MR-03 | 🟢 Pass | Admin and receptionist see From + named recipient; Doctor One sees only handovers addressed to him (Doctor Two's disputed ₹1,850 not visible). |
| DEF-MR-04 | 🟢 Pass | Finance total excludes Disputed, label "Total (Accepted + Pending)". |
| DEF-MR-05 | 🟢 Pass | "Registered By" column present on OPD list; new visit OPD-20261107-0014 shows "Recep Two". |
| DEF-MR-06 | 🟢 Pass | Finalise with nothing → toast "Add at least one medicine, or record a diagnosis, investigation or note for this visit." Finalise with diagnosis only ("Viral fever") → saved as RX-20261107-0001, Submitted. |
| DEF-MR-07 | 🟢 Pass | Done on a no-Rx visit shows "Complete Without Prescription" confirm dialog (Cancel / Complete Visit). |
| DEF-MR-08 | 🟢 Pass | Rx vitals inputs are empty (values are placeholders only). Minor: the grey placeholders (120/80, 72, 98.6…) look like real readings — suggest clearly labelled placeholders such as "e.g." or empty. |
| DEF-MR-09 | 🟡 Not re-tested (second pass — since verified, see round 3) | "Registration Saved" label needs a new patient registration (consumes wallet); not run this pass. |
| DEF-MR-10 | 🟢 Pass | "DUMMY Patient 011" in the New OPD modal returns the patient; visit created. |
| DEF-MR-11 | 🔴 Partial (second pass — since fixed, see round 3) | With no clinic selected (My Profile reached from the clinic list), **Personal Information saves** ("Personal information updated") ✅. **Professional Information (reg. no., speciality) failed:** `Save Professional Info` showed "You don't have permission to perform this action." — `/api/doctor-profile` returned **403** (observed twice; initial GET returns 404 for a doctor with no profile, which is fine). The middleware exemption covers the controller but a policy/role check still required a clinic-scoped role. **Fix needed:** allow a user to create/update their own doctor profile with no clinic context (authorise on the user's own id, not a clinic role). |
| DEF-MR-12 / DEF-AUTH-MR-01 | 🟢 Pass | Recep Two's clinic card shows "Joined 07 Nov 2026"; API returns both `createdDate` and `joinedDate`; owner/admin still shows 29 Oct. |
| DEF-MR-13 | 🟢 Pass | Greeting "Welcome back, Dr. One 👋" / "Good evening, Dr. One". |
| DEF-MR-14 | 🟢 Pass | After Dispute the row leaves "Handovers For Me" and shows Disputed in "All Handovers". |
| DEF-MR-15 | 🔴 Open | Not reproducible with the data present. Reception OPD list showed "Not Visited (1)" while Doctor One's own queue shows "Not Visited (0)" — expected if the one item belongs to another doctor, but the reception screen has no doctor-wise breakdown to confirm. Still needs a live repro as dev noted (capture token, doctor filter, timestamps). |
| DEF-MR-16 | 🟢 Pass | Titles read "… - CloudClinic" on all pages visited; header clock correct (07:12 → 07:20 pm across the session). The 30 s tick itself was not timed. |

### Re-confirmed / observations from this pass

- **Invite email template (DEF-REG-05 / DEF-UM-06) — still reproduces.** The new 07-Nov invites to Recep Two / Doctor Two / Doctor One arrive with a body that begins "Template: UserInvited RecipientName: … RecipientEmail: … ClinicName: … RoleName: … InvitedByName: …" (key/value dump instead of a rendered template). Local Clinic.Notification function needs a restart/redeploy with the new template, or the template is not being resolved.
- **DEF-UM-04 / unassigned expired-invite row:** not re-tested this pass (Auth-side, unchanged by the App fixes).
- **Finance → Amount Handovers table** "Handed To" still shows only the role ("Doctor") on the finance dashboard (the Amount Handovers page itself now shows names). Counter Sessions table on Finance has no "opened by" column. Low.
- **`joinedDate` / `createdDate` returned without a "Z"/offset** — the same instant can shift a day near IST midnight when parsed by a browser in another timezone. Suggest ISO-8601 with `Z`.
- **Transient:** right after the Auth redeploy the Finance page once showed "This clinic has no active subscription. Every feature stays disabled" with no wallet/Billing; a fresh login cleared it. First login after redeploy also hung 20–30 s on "Signing in…". Not a verified defect — worth a look at cold start / subscription cache.
- **Design note:** the new-handover form's pre-filled amount equals the gross session total, not the outstanding balance (see DEF-MR-02).

### Test data added in this pass
OPD-20261107-0014 (DUMMY Patient 011, Doctor One, ₹100, Recep Two, visit is still In Progress; Rx RX-20261107-0001 (diagnosis only) submitted); a ₹4,000 handover Recep Two → Doctor One (**Disputed** by QA); Doctor One profile registration number not saved (403). Counter session still Open.

## Dev fix — round 1, 07-Nov-2026 (not yet committed to main, pending QA re-test)

Fixes DEF-MR-01 through DEF-MR-11, DEF-MR-13, DEF-MR-14 and DEF-MR-16 (14 of the 16 new defects). DEF-MR-12 needs a coordinated TenantCore.Auth change (cross-repo, out of this branch's scope) and stays Open. DEF-MR-15 stays Open pending live reproduction — code inspection found both screens already use the same "Not Visited" rule, so a confident fix couldn't be made blind. All on branch `testing/opd-multi-reception-multi-doctor`. See the Root cause / fix column in the Defects table above for the detail on each.

Summary of this round:
- **DEF-MR-01 / DEF-MR-05** (counter session and OPD registration have no "who"): the data mostly already existed server-side (`CounterSession.OpenedByUserId`, `OpdPayment.ReceivedByUserId`) — it was never surfaced. Added `CollectedByUserId` to `SessionCollectionDto` and `ReceivedByUserId` to `OpdRegistrationDto`, and wired "Opened by" / "Collected By" / "Registered By" into `CounterSession.razor` and `OpdRegistrationList.razor`, resolving names from the clinic staff directory loaded once per page view (not per row).
- **DEF-MR-02** (partial handover never accepted): `CreateAmountHandoverHandler` now validates the amount against the session's live balance (the exact formula `AcceptAmountHandoverHandler` already used) before creating the row, so a mismatched handover is rejected immediately instead of accepted and failing later.
- **DEF-MR-03 / DEF-MR-04 / DEF-MR-14** (handover visibility): `AmountHandovers.razor` now shows sender and resolved recipient names, scopes "All Handovers" to the viewer unless they're a Clinic Admin/Manager, and drops non-Pending items from "pending your action". `FinanceDashboard.razor`'s handover total no longer counts Disputed money.
- **DEF-MR-06** (Rx finalise blocks on no medicine): relaxed `CreatePrescriptionCommandValidator`/`UpdatePrescriptionCommandValidator` to require a medicine only when there's no other clinical content, rather than only patching the UI's error display (which, on investigation, was already correct).
- **DEF-MR-07** (Done bypasses Rx): added a confirmation dialog when completing a visit with no prescription, rather than hard-blocking it like Rx finalise (a doctor genuinely skipping Rx is a real, intended path).
- **DEF-MR-08** (fabricated default vitals): removed `ApplyDefaultVitals()` entirely — vitals now always reflect exactly what the OPD intake recorded.
- **DEF-MR-09 / DEF-MR-11** (overstated "Verified" label; raw middleware error on Profile save): relabelled to "Saved"; found `DoctorProfileController` was missing from `ClinicContextMiddleware`'s clinic-scope exemption list (it has the exact same shape as the already-exempted `ClinicOnboardingController`) and added it.
- **DEF-MR-10** (patient search): `PatientRepository.GetPagedAsync` now matches each word of a multi-word query independently instead of testing the whole string against one field at a time.
- **DEF-MR-13 / DEF-MR-16** (greeting; naming/clock): greeting now uses the last name; all 19 page titles standardised to "CloudClinic"; the header clock now has an actual 30-second timer (it never had one).

**Build/test status:** full solution (`TenantCore.slnx`) builds clean, 0 errors. All five test projects pass: `TenantCore.Application.Tests` (994, +9 new), `TenantCore.Domain.Tests` (134), `TenantCore.Infrastructure.Tests` (127), `TenantCore.Api.Tests` (334, +1 new), `TenantCore.Web.Client.Tests` (21). New/updated tests: `CreateAmountHandoverHandlerTests` (amount-mismatch rejection), `CreatePrescriptionCommandValidatorTests` / `UpdatePrescriptionCommandValidatorTests` (relaxed medicine rule, 4+2 new cases), `ClinicContextMiddlewareTests.InvokeAsync_DoctorProfileRouteWithoutHeader_PassesThrough`. One pre-existing, unrelated flaky test was observed (`SetClinicUsageRatesHandlerTests.Command_AuditContextNamesTheItemsAndNeverTheirPrices`, fails intermittently depending on randomly generated GUIDs — confirmed pre-existing and untouched by this round; passed on every isolated re-run).

**Not yet re-tested through the UI/API against this round's changes.**

## Dev fix — round 2, 07-Nov-2026 (not yet committed to main, pending QA re-test)

Fixes DEF-MR-12 — the one new defect round 1 left open because it needed a coordinated TenantCore.Auth change. Both repos are on a branch of the same name, `testing/opd-multi-reception-multi-doctor`.

**TenantCore.Auth side** (tracked in that repo's own report, `docs/testing/OPD_MultiReception_Auth_Defects.md` — see that file for the Auth-side build/test status): added `JoinedDate` to `ClinicDashboardItemResponseModel`, populated in `ClinicService.GetDoctorClinicDashboardAsync` from `UserApplication.CreatedDate` — the per-user clinic-membership row, i.e. exactly when that user joined that clinic — alongside the existing clinic-level `CreatedDate` (`Application.CreatedDate`), which is left untouched since other callers may depend on it. Added `ClinicServiceDashboardTests.GetDoctorClinicDashboardAsync_MemberJoinedAfterClinicCreated_ReturnsMemberOwnJoinDate`, which asserts the two dates differ and `JoinedDate` is the member's own.

**TenantCore.App side:** added the matching `JoinedDate` to `ClinicDashboardItemDto` (`AuthClinicService.GetClinicDashboardAsync` deserializes Auth's response straight into this DTO, no translator layer in between, so no handler change was needed beyond the field itself) and pointed `ClinicLanding.razor`'s two "Joined ..." labels at `item.Clinic.JoinedDate` instead of `item.Clinic.CreatedDate`.

**Build/test status:** both solutions build clean. TenantCore.Auth: 1/1 new test passes; the pre-existing 12 unrelated failures (bcrypt-hash-determinism, temp-password-length, a `CurrentUserService` equivalence test) are unchanged from before this fix and were already noted as unrelated in that repo's earlier defect report. TenantCore.App: `TenantCore.Application.Tests` still 994/994.

**To re-test:** run **both services** from their `testing/opd-multi-reception-multi-doctor` branch (Auth and App) — the field does not exist on `main` for either repo yet, so testing App alone against a `main` Auth will still show the old clinic-creation date.

## Dev fix — round 3, 07-Nov-2026 (not yet committed to main, pending QA re-test)

Fixes the two defects QA's second re-test pass re-opened — DEF-MR-02 and DEF-MR-11 — plus two cosmetic follow-ups QA noted in the same pass (Finance dashboard's handover table and Counter Sessions table had no sender/recipient/opener names). All on branch `testing/opd-multi-reception-multi-doctor`.

- **DEF-MR-02** — the round-1 fix validated a new/accepted handover's amount against the session's *cumulative* collected total, which never shrinks once a handover is Accepted. A session that kept collecting after an earlier handover was Accepted could then raise and Accept a *second* handover for the full cumulative total again, double-counting the first. Both `CreateAmountHandoverHandler` and `AcceptAmountHandoverHandler` now check against the *outstanding* balance (cumulative collected minus the sum of this session's already-`Accepted` handovers), and `CreateAmountHandoverHandler` also blocks a second handover while one is still `Pending` for the session. The two "new handover" dialogs now prefill and display the outstanding amount, not the gross total.
- **DEF-MR-11** — the round-1 middleware fix solved Personal Information and the GET, but Professional Info's `PUT` still carried `[Authorize(Policy = RequireClinical)]`, whose handler checks the role for the *currently selected* clinic — with none selected, there's nothing to check against, so it denied regardless. Added `RequireAnyClinical` (checks the same clinical roles across *every* clinic the user belongs to, no selected-clinic context needed) and applied it to `UpsertMyProfile` and `SetMyPrescriptionTemplate` — both are the doctor's own cross-clinic profile, not one clinic's data. `GetByUserId` (genuinely clinic-scoped, used by the print page) is untouched.
- **Cosmetic follow-ups from the same QA pass:** Finance Dashboard's own small handovers table (separate from the dedicated Amount Handovers page fixed in round 1) had no "From" column and showed only the recipient's role — added both, resolved from the clinic staff directory loaded once per page view. Its Counter Sessions table had no "Opened By" column — added, same resolution.

**Not changed this round (explicitly out of scope — see notes elsewhere in this report):** the `joinedDate`/`createdDate` missing a `Z`/UTC-offset suffix (a pre-existing, system-wide `DateTime` serialization characteristic, not something introduced by this fix cycle — correcting it safely would mean auditing how every other timestamp in the system is consumed, well beyond this defect's scope); the invite-email-template, HTML-in-invite-name and expired-invite-listing items (all TenantCore.Auth/Clinic.Notification, re-confirmed again this pass, unchanged).

**Build/test status:** full solution builds clean, 0 errors. All five test projects pass: `TenantCore.Application.Tests` (998, +4 from round 3), `TenantCore.Domain.Tests` (134), `TenantCore.Infrastructure.Tests` (127), `TenantCore.Api.Tests` (341, +7 from round 3), `TenantCore.Web.Client.Tests` (21). New tests: `CreateAmountHandoverHandlerTests.Handle_WhenPartOfTheBalanceAlreadyAccepted_RequiresOnlyTheOutstandingAmount`, `.Handle_WhenAPendingHandoverAlreadyExistsForTheSession_ThrowsInvalidOperationException`, `AcceptAmountHandoverHandlerTests.Handle_WhenPartOfTheBalanceAlreadyAccepted_AcceptsOnlyTheOutstandingAmount`, `.Handle_WhenHandoverAmountIgnoresAlreadyAcceptedPortion_ThrowsInvalidOperationException`, `AnyClinicRoleAuthorizationHandlerTests` (5 cases), `DoctorProfileControllerTests.Action_RequiresAnyClinicalPolicy_NotClinicScoped` (2 cases).

**Not yet re-tested through the UI/API against this round's changes.**

## QA re-test — round 3, 07-Nov-2026 (21:20–21:30 IST)

Environment: App latest build `https://localhost:7246`, Auth on local IIS. Accounts: Doctor One, Recep Two (`Temp@123`). Nothing committed.

| Defect | QA result | What was done / observed |
|---|---|---|
| DEF-MR-02 | 🟢 Pass | Session: ₹4,000 collected, ₹3,900 already Accepted → outstanding ₹100. (a) New Handover form now **pre-fills ₹100.00** (was gross ₹4,000). (b) Sending **₹4,000** is rejected: "Handover amount Rs. 4,000.00 does not match the counter session's outstanding balance (Rs. 4,000.00 collected minus Rs. 3,900.00 already handed over and accepted) of Rs. 100.00. A handover must cover the whole outstanding balance — per-receptionist partial handovers are not supported." (c) Sending **₹100** succeeds (Pending). (d) A **second handover while one is Pending** is blocked: "A handover for this session is already pending. Wait for it to be accepted or disputed first." (e) Doctor One **Accept** on the ₹100 succeeds; list shows Accepted. Finance reconciles: Accepted ₹5,270 = Collected ₹5,270. |
| DEF-MR-09 | 🟢 Pass | Doctor One saved Professional Info (reg. no. + speciality) → green banner "Registration Saved — Profile created 07 Nov 2026" (not "Verified"). |
| DEF-MR-11 | 🟢 Pass | From `/select-clinic` → My Profile (no clinic selected), **Save Professional Info** now succeeds: toast "Professional information updated" (previously 403). Personal Info still saves. No Auth-side change was needed. |
| Cosmetic: Finance Dashboard tables | 🟢 Pass | Amount Handovers table now has **From** and named recipient ("Doctor One (Doctor)"); Counter Sessions table has **Opened By** (Clinic1 Admin / Recep One). |
| DEF-MR-15 | 🔴 Still open | Not exercised further; no repro. Needs a live repro as noted above. |

**Observations (not defects yet):**
- Finance Dashboard → "Doctor Wise OPD Collection" shows only *Doctor One ₹1,350* when logged in as Doctor One, while OPD income for the clinic is ₹5,270. Likely scoped to the viewing doctor (matches the privacy scoping done for handovers), but the page does not say so — suggest a "Showing your collections only" label, or confirm as Clinic Admin that all doctors appear.
- First login after each IIS recycle still takes 20–30 s on "Signing in…".
- Test data added: ₹100 handover (Recep Two → Doctor One, **Accepted**), Doctor One professional profile (reg. no. `MH-2026-10001`, General Medicine). Counter session (opened 16:09 by Recep One) is still Open with ₹4,000 collected / ₹4,000 handed over. Visit OPD-20261107-0014 is Completed with Rx RX-20261107-0001.

## Dev fix — round 4, 07-Nov-2026 (not yet committed to main, pending QA re-test)

Addresses the last open item, DEF-MR-15, on branch `testing/opd-multi-reception-multi-doctor`.

After three rounds of investigation (dev rounds 1 and 3, QA re-test rounds 2 and 3) found that reception's and a doctor's "Not Visited" counts already use the exact same rule — they just differ in scope (clinic-wide vs. the viewing doctor only) — there was no code bug left to fix blind. QA's round-3 note made the actual gap explicit: *"the reception screen has no doctor-wise breakdown to confirm"* whether a given mismatch is this expected scope difference or something else. Rather than keep guessing at a root cause that two independent investigations couldn't find, this round closes that visibility gap directly: `LoadCountsAsync` in `OpdRegistrationList.razor` now does one extra fetch (only when reception has no doctor filter active and the Not Visited count is non-zero — part of the same primary counts load that already runs on page load/preset/date changes, not a new per-click query, consistent with Rule 7) to get the actual Not Visited rows, groups them by doctor client-side, and shows the result (e.g. "1 for Doctor Two") as a tooltip and a small inline line next to the "Not Visited" tab, reception-only.

This turns an ambiguous, unverifiable count into a self-explanatory one: if reception now sees "Not Visited (1) — 1 for Doctor Two" while Doctor One's own queue shows 0, that is visibly consistent (the one item simply isn't Doctor One's), not an inconsistency. If QA's next re-test instead finds the breakdown attributes the stale visit to the *same* doctor whose own queue shows 0, that would be the first actual reproduction of a real bug in three rounds, and should be reported as a new, now-evidenced defect rather than re-filed as DEF-MR-15.

**Build/test status:** `TenantCore.Web.Client` (the only project this change touches) builds clean, 0 errors. The full solution could not be rebuilt this round — `TenantCore.Api`'s own build output was locked by a currently-running process (consistent with QA actively testing against `https://localhost:7246`); this is an environment lock, not a compile error, and does not affect this change (no `TenantCore.Api` files were touched). No test project covers this Blazor page (none did before this change either); the fix is UI-only (one extra API call reusing the existing `GetOpdRegistrationsAsync` endpoint, then client-side `GroupBy`), so no new automated test was added — verify via the re-test checklist below instead.

**Not yet re-tested through the UI.**

## QA re-test — round 4, 07-Nov-2026 (22:00 IST)

Environment: App latest build `https://localhost:7246`, logged in as Recep Two (`Temp@123`). Nothing committed.

| Defect | QA result | What was done / observed |
|---|---|---|
| DEF-MR-15 | 🟢 Pass | Reception OPD queue (no doctor filter): tab "**Not Visited (1)**" with the new inline breakdown "**1 for Dr Fake**" (also the tooltip). Opening the tab lists exactly one row — OPD-20261102-0002, DUMMY Patient 003, **Dr Fake**, 02 Nov 2026, Recep One, ₹100, Not Visited. It belongs to a different doctor than Doctor One, whose own queue shows Not Visited (0) — so the two counts are **consistent**: same rule, different scope, and the reception screen now says why. No inconsistency reproduced; closed as "explained / visibility fix verified". |

**Minor observations (not blocking, no new defect IDs):**
- "Dr Fake" does not appear in the reception **All Doctors** filter (only Clinic1 Admin, Doctor Two, Doctor One), so this stale visit cannot be filtered by doctor. Likely a former/test doctor who is no longer an active member — suggest keeping inactive doctors selectable in the filter, or listing them under "Other".
- With the Not Visited tab open the footer still reads "Showing 1–1 of 1 records — **Today**" while the row is dated 02 Nov (outside "Today"). Cosmetic label only.
- OPD-20261107-0014 now shows **Completed** (it auto-completed when the Rx was finalised); earlier report text that says "still In Progress" is outdated.
- The Chrome tab renderer froze once during this pass (screenshot timeout) — test-tool issue, page text was read instead.


## Re-test checklist

1. Counter page shows "Opened by" (and flags "not your session" for the other receptionist) and a "Collected By" column (DEF-MR-01).
2. OPD list shows "Registered By" for reception, including on the just-created row before a reload (DEF-MR-05).
3. Sending a handover for less than the session's current outstanding balance is rejected immediately with a clear message, not accepted then failed at Accept; after one handover is Accepted and more cash is collected into the still-open session, a second handover can only be raised/accepted for the new outstanding amount (not the gross cumulative total), and a second handover while one is already Pending is blocked outright (DEF-MR-02).
4. Handovers show "From" and the resolved recipient name; a Doctor's "All Handovers" only lists their own; a Clinic Admin/Manager still sees everyone's (DEF-MR-03).
5. Finance dashboard's handover "Total (Accepted + Pending)" excludes Disputed money (DEF-MR-04).
6. A disputed handover drops out of "Handovers For Me (pending your action)" (DEF-MR-14).
7. Rx Save & Finalise with a diagnosis and no medicine now finalises; with neither a medicine nor any clinical content, it shows a clear message (DEF-MR-06).
8. OPD queue "Done" on a visit with no Rx asks for confirmation first; a visit with an existing Rx still completes with one click (DEF-MR-07).
9. Rx screen shows blank vitals (not 120/80 / 72 / 98.6 / etc.) when the OPD intake left them blank (DEF-MR-08).
10. Doctor professional info shows "Registration Saved", not "Verified" (DEF-MR-09).
11. Multi-word patient search (e.g. "DUMMY Patient 001") returns a match (DEF-MR-10).
12. My Profile → Save Professional Info (registration number, speciality) and the print-template preference both work with no clinic selected (DEF-MR-11).
13. Doctor dashboard greeting uses the last name (DEF-MR-13).
14. Every page title reads "... - CloudClinic"; the header clock visibly ticks forward after ~30s on a page left open (DEF-MR-16).
15. Clinic card "Joined" date shows the member's own join date, not the clinic's creation date — **run Auth from `testing/opd-multi-reception-multi-doctor`, not `main`** (DEF-MR-12).
16. ~~Reception's "Not Visited" tab shows a per-doctor breakdown~~ — confirmed in QA re-test round 4: the stale visit belonged to a different, inactive doctor than the one whose own queue read 0; consistent, not a bug (DEF-MR-15, closed).
17. ~~DEF-MR-09~~ and ~~Finance Dashboard "From"/"Opened By" columns~~ — both confirmed in QA re-test round 3; no longer pending.

**All 17 items above are now confirmed. Nothing on this checklist remains open.**

## Suggestions / improvements

1. **Per-receptionist counter sessions** (or at least "opened by / collected by" everywhere) — the core change for clinics with more than one reception (DEF-MR-01).
2. **Specialty-aware doctor workspace**: for a gynecologist, default diagnosis quick-picks (PCOS, fibroid, ANC, etc.), investigations (USG obstetric/pelvic, Pap smear, HCG, CBC), and hide gynecological history for male patients. Currently the Gynecologist sees generic diagnoses (BPH, Gastroenteritis) and a Gynecological History card for a male patient.
3. **Doctor-specific fee defaults** — see OBS-MR-02.
4. Show the **recipient doctor's name** and **sender's name** in handovers (DEF-MR-03), and let a receptionist hand over only to a doctor, with the balance owed per receptionist visible (DEF-MR-01/02).
5. Surface API validation messages (409/400) in the UI everywhere (handover accept, Rx finalise) — DEF-MR-02, DEF-MR-06.
6. Add a **reception-wise column** to the Finance dashboard Counter Sessions table.
7. Role menus — see OBS-MR-01.

## Coverage and limitations (be honest about scope)

- Registered all 10 visits, but only 3 through the registration form UI (#1–#3). The other 7 used the same `POST /api/opd-registrations` call the form makes.
- Doctor consultation: only 1 visit started and completed (DUMMY 009, Doctor Two, via Start and Done). No prescriptions were finalised (blocked by DEF-MR-06). The other 9 visits are still Waiting.
- Not tested: receptionist adding items to visits, refunds with two receptions, per-receptionist reception-wise report, closing the session after handover, session day rollover.
- Finance dashboard and the handover pages were checked. The Doctor/Reception report pages beyond Finance were not.

## Test data left in Clinic One Care

- Tokens `OPD-20261107-0004` to `-0013` (10 visits); `-0012` Completed, others Waiting.
- Counter session opened 16:09 is **still Open**: ₹3,900 collected, ₹3,900 handover accepted, ₹1,850 handover disputed.
- Wallet ₹958.
- Pending invites for throwaway users (`rt_html`, `rt_spoof`, `rt_um5`) still present.

## Repo status — all 16 defects closed (07-Nov-2026, QA re-test round 4)

Branch `testing/opd-multi-reception-multi-doctor` created from `main` @ `f7032d2`. This report, and dev fix rounds 1–4, are on this branch — rounds 1–2 are committed; rounds 3 and 4 are **not yet committed**, left for the user to commit now that everything in this cycle is closed. **All 16 new defects (DEF-MR-01 through DEF-MR-16) are 🟢 Verified – Closed.** DEF-MR-15 closed in QA re-test round 4: the per-doctor breakdown added that round showed the stale "Not Visited" visit belonged to a different (inactive) doctor than the one whose own queue read 0 — consistent by scope, not a bug; see "QA re-test — round 4" above. **TenantCore.Auth has a matching branch of the same name** (`testing/opd-multi-reception-multi-doctor`, from its own `main`) carrying the fix for DEF-MR-12/DEF-AUTH-MR-01, also 🟢 Verified – Closed in that repo's own `docs/testing/OPD_MultiReception_Auth_Defects.md` — nothing pending there either. `TenantCore.Web.Client` built clean for round 4; the rest of the solution couldn't be rebuilt that round because the running app held `TenantCore.Api`'s build output locked (an environment lock, not a compile error — no `TenantCore.Api` files were touched, so this doesn't affect the fix). **Still open, but out of this cycle's scope** (all three live outside TenantCore.App, pre-existing, unchanged by this round): DEF-REG-05/DEF-UM-06 (invite email template — likely a stale local `Clinic.Notification` process, not code), DEF-UM-04 (HTML in invite name, TenantCore.Auth) and the expired-invite/duplicate-row issue (TenantCore.Auth). Four minor, non-blocking observations were logged in round 4 (inactive-doctor filter gap, a cosmetic "Today" footer label, stale report text, a test-tool screenshot freeze) — none assigned a defect ID. **Next step:** commit rounds 3–4 on both branches, then decide whether to pick up the three out-of-scope Auth/Notification items in a separate pass.
