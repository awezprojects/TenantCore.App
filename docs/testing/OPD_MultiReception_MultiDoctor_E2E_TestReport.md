# OPD Multi-Reception / Multi-Doctor E2E Test & Defect Report

> **REPORT STATUS: 🟡 FIXED – PENDING QA — 07-Nov-2026.** Dev fix round 1 applied for 14 of the 16 new defects (all 🟡 Fixed – Pending QA). DEF-MR-12 needs a TenantCore.Auth API change (cross-repo, out of scope for this branch — see its row) and stays 🔴 Open. DEF-MR-15 stays 🔴 Open — could not confirm a root cause from code alone; needs live reproduction with exact timestamps. The 3 re-confirmed defects live in TenantCore.Auth/Clinic.Notification, not this repo — see "Re-confirmed earlier defects" for what was found. QA re-test is next.

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
| Fixed this round (🟡 pending QA) | 14 (2 High · 7 Medium · 5 Low) |
| Still open — needs a TenantCore.Auth change (cross-repo) | 1 (DEF-MR-12) |
| Still open — needs live reproduction | 1 (DEF-MR-15) |
| Still open — lives outside TenantCore.App | 3 (re-confirmed) |

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
| DEF-MR-01 | High | 🟡 Fixed – Pending QA | Counter session is clinic-wide, not per receptionist | Recep One opens a session and registers 5 visits. Sign in as Recep Two and open Counter. | Each receptionist has their own session and cash; handover/close applies only to their own money. At minimum the session shows "Opened by" and each collection shows "Collected by". | Recep Two sees Recep One's session ("Opened 16:09", ₹2,050, 5 patients) with live Handover and Close Session buttons. Recep Two never opened a session, yet their 5 visits (₹1,850) were added to Recep One's session. No "opened by"/"collected by" shown anywhere. No cash accountability — either receptionist can hand over or close the other's cash. | **Fix applied (the "at minimum" ask):** `CounterSession.OpenedByUserId` and `OpdPayment.ReceivedByUserId` already existed server-side but were never surfaced. Added `CollectedByUserId` to `SessionCollectionDto` (populated in `GetOpdCollectionsBySessionHandler` from the session payment's `ReceivedByUserId`) and wired `CounterSession.razor` to show "Opened by" on the session header (flagging "— not your session" when it isn't the viewer's) and a "Collected By" column on the Patient Collections table, resolving names from the clinic staff directory loaded once per page view. **Not fixed — deliberately out of scope:** true concurrent per-receptionist sessions (one open session per user instead of one per clinic) is a larger architectural change than a defect-fix round; tracked for a follow-up if the product decides to pursue it. | Dev – fixed, pending QA re-test |
| DEF-MR-02 | High | 🟡 Fixed – Pending QA | A partial handover can be created but never accepted; the doctor gets no error | Recep Two sends ₹1,850 (their own share) to Doctor Two while the session balance is ₹3,900. Doctor Two clicks Accept. | Either the partial amount is blocked at send time with a clear message, or per-receptionist handovers are supported; the API's error is always shown to the doctor. | Send returns 201 and the doctor's screen says "pending". Accept returns **409** (amount differs from the session balance) and the UI shows **no message** — the button just does nothing. The only way out is Dispute. The session page warns "pending handover no longer matches the session balance", but only after the send. | **Fix applied:** `CreateAmountHandoverHandler` now computes the session's live net balance (same formula `AcceptAmountHandoverHandler` already used) and rejects the create with a clear 400/409 message — `"Handover amount Rs. X does not match the counter session's current balance of Rs. Y..."` — before a mismatched handover can ever be sent, closing off the scenario this defect describes. Added `CreateAmountHandoverHandlerTests.Handle_WhenAmountDoesNotMatchSessionBalance_ThrowsInvalidOperationException`. **On investigation, the second half ("UI shows no message") was already fixed in the codebase** — `AmountHandovers.razor`'s `AcceptAsync` already calls `Toast.ShowError(response.Message ?? ...)`, and `ApiResponseReader`/`AmountHandoverApiClient` already extract the ProblemDetails `detail` text for a 409 (a comment in `ApiResponseReader.cs` describes fixing exactly this class of "blank toast" bug). Re-test should confirm the toast renders live. | Dev – fixed, pending QA re-test |
| DEF-MR-03 | Medium | 🟡 Fixed – Pending QA | Handover screens do not show who sent or who should receive | View the Handovers table as a receptionist or as a doctor. | "From (receptionist)" and the named recipient doctor are both visible; a doctor's "All Handovers" only lists handovers addressed to them. | Table has Amount / Sent to role / Status / Sent at only — no "From", and the recipient is only the role "Doctor" (indistinguishable with 3 doctors). Doctor One also sees the ₹1,850 handover sent to Doctor Two in "All Handovers". | **Fix applied:** `AmountHandoverDto`/`AmountHandoverSummaryDto` already carried `HandedOverByUserId` and `HandedOverToUserId` — only the UI never showed them. `AmountHandovers.razor` now loads the clinic staff directory once per page view and adds a "From" column plus a resolved recipient name next to the role, on both "Handovers For Me" and "All Handovers". "All Handovers" is now scoped to handovers the viewer sent or received unless they are a Clinic Admin/Manager (`_canSeeAllHandovers`) — a Doctor or Receptionist no longer sees another doctor's handovers. | Dev – fixed, pending QA re-test |
| DEF-MR-04 | Medium | 🟡 Fixed – Pending QA | Finance dashboard handover total includes disputed money | View Finance Dashboard → Amount Handovers after one accepted (₹5,170) and one disputed (₹1,850) handover. | Disputed amounts excluded from the total, or shown as a separate line. | "Accepted ₹5,170 + Disputed ₹1,850 → TOTAL ₹7,020", higher than total income (₹5,170). | **Fix applied:** `FinanceDashboard.razor`'s "Total" card summed every handover regardless of status (`_handovers.Sum(h => h.Amount)`). Changed to `_handoverTotal`, which excludes `Disputed` (disputed cash may never actually change hands, so it shouldn't inflate a total compared against income) and relabelled the card "Total (Accepted + Pending)" so what it means is explicit. Disputed still has its own card, unchanged. | Dev – fixed, pending QA re-test |
| DEF-MR-05 | Medium | 🟡 Fixed – Pending QA | OPD registration does not record the receptionist | Register an OPD visit via UI or API, inspect the create response and the OPD list. | Create response/list includes `createdByUserId`/`Name`. | No created-by field anywhere; reception-wise reporting and per-receptionist counter/audit cannot be derived. | **Fix applied, via the existing payment record rather than a new column:** `CreateOpdRegistrationHandler` already calls `OpdPayment.AcceptVisitFee(request.ReceivedByUserId, ...)` for a prepaid visit, which sets `OpdPayment.ReceivedByUserId` — this was simply never surfaced. Added `ReceivedByUserId` to `OpdRegistrationDto`, populated in `OpdRegistrationTranslator.ToDto` from the payment; the create handler now returns the newly-accepted payment in its response instead of `null`, so even the just-created row (before any reload) carries it. Added a "Registered By" column to the OPD list for reception users, resolved from the clinic staff directory. Stays blank when prepaid is disabled (nothing collected yet, so no receiver to show) — covered by `CreateOpdRegistrationHandlerTests.Handle_PrepaidDisabled_LeavesPaymentPending`. | Dev – fixed, pending QA re-test |
| DEF-MR-06 | Medium | 🟡 Fixed – Pending QA | Prescription "Save & Finalise" fails silently | Doctor starts a visit, clicks Rx, adds a diagnosis only (no medicine), clicks Save & Finalise. | Either the visit finalises without a medicine (counselling, scan-only or follow-up gynecology visit), or a clear inline/toast error is shown. | `POST /api/prescriptions` returns 400 `"At least one medicine is required."` but the screen shows nothing — no toast, no inline error. A consultation with no medicine cannot be finalised at all today. | **Fix applied, by relaxing the rule rather than only surfacing its error:** `CreatePrescriptionCommandValidator`/`UpdatePrescriptionCommandValidator` now require a medicine *only* when there is no other clinical content — a non-empty diagnosis, an investigation, or a note. A counselling-only, scan-only or follow-up visit with a diagnosis can now finalise with zero medicines; a visit with genuinely nothing recorded still gets a clear message: "Add at least one medicine, or record a diagnosis, investigation or note for this visit." Added 4 new validator tests per command covering empty-items-with/without clinical content. On investigation, `PrescriptionForm.razor`'s existing error handling (`Toast.ShowError(r.Message ?? ...)` in `SaveAsync`) already fires on any Create/Update failure and `PrescriptionApiClient` already parses the ProblemDetails `detail`/`errors` fields correctly — no separate UI bug found; re-test should confirm with the relaxed rule in place. | Dev – fixed, pending QA re-test |
| DEF-MR-07 | Medium | 🟡 Fixed – Pending QA | "Done" completes a visit with no prescription | From the OPD queue, click **Done** on a visit that was never taken to Rx. | Same completion rule as Save & Finalise (medicine required), or an explicit, separate "complete without Rx" action with its own confirmation. | The queue's **Done** button marks the visit Completed with no Rx at all, even though Save & Finalise requires a medicine — the two completion paths enforce different rules. | **Fix applied, as the explicit-confirmation option** (forcing the same hard rule as Rx finalise would make "Done" unable to close a visit the doctor deliberately skipped Rx for, which is also a real, intended use of that button): `OpdRegistrationList.razor`'s Done button now checks whether a prescription exists for the visit first; if not, it opens a `ConfirmDialog` ("No prescription was recorded... Mark this visit as completed anyway?", per ADR-007) before completing. A visit that already has a prescription completes immediately, with no extra click. | Dev – fixed, pending QA re-test |
| DEF-MR-08 | Medium | 🟡 Fixed – Pending QA | Rx screen displays default vitals that were never recorded | Reception leaves vitals blank at registration (OPD list shows "—"). Doctor opens Rx screen for that visit. | Blanks or explicit placeholders, matching the OPD list. | Rx screen shows BP 120/80, Pulse 72, Temp 98.6, Weight 70, SpO₂ 98, RR 16, Sugar 95 as if actually measured — these could be saved or printed as real readings. | **Root cause confirmed exactly as suspected:** `PrescriptionForm.razor`'s `ApplyDefaultVitals()` deliberately pre-filled BP/Pulse/SpO2 with a "typical" value whenever the OPD intake left them blank, on the reasoning that those three are "routinely checked at every visit" — true in general, but not for this clinic's test data, and not something the UI should assume on the doctor's behalf. **Fix applied:** removed the call and the method entirely; every vital now shows exactly what the OPD intake recorded, blank stays blank. | Dev – fixed, pending QA re-test |
| DEF-MR-09 | Medium | 🟡 Fixed – Pending QA | Doctor professional info reports "Registration Verified" instantly | Doctor Two types registration number `MH-2026-20002` and saves. | Labelled "Saved" unless a real verification check has run. | Any typed registration number is marked "✔ Registration Verified" immediately — no verification actually occurs. | **Fix applied:** confirmed there is no verification check anywhere in `DoctorProfileController`/`UpsertDoctorProfileCommand` — the badge only ever confirmed the profile row was saved. Relabelled "Registration Verified" → "Registration Saved" in `DoctorProfile.razor`. | Dev – fixed, pending QA re-test |
| DEF-MR-10 | Low | 🟡 Fixed – Pending QA | Patient search misses full names | Type `DUMMY Patient 001` in New OPD Registration patient search. | Matches. | "No patients found"; `DUMMY` alone works — multi-word search does not match. | **Fix applied:** `PatientRepository.GetPagedAsync` checked the whole search string against `FirstName`/`LastName`/`PhoneNumber` individually with `Contains()` — a query spanning both fields (e.g. `FirstName="DUMMY"`, `LastName="Patient 001"`) could never match as one string. Now splits the query into words and requires each word to match somewhere across `FirstName`, `LastName` or `PhoneNumber` (AND across words, OR across fields) — `"DUMMY Patient 001"` now matches. | Dev – fixed, pending QA re-test |
| DEF-MR-11 | Low | 🟡 Fixed – Pending QA | Profile save fails when no clinic is selected | Open My Profile from the clinic portal (no clinic in context), click Save Professional Info. | A friendly validation message ("Select a clinic first"). | Raw error surfaced: "This endpoint requires the X-Application-Id header. Select a clinic before calling it." The same save works after entering a clinic. | **Root cause found, fixed at the source rather than papering over the message:** `DoctorProfileController` only inherits `ClinicControllerBase` to reuse `GetCurrentUserId()` — every action on it (registration number, speciality, qualifications, print template) is the doctor's own global profile, not clinic-scoped data, and none of them call `GetApplicationId()`. It was never added to `ClinicContextMiddleware`'s `ExemptControllerTypes`, unlike `ClinicOnboardingController`, which has the exact same shape and was already exempted for the same reason. **Fix applied:** added `DoctorProfileController` to that exemption list, so My Profile saves with no clinic selected at all now. Added `ClinicContextMiddlewareTests.InvokeAsync_DoctorProfileRouteWithoutHeader_PassesThrough`. | Dev – fixed, pending QA re-test |
| DEF-MR-12 | Low | 🔴 Open (cross-repo) | Clinic card "Joined" date is wrong for new members | Doctor Two joins the clinic on 07-Nov; view their own clinic card. | "Joined 07 Nov 2026" (their own join date). | Shows "Joined 29 Oct 2026" — the clinic's own creation date, not the member's. The admin's member list shows the correct individual dates, so the data exists. | **Root cause confirmed, fix is out of scope for this branch.** `ClinicLanding.razor`'s card does read `item.Clinic.CreatedDate` (`ClinicDashboardItemDto.CreatedDate`) exactly as suspected — that DTO has no per-user join date field at all. The admin member list is correct because it reads a *different* Auth endpoint/DTO (`ApplicationUserResponseDto.JoinedDate`, an admin-only roster). Fixing this needs **TenantCore.Auth** to add a per-user join date to whichever endpoint backs `GetClinicDashboardQuery`/`ClinicDashboardItemDto` (the current-user-scoped "my clinics" list) — a cross-repo change this branch's scope doesn't cover (workspace rule: don't touch multiple repos in one task unless asked to coordinate). Left **Open**; needs a coordinated TenantCore.Auth + TenantCore.App change. | Dev – needs TenantCore.Auth change, not started |
| DEF-MR-13 | Low | 🟡 Fixed – Pending QA | Greeting uses first name with "Dr." | Doctor One or Doctor Two views their dashboard greeting. | Full or last name, e.g. "Good afternoon, Dr. Sharma". | "Good afternoon, Dr. Doctor" for both doctors — first name only, which reads oddly with the honorific. | **Fix applied:** `Dashboard.razor` built the greeting from `fullName.Split(' ').FirstOrDefault()`. Changed to use the last name (falling back to the full name for a single-word name), so it now reads "Good afternoon, Dr. One" / "Dr. Two" for these test accounts. | Dev – fixed, pending QA re-test |
| DEF-MR-14 | Low | 🟡 Fixed – Pending QA | Disputed handover stays in "Handovers For Me (pending your action)" | Dispute a handover, then revisit the doctor's "pending your action" list. | Removed from that list once disputed (no action is actually possible on it). | Still listed with no available actions — the list is misleading about what needs attention. | **Fix applied:** `AmountHandovers.razor`'s `_forMe` filter matched on recipient only, with no status check at all — an Accepted or Disputed handover addressed to the viewer stayed listed under "pending your action" indefinitely. Added `h.Status == HandoverStatus.Pending` to the filter. | Dev – fixed, pending QA re-test |
| DEF-MR-15 | Low | 🔴 Open | "Not Visited" count inconsistent | Compare "Not Visited" count on reception's OPD queue vs the doctor's view for the same visits. | Same count on both screens. | Reception OPD queue showed "Not Visited (1)" while all 3 visits were Waiting and none cancelled; the doctor view showed (0) for the same data. | **Investigated, not fixed — no confident root cause from code alone.** Reception and the doctor both load `/opd` (`OpdRegistrationList.razor`) with the *same* "Not Visited" predicate (`Status == Pending && RegistrationDate.Date < today`, identical client badge logic and server repository filter in `OpdRegistrationRepository.GetPagedAsync`) — so this isn't two different rules. The only structural difference is scope: reception's count query passes no doctor filter (clinic-wide), while a doctor's own query is scoped to `doctorUserId == themselves`. If the "1" not-visited visit belonged to a doctor other than the one viewing, that's expected scope, not a bug — but the report describes comparing the same data. Re-test should capture the exact token, both users' doctor filter state, and server vs. client clock at the time, so the next round can actually reproduce it rather than guess. | Dev – needs reproduction, not started |
| DEF-MR-16 | Low | 🟡 Fixed – Pending QA | Inconsistent product naming / stale clock | Browse multiple pages in one session; leave a page open for several minutes. | One consistent product name across all page titles; header clock updates live. | Page titles vary (CloudClinic / ClinicCore / TenantCore); header time did not update while the page stayed open (showed 04:08 pm while content showed 04:11 pm). | **Fix applied, both halves:** (1) 19 pages used `<PageTitle>` text with " - TenantCore" or " — ClinicCore" instead of the dominant "CloudClinic" (Login, Dashboard, OPD, etc.) — standardised all of them to "CloudClinic". (2) `AuthorizedLayout.razor`'s `_currentTime` field was set once at construction and never updated again — confirmed exactly the suspected root cause (no periodic timer existed at all). Added a 30-second `System.Threading.Timer` that refreshes `_currentTime` and calls `StateHasChanged`, disposed in the component's existing `Dispose()`. | Dev – fixed, pending QA re-test |

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

## Re-test checklist

1. Counter page shows "Opened by" (and flags "not your session" for the other receptionist) and a "Collected By" column (DEF-MR-01).
2. OPD list shows "Registered By" for reception, including on the just-created row before a reload (DEF-MR-05).
3. Sending a handover for less than the session's current balance is rejected immediately with a clear message, not accepted then failed at Accept (DEF-MR-02).
4. Handovers show "From" and the resolved recipient name; a Doctor's "All Handovers" only lists their own; a Clinic Admin/Manager still sees everyone's (DEF-MR-03).
5. Finance dashboard's handover "Total (Accepted + Pending)" excludes Disputed money (DEF-MR-04).
6. A disputed handover drops out of "Handovers For Me (pending your action)" (DEF-MR-14).
7. Rx Save & Finalise with a diagnosis and no medicine now finalises; with neither a medicine nor any clinical content, it shows a clear message (DEF-MR-06).
8. OPD queue "Done" on a visit with no Rx asks for confirmation first; a visit with an existing Rx still completes with one click (DEF-MR-07).
9. Rx screen shows blank vitals (not 120/80 / 72 / 98.6 / etc.) when the OPD intake left them blank (DEF-MR-08).
10. Doctor professional info shows "Registration Saved", not "Verified" (DEF-MR-09).
11. Multi-word patient search (e.g. "DUMMY Patient 001") returns a match (DEF-MR-10).
12. My Profile → Save Professional Info works with no clinic selected (DEF-MR-11).
13. Doctor dashboard greeting uses the last name (DEF-MR-13).
14. Every page title reads "... - CloudClinic"; the header clock visibly ticks forward after ~30s on a page left open (DEF-MR-16).
15. **Not covered by this round — re-test only to confirm still open:** DEF-MR-12 (clinic card Joined date) and DEF-MR-15 (Not Visited count); capture exact repro steps (token, both users' doctor-filter state, timestamps) for DEF-MR-15 if it reproduces.

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

## Repo status

Branch `testing/opd-multi-reception-multi-doctor` created from `main` @ `f7032d2`. This report, and dev fix round 1 (14 of 16 new defects), are on this branch, uncommitted pending QA re-test — same uncommitted-until-closed convention as the prior two reports in this folder. Per the status workflow above, this report is updated in place after each dev-fix round (defects move 🔴 Open → 🟡 Fixed – Pending QA → 🟢 Verified – Closed) rather than superseded by a new file. Full solution build and all five test projects pass (see "Dev fix — round 1" above). **Next step:** QA re-test against this round's changes using the checklist above, then either close out DEF-MR-01…11/13/14/16 or report what's still wrong; DEF-MR-12 and DEF-MR-15 need further work (cross-repo change and live reproduction respectively) before they can be fixed.
