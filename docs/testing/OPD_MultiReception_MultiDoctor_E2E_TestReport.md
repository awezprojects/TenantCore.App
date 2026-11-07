# OPD Multi-Reception / Multi-Doctor E2E Test & Defect Report

> **REPORT STATUS: 🔴 OPEN — 07-Nov-2026.** 16 new defects (2 High · 7 Medium · 7 Low), all 🔴 Open, plus 3 earlier defects re-confirmed open. Awaiting dev fix round 1.

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
| DEF-MR-01 | High | 🔴 Open | Counter session is clinic-wide, not per receptionist | Recep One opens a session and registers 5 visits. Sign in as Recep Two and open Counter. | Each receptionist has their own session and cash; handover/close applies only to their own money. At minimum the session shows "Opened by" and each collection shows "Collected by". | Recep Two sees Recep One's session ("Opened 16:09", ₹2,050, 5 patients) with live Handover and Close Session buttons. Recep Two never opened a session, yet their 5 visits (₹1,850) were added to Recep One's session. No "opened by"/"collected by" shown anywhere. No cash accountability — either receptionist can hand over or close the other's cash. | Suggested fix: scope the active counter session query/open-session command by the acting receptionist's user id (not just `ApplicationId`), and store/display `OpenedByUserId`/`Name` on the session and `CollectedByUserId`/`Name` on each payment/particular collection. This is the core architectural gap for a multi-reception clinic — see also DEF-MR-02 and DEF-MR-05. | Dev – pending fix |
| DEF-MR-02 | High | 🔴 Open | A partial handover can be created but never accepted; the doctor gets no error | Recep Two sends ₹1,850 (their own share) to Doctor Two while the session balance is ₹3,900. Doctor Two clicks Accept. | Either the partial amount is blocked at send time with a clear message, or per-receptionist handovers are supported; the API's error is always shown to the doctor. | Send returns 201 and the doctor's screen says "pending". Accept returns **409** (amount differs from the session balance) and the UI shows **no message** — the button just does nothing. The only way out is Dispute. The session page warns "pending handover no longer matches the session balance", but only after the send. | Suggested fix: validate the handover amount against the *current* session balance at send time (`CreateAmountHandoverHandler`) and return a validation error instead of letting it reach Accept; on the UI, surface the 409 `ProblemDetails.Detail` from `AcceptAmountHandoverHandler` on the doctor's Accept button instead of silently no-opping. Properly fixing this depends on DEF-MR-01 (per-receptionist sessions). | Dev – pending fix |
| DEF-MR-03 | Medium | 🔴 Open | Handover screens do not show who sent or who should receive | View the Handovers table as a receptionist or as a doctor. | "From (receptionist)" and the named recipient doctor are both visible; a doctor's "All Handovers" only lists handovers addressed to them. | Table has Amount / Sent to role / Status / Sent at only — no "From", and the recipient is only the role "Doctor" (indistinguishable with 3 doctors). Doctor One also sees the ₹1,850 handover sent to Doctor Two in "All Handovers". | Suggested fix: add `SentByUserId`/`Name` and `RecipientUserId`/`Name` to the handover DTO and list/detail views; filter "All Handovers" for a doctor role by `RecipientUserId == currentUserId` (or an explicit admin-only "all" view). | Dev – pending fix |
| DEF-MR-04 | Medium | 🔴 Open | Finance dashboard handover total includes disputed money | View Finance Dashboard → Amount Handovers after one accepted (₹5,170) and one disputed (₹1,850) handover. | Disputed amounts excluded from the total, or shown as a separate line. | "Accepted ₹5,170 + Disputed ₹1,850 → TOTAL ₹7,020", higher than total income (₹5,170). | Suggested fix: in the dashboard aggregate, sum only `Accepted` handovers into the headline total, and surface `Disputed`/`Pending` as their own labelled rows instead of folding them into the sum. | Dev – pending fix |
| DEF-MR-05 | Medium | 🔴 Open | OPD registration does not record the receptionist | Register an OPD visit via UI or API, inspect the create response and the OPD list. | Create response/list includes `createdByUserId`/`Name`. | No created-by field anywhere; reception-wise reporting and per-receptionist counter/audit cannot be derived. | Suggested fix: add `CreatedByUserId` (already on `AuditableEntity`, just needs surfacing) to `OpdRegistrationDto` and the OPD list columns; populate from the acting user in `CreateOpdRegistrationHandler`. | Dev – pending fix |
| DEF-MR-06 | Medium | 🔴 Open | Prescription "Save & Finalise" fails silently | Doctor starts a visit, clicks Rx, adds a diagnosis only (no medicine), clicks Save & Finalise. | Either the visit finalises without a medicine (counselling, scan-only or follow-up gynecology visit), or a clear inline/toast error is shown. | `POST /api/prescriptions` returns 400 `"At least one medicine is required."` but the screen shows nothing — no toast, no inline error. A consultation with no medicine cannot be finalised at all today. | Suggested fix: on the UI, surface the 400 validation message from the API on the Rx screen (toast or inline); separately, consider relaxing `CreatePrescriptionCommandValidator`'s "at least one medicine" rule for visit types that legitimately have none (counselling/scan-only/follow-up). | Dev – pending fix |
| DEF-MR-07 | Medium | 🔴 Open | "Done" completes a visit with no prescription | From the OPD queue, click **Done** on a visit that was never taken to Rx. | Same completion rule as Save & Finalise (medicine required), or an explicit, separate "complete without Rx" action with its own confirmation. | The queue's **Done** button marks the visit Completed with no Rx at all, even though Save & Finalise requires a medicine — the two completion paths enforce different rules. | Suggested fix: route the queue's "Done" action through the same completion rule/validator as Rx finalise (or explicitly document and confirm it as a separate "complete without consultation" action, per ADR-007's confirm-dialog rule for destructive/irreversible actions). | Dev – pending fix |
| DEF-MR-08 | Medium | 🔴 Open | Rx screen displays default vitals that were never recorded | Reception leaves vitals blank at registration (OPD list shows "—"). Doctor opens Rx screen for that visit. | Blanks or explicit placeholders, matching the OPD list. | Rx screen shows BP 120/80, Pulse 72, Temp 98.6, Weight 70, SpO₂ 98, RR 16, Sugar 95 as if actually measured — these could be saved or printed as real readings. | Suggested fix: the Rx screen's vitals form is defaulting unset/null vitals to these hardcoded values; change the defaults to blank/placeholder text and only populate from `OpdRegistration.Vitals` when actually present. | Dev – pending fix |
| DEF-MR-09 | Medium | 🔴 Open | Doctor professional info reports "Registration Verified" instantly | Doctor Two types registration number `MH-2026-20002` and saves. | Labelled "Saved" unless a real verification check has run. | Any typed registration number is marked "✔ Registration Verified" immediately — no verification actually occurs. | Suggested fix: rename the label/icon to "Saved" until a real verification integration exists; do not claim "Verified" for an unvalidated free-text field. | Dev – pending fix |
| DEF-MR-10 | Low | 🔴 Open | Patient search misses full names | Type `DUMMY Patient 001` in New OPD Registration patient search. | Matches. | "No patients found"; `DUMMY` alone works — multi-word search does not match. | Suggested fix: the search likely does an exact/prefix match on a single term; switch to splitting the query on whitespace and matching all tokens (AND), or a `CONTAINS`-style match against the concatenated name. | Dev – pending fix |
| DEF-MR-11 | Low | 🔴 Open | Profile save fails when no clinic is selected | Open My Profile from the clinic portal (no clinic in context), click Save Professional Info. | A friendly validation message ("Select a clinic first"). | Raw error surfaced: "This endpoint requires the X-Application-Id header. Select a clinic before calling it." The same save works after entering a clinic. | Suggested fix: on the Profile page, either disable the Professional Info save when no clinic is selected, or catch the specific `ClinicContextMiddleware` error and show a friendly message instead of the raw text. | Dev – pending fix |
| DEF-MR-12 | Low | 🔴 Open | Clinic card "Joined" date is wrong for new members | Doctor Two joins the clinic on 07-Nov; view their own clinic card. | "Joined 07 Nov 2026" (their own join date). | Shows "Joined 29 Oct 2026" — the clinic's own creation date, not the member's. The admin's member list shows the correct individual dates, so the data exists. | Suggested fix: the clinic card is likely binding to `Application.CreatedAt` instead of the member/`ApplicationUser` join-date field the admin member list already reads correctly; point it at the same field. | Dev – pending fix |
| DEF-MR-13 | Low | 🔴 Open | Greeting uses first name with "Dr." | Doctor One or Doctor Two views their dashboard greeting. | Full or last name, e.g. "Good afternoon, Dr. Sharma". | "Good afternoon, Dr. Doctor" for both doctors — first name only, which reads oddly with the honorific. | Suggested fix: build the greeting from `LastName` (or `FirstName LastName`) instead of `FirstName` when the user has a Doctor role. | Dev – pending fix |
| DEF-MR-14 | Low | 🔴 Open | Disputed handover stays in "Handovers For Me (pending your action)" | Dispute a handover, then revisit the doctor's "pending your action" list. | Removed from that list once disputed (no action is actually possible on it). | Still listed with no available actions — the list is misleading about what needs attention. | Suggested fix: filter the "pending your action" list to `Status == Pending` only; show disputed items in a separate "Disputed" section/tab. | Dev – pending fix |
| DEF-MR-15 | Low | 🔴 Open | "Not Visited" count inconsistent | Compare "Not Visited" count on reception's OPD queue vs the doctor's view for the same visits. | Same count on both screens. | Reception OPD queue showed "Not Visited (1)" while all 3 visits were Waiting and none cancelled; the doctor view showed (0) for the same data. | Suggested fix: the two screens appear to compute "Not Visited" from different status sets (likely one includes a stale/cancelled-adjacent state the other excludes); align both to the same status predicate. | Dev – pending fix |
| DEF-MR-16 | Low | 🔴 Open | Inconsistent product naming / stale clock | Browse multiple pages in one session; leave a page open for several minutes. | One consistent product name across all page titles; header clock updates live. | Page titles vary (CloudClinic / ClinicCore / TenantCore); header time did not update while the page stayed open (showed 04:08 pm while content showed 04:11 pm). | Suggested fix: standardise `<title>`/branding strings to one product name across layouts; check the header clock component for a missing timer/re-render trigger (likely a one-shot `DateTime.Now` read instead of a periodic timer). | Dev – pending fix |

### Re-confirmed earlier defects (still open)

| Defect ID | Status | Note |
|---|---|---|
| DEF-REG-05 / DEF-UM-06 | 🔴 Open (re-confirmed) | Invitation emails still render raw template text (`Template: UserInvited … AcceptInvitationUrl: …`). Every invite sent in this session looked like this. |
| DEF-UM-04 | 🔴 Open (re-confirmed) | Invite name containing HTML (`<img src=x onerror=alert(1)>Eve Html`) is still stored and listed. It displays escaped here, but the API should reject it. |
| *(unassigned)* | 🔴 Open (re-confirmed) | Invitation list shows "0 days left" for invites sent 6–7 days earlier (expired); Resend still says "valid for 7 days" — should show "Expired". The same person (Doctor One) appears twice (Accepted and Pending). |

### Observations

| ID | Observation |
|---|---|
| OBS-MR-01 | Doctor role sees Patient Reg., Counter, Expenses and Handovers menu items. Confirm this menu set is intended for the Doctor role. |
| OBS-MR-02 | `GET /doctor-fee-configs/by-doctor/{id}` returned 204 for Doctor Two, so reception types the fee by hand each time. Consider prompting the admin to set a fee per doctor at onboarding. |

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

Branch `testing/opd-multi-reception-multi-doctor` created from `main` @ `f7032d2` and this report committed to `docs/testing/`. Pending: dev fix round 1 for DEF-MR-01 through DEF-MR-16 and the 3 re-confirmed defects, followed by QA re-test. Per the status workflow above, this report is updated in place after each dev-fix round (defects move 🔴 Open → 🟡 Fixed – Pending QA → 🟢 Verified – Closed) rather than superseded by a new file — see the prior two reports in this folder for the pattern.
