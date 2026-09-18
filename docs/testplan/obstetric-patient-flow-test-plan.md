# Obstetric Patient Flow - End-to-End Test Plan

> **Application:** TenantCore.App (CloudClinic)
> **Test type:** Manual end-to-end functional testing + edge cases + penetration testing
> **Environment:** Shifa Clinic (obstetric / gynecology clinic)
> **Status:** Ready for testers

---

## 1. Document Control

| Field | Value |
|---|---|
| Document version | 1.0 |
| Prepared for | QA / testers |
| Scope | Obstetric patient lifecycle: OPD booking (reception) -> prescription (doctor) -> payments / discount / refund (reception + doctor) -> amount handover -> session close |
| Out of scope | Patient registration (create patient flow), IPD admission, USG template admin, medicine catalogue admin, subscription / billing admin |

---

## 2. Scope & Objectives

This plan verifies the **complete obstetric patient journey** from two perspectives:

1. **Reception** - open counter session, create OPD registrations for obstetric patients against the obstetric doctor, add service items (injection, IV, etc.), accept payments, process refunds, create amount handover, close session.
2. **Doctor (Obstetrician / Gynecologist)** - pick the appointment, record vitals / history / LMP / diagnosis / investigations / medicines (all form types + bundles), submit the prescription, print it, and later apply discounts.

Objectives:
- Verify the happy path works end to end for 3+ new obstetric patients.
- Verify all obstetric-specific fields (LMP, EDD by LMP, EDD by USG, the 7 history sections, pregnancy tenure, overdue EDD).
- Verify prescription immutability after submit (only investigations can still be added).
- Verify vitals placeholder behaviour (defaults shown but not persisted until explicitly picked).
- Verify discount / service-item / refund / handover / session-close rules.
- Verify edge cases, negative paths, and security (auth, authorization, tenant isolation, input validation, IDOR).

---

## 3. Test Environment & Credentials

| Role | Email | Password | Notes |
|---|---|---|---|
| Reception | `awezlala+1@gmail.com` | `Temp@123` | Books OPD, collects payments, opens / closes counter session |
| Doctor (Obstetric) | `awez.vtt+1@gmail.com` | `Temp@123` | The obstetric doctor - MUST be selected during OPD booking |

> The doctor is an **obstetrician (Gynecologist)**. When the receptionist books the OPD, they must select **this exact doctor** from the doctor list so the doctor can see the patient on their prescription queue. If a different doctor is selected, the obstetric doctor will not see the appointment.

Base URLs (fill in per environment):
- App: `https://localhost:7246`
- Swagger (dev only): `https://localhost:7246/swagger`

---

## 4. Roles & Responsibilities

| Role | Can do | Cannot do |
|---|---|---|
| Reception | Open / close counter session; create / delete OPD; add service items; accept payments; process refunds; create amount handover | Prescribe; accept / dispute handover |
| Doctor (Obstetric) | Create / submit prescription; set LMP / EDD / history; add investigations; apply discount; accept / dispute handover; collect from reception | Create / delete OPD; open / close session; accept payments |

---

## 5. Entry / Exit Criteria

**Entry:** app is running, clinic (Shifa) is active with a valid subscription, both accounts can log in, the doctor list contains the obstetric doctor, the medicine catalogue has test medicines (or testers add them), and the default vital presets exist.

**Exit:** all P0 / P1 cases pass; every defect is logged with steps / actual / expected; the final traceability matrix (Section 13) is filled with the actual IDs and numbers observed.

---

## 6. Test Data (create these patients first)

> Patient **registration** is out of scope (already tested). Create the patients below using the existing working patient-registration flow, then note their **MR number** in the traceability matrix (Section 13).

| # | Patient name (suggested) | Gender | Phone | Purpose |
|---|---|---|---|---|
| P1 | `Obstetric Test A - Normal` | Female | 9000000001 | Normal obstetric journey, full prescription + bundle + print |
| P2 | `Obstetric Test B - Overdue EDD` | Female | 9000000002 | LMP in the far past -> overdue EDD, tenure close, blocked new LMP |
| P3 | `Obstetric Test C - Refund` | Female | 9000000003 | Cancel appointment, refund, handover, session-close negative tests |
| P4 | `Obstetric Test D - Repeat Visit` | Female | 9000000004 | Second visit for history / medicine carry-forward |

### 6.1 Reference values to use

| Field | Value to use |
|---|---|
| LMP (normal, P1 / P4) | Today (-) 56 days (approx 8 weeks) -> EDD = LMP + 280 days |
| LMP (overdue, P2) | Today (-) 300 days (EDD already passed) |
| EDD by USG (P1) | A date different from EDD-by-LMP (e.g. EDD-by-LMP (-) 3 days) |
| Vitals (P1) | BP 120/80, Pulse 78, Temp 98.6 F, Weight 62 kg, SpO2 99, RR 16, Sugar 92 |
| Doctor to select | **The obstetric doctor** (`awez.vtt+1@gmail.com`) for every OPD |

### 6.2 Preconditions checklist (do once, before Phase A)

- [ ] Reception can log in (`awezlala+1@gmail.com`).
- [ ] Doctor can log in (`awez.vtt+1@gmail.com`) and lands on the doctor dashboard.
- [ ] The obstetric doctor appears in the OPD doctor dropdown.
- [ ] A **Doctor Fee Config** exists for the obstetric doctor (Admin > Doctor Fee Configs) so the fee auto-fills.
- [ ] At least these **particulars** exist (Admin > Particulars): `Injection`, `IV`, `Dressing`, `ECG`, `Sugar Test`.
- [ ] At least one **medicine bundle** exists (Medicines > Medicine Bundles), e.g. `ANC Bundle`.
- [ ] Medicines exist covering form types: Tablet, Capsule, Syrup, Injection, Powder, Drops, Tube, Other (add if missing).
- [ ] Default vital presets are present (Vitals Lookup page shows the 7 vitals).
- [ ] Clinic feature flags: note the state of **Prepaid OPD Fee Collection** and **Billing** (both affect the money flow).

---

## 7. Phase A - Reception: Session + OPD Registration

> Login as **Reception** first. All cases in this phase are on the reception account.

### A. Counter session

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| A-01 | No active session | Open `Counter Session` page | Page loads; no active session; `Open Session` button visible; stats hidden | P0 |
| A-02 | No active session | Click `Open Session` | Success toast; session opens for today; badge `Session Open`; stats show Collected / Paid Expenses / Net Balance (all 0) | P0 |
| A-03 | Session already open | Reload `Counter Session` page | The same open session is shown (no duplicate); `Open Session` button is hidden | P0 |
| A-04 | Session already open | Try to open a second session (refresh + click if the button reappears, or call `POST /api/counter-sessions/open` twice) | Second open is rejected with a `409` / error toast: `A counter session is already open.` | P1 |
| A-05 | Session open, no handover | Click the close-session action | Session closes successfully (baseline: with **no pending handover** a close is allowed) | P0 |
| A-06 | After A-05 | Re-open the session for the rest of the test run | Session opens again; use this session for all following phases | P0 |

### B. OPD registration (repeat for P1, P2, P3, P4)

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| A-07 | Session open | `OPD Management` > `+ New OPD Entry` | New OPD dialog opens | P0 |
| A-08 | Dialog open | Search and select patient P1 | Patient resolves correctly; name / MR / phone shown | P0 |
| A-09 | Patient selected | Open the doctor dropdown | **The obstetric doctor is listed** and selectable | P0 |
| A-10 | Doctor selected | Select the obstetric doctor | Fee field **auto-fills** from Doctor Fee Config and is read-only | P0 |
| A-11 | Doctor selected | Observe the vitals fields in the dialog | Quick-pick dropdowns available for BP / Pulse / Temp / Weight / SpO2 / RR / Sugar | P1 |
| A-12 | Dialog open | Enter vitals (P1 value from 6.1) + notes, then Save | OPD created; success toast `OPD registration <no> created`; record the registration number | P0 |
| A-13 | Session closed (temporarily) | Cancel the session, then try to create an OPD | Creation is blocked (prepaid flow needs a session); toast offers an `Open counter session` link | P1 |
| A-14 | Re-open session | Re-create the blocked OPD | OPD now created | P1 |
| A-15 | Session open | Create OPD for **P2, P3, P4** the same way (all against the obstetric doctor) | 4 registrations exist, all assigned to the obstetric doctor, status `Waiting` (Pending) | P0 |
| A-16 | OPDs created | Check the OPD stats strip | Total / Waiting / In Progress / Completed counts are correct | P1 |
| A-17 | OPDs created | Search OPD list by patient name, token, and doctor | Correct filtering; clearing search restores the list | P1 |
| A-18 | OPDs created | Switch date presets (Today / Yesterday / Last 7 Days / Last Month / Custom) and status tabs (All / Waiting / In Progress / Completed / Not Visited) | Correct results per filter; the 4 new OPDs appear under `Waiting` / `Today` | P1 |
| A-19 | OPD created | Repeat an OPD for the **same patient P1** on the same day | Behaviour recorded: either a second registration is allowed or a duplicate is blocked (record actual behaviour as an observation) | P2 |

### C. Payment at booking (prepaid)

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| A-20 | Prepaid OPD = ON | Create an OPD | Visit fee is **auto-collected** against the active session; payment status `Received`; the session `Collected` total increases by the fee | P0 |
| A-21 | Prepaid OPD = OFF (toggle in Settings > Feature Flags) | Create an OPD | Payment stays `Pending` (nothing collected); session totals unchanged | P1 |
| A-22 | After A-20 | Open the OPD row details / payment view | Collected amount = fee; discount = 0; refund status = None | P1 |

---

## 8. Phase B - Doctor: Prescription

> Log out of reception and log in as the **Doctor** (`awez.vtt+1@gmail.com`).

### B1. Finding the patient and opening the prescription

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-01 | Doctor logged in | Open the doctor dashboard | Pending OPD patient count for this doctor is visible and includes the 4 new patients | P0 |
| B-02 | Doctor logged in | Open `OPD Management` | The doctor sees **only their own** appointments (the 4 new obstetric patients) | P0 |
| B-03 | OPD list | Filter by `Not Visited` / `Waiting` on Today | The 4 new patients appear | P0 |
| B-04 | OPD row for P1 | Click the action that starts the prescription (`Start Prescription` / `+ Prescription`) | Navigator opens `/prescriptions/new/{opdRegistrationId}`; header `New Prescription` | P0 |
| B-05 | Prescription page open | Observe the patient banner | Correct patient name / MR / age / gender; correct OPD/doctor; correct date | P0 |
| B-06 | Prescription page open | Observe the OPD intake vitals that reception captured in A-12 | The values entered at registration are pre-filled (they are the OPD intake values, not defaults) | P0 |
| B-07 | Doctor logged in, another doctor exists | Try to open a prescription for an appointment **not** assigned to this doctor (deep-link the OPD id or use the other doctor list) | Either not shown, or `404 / 403`; must NOT be able to prescribe for another doctor patients | P0 |
| B-08 | Prescription page open | Reload the page mid-entry | Unsaved data behaviour recorded: entries not yet saved are lost / restored (record actual) | P2 |

### B2. Vitals - including the placeholder (not-saved) rule

> **Key rule to verify:** on a NEW prescription the vitals inputs are shown pre-filled with **typical/default placeholder values**. Those defaults must **NOT** be persisted to the prescription unless the doctor explicitly picks a value from the vital quick-pick list (or types into the field and it is captured on save). If the doctor never touches a vital, the saved prescription must not silently store the placeholder.

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-09 | New prescription for a patient with **no** OPD intake vitals | Open the prescription, do not touch any vital | Vital inputs show placeholder/default values, but the values are visually indistinguishable from saved data only as placeholders; after `Save Draft`, the stored vitals for untouched fields are **blank/null** (NOT the placeholder) | P0 |
| B-10 | Same as B-09 | Click a vital quick-pick (e.g. BP) and choose `120/80` | The value is committed for that vital; after save it persists | P0 |
| B-11 | Same as B-09 | Type a non-preset value into a vital (e.g. Temp `100.2`) and save | The typed value persists | P0 |
| B-12 | Saved draft | Re-open the draft (edit) | Defaults are **never** re-applied to an existing prescription; the previously saved values (including intentionally blank ones) are shown exactly as stored | P0 |
| B-13 | New prescription | Enter an out-of-range/odd value (BP `abc`, Temp `999`, Pulse `-5`) | Input is rejected / sanitised (type=number); no negative or non-numeric saved; record behaviour | P1 |
| B-14 | New prescription | Enter all 7 vitals then save | All 7 persist and later appear on the print page | P0 |
| B-15 | Vitals entered | Confirm the same vitals block renders on the **history timeline / previous visits** for that patient | Correct values shown against this visit | P2 |

### B3. History sections + print-on-prescription toggles

> Seven sections: **Past Medical History** and **Family History** are always visible (all doctors). **Menstrual History, Surgical History, Per Abdomen, Per Vaginum, Per Speculum** are visible only to the Gynecologist. Each selected item has a per-item `print` flag and each section has a tri-state `select all` checkbox.

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-16 | Prescription open as Gynecologist | Locate the history sections | `Medical History` (Past Medical + Family) and `Obstetric Details` (Menstrual / Surgical / Per Abdomen / Per Vaginum / Per Speculum) are all visible | P0 |
| B-17 | Any history section | Add items from the catalogue (click chips) and add a free-typed custom item (`+ Add from list`) | Chips appear; custom item is saved to the lookup list for reuse | P1 |
| B-18 | Items selected | Toggle a single item `print` flag off | Item stays selected in the editor but is excluded from the print output | P0 |
| B-19 | Multiple items in one section | Click the section header `select all` checkbox | All items flip on; clicking again flips all off; mixed state shows the indeterminate marker | P1 |
| B-20 | New prescription for a patient with a previous obstetric visit | Open the new prescription | Past Medical / Family / obstetric history is **carried forward** from the most recent visit that had obstetric data | P0 |
| B-21 | After B-18/B-19 | Save draft, then open the print page | Only `print = true` items appear; the full selection is retained in the editor | P0 |
| B-22 | General Physician (non-Gynec) account if available | Open a prescription | The 5 obstetric-only sections are hidden; Past Medical + Family History are still available and savable | P1 |

### B4. LMP, EDD and pregnancy tenure

> Setting an LMP auto-creates a `PregnancyTenure` and computes **EDD by LMP = LMP + 280 days**. EDD by USG is captured separately and takes precedence when set (`EffectiveEdd = EddByUsg ?? EddByLmp`).

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-23 | New prescription for P1 (no active tenure) | Enter LMP = today (-) 56 days | EDD-by-LMP is auto-computed = LMP + 280 days; an **Active PregnancyTenure** is created for P1 | P0 |
| B-24 | LMP set | Verify the computed EDD | Exactly LMP + 280 days (spot-check the date arithmetic) | P0 |
| B-25 | LMP set | Change the LMP to a new valid past date | EDD recalculates; the existing tenure is updated (not duplicated) | P0 |
| B-26 | LMP set | Enter **EDD by USG** (different from EDD-by-LMP) | EDD-USG stored; `EffectiveEdd` = EDD-USG (USG wins) | P0 |
| B-27 | New prescription | Try to set a **future** LMP | Rejected is the expected/acceptable behaviour (validation); record actual | P1 |
| B-28 | Obstetric dates block | View the computed **USG schedule chart** (11-milestone template) | Chart rows render (8 weeks ... EDD); Sunday dates shift to Monday | P1 |
| B-29 | Obstetric dates block | Open the patient detail page / obstetric summary section | LMP, EDD-by-LMP, EDD-by-USG and the schedule are shown for P1 | P1 |
| B-30 | P1 active tenure exists | Try to **clear** the LMP | LMP cleared; record whether the tenure is also affected | P2 |
| B-31 | P2 (overdue) | Set LMP = today (-) 300 days (EDD already past) | An overdue tenure exists for P2 | P0 |
| B-32 | P2 overdue tenure | On a **new** prescription for P2, try to set a new LMP | **Blocked** (`InvalidOperationException` / 409); an inline popup prompts to close the overdue tenure first | P0 |
| B-33 | Close-tenure popup | Close the overdue tenure (outcome = Delivered / Abortion / NotKnown + notes) | Tenure becomes `Closed` with the outcome and notes; the popup updates state without a full page reload | P0 |
| B-34 | After B-33 | Set a new LMP for P2 | Now allowed; a **new** Active tenure is created | P0 |
| B-35 | Doctor + reception | Open the `EDD Overdue` tab (both roles) | Overdue patients list shows P2 while overdue, and drops off once closed | P1 |

### B5. Diagnosis

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-36 | Prescription open | Add 2-3 diagnosis chips from the list | Chips appear and are comma-joined on the record | P0 |
| B-37 | Diagnosis chips | Add a free-typed custom diagnosis | Saved and reusable | P1 |
| B-38 | Diagnosis chips | Remove one chip | Removed from the saved value | P1 |
| B-39 | Diagnosis chips | Enter a very long diagnosis string | Saved up to the field limit; no crash | P2 |
| B-40 | Prescription saved | Re-open and check diagnosis | All chips restored exactly | P0 |

### B6. Investigations

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-41 | Prescription open | Add investigations via the modal/toggle flow | Selected investigations appear as chips | P0 |
| B-42 | Investigations added | Add a free-typed investigation and save it to the lookup | Reusable on later prescriptions | P1 |
| B-43 | Investigations | Remove an investigation | Removed | P1 |
| B-44 | **Submitted** prescription | Open the submitted prescription | Investigations can **still be added** (this is the only field editable post-submit) | P0 |
| B-45 | Submitted prescription | Try to edit any other field (diagnosis, notes, medicines, vitals) | Field is read-only / edits are rejected | P0 |

### B7. Medicines - all form types and combinations

> Reminder of form types: Tablet, Capsule, Syrup, Drops, Tube, Injection, Powder, Other. Dosage is per slot (Morning / Afternoon / Evening / Night) with auto-calculated quantity and a multilingual remark (English / Hindi / Marathi).

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-46 | Prescription open | Search a medicine and add a **Tablet** (e.g. Folic Acid 5mg), 1-0-1, 30 days | Item added; quantity auto = 60; remark generated | P0 |
| B-47 | Prescription open | Add a **Capsule** (e.g. Calcium+D3), 0-0-1, 30 days | Added; correct unit label (`cap`) | P0 |
| B-48 | Prescription open | Add a **Syrup** (e.g. Iron syrup), 5 ml twice a day, 7 days | Added; unit `ml`; quantity correct | P0 |
| B-49 | Prescription open | Add a **Injection** (e.g. Tetanus Toxoid), single dose | Added; injection icon/unit shown; dose described as single | P0 |
| B-50 | Prescription open | Add a **Powder / Sachet** (e.g. vitamin D3 granules) | Added; unit `sachet` | P0 |
| B-51 | Prescription open | Add a **Drops** (e.g. multivitamin drops) | Added; unit `drops` | P0 |
| B-52 | Prescription open | Add a **Tube** (topical) | Added; unit `application` | P1 |
| B-53 | Prescription open | Add an **Other** form item | Added | P1 |
| B-54 | Items added | Set dose with a **half tablet** at night only (`0-0-0.5`) | Per-slot remark `0.5 tablet - Night`; quantity = 15 for 30 days | P0 |
| B-55 | Items added | Set different amounts across slots (e.g. 1-0-0.5-0) | Per-slot breakdown remark generated correctly | P1 |
| B-56 | Items added | Change the remark **language** (EN / HI / MR) via Settings | Remark text renders in the chosen language (unit labels translated too) | P0 |
| B-57 | Items added | Add a medicine **already on the prescription** | Blocked / warn `already added` (no duplicate) | P1 |
| B-58 | Item added | Edit the item (strength, dose, duration, frequency, timing, instructions) | Edits persist after save | P0 |
| B-59 | Item added | Remove an item | Removed; counts update | P1 |
| B-60 | Items added | Open the **Rx Preview** panel | Live preview lists each drug with generated remark, numbered | P1 |
| B-61 | Items added | Check the drug-drug **interaction** warning (add two interacting drugs, e.g. Aspirin + Warfarin) | Interaction warning shown (if configured); non-interacting combo shows none | P2 |
| B-62 | Prescription open | Add 15+ medicines | List stays usable; save/submit works; print paginates | P2 |

### B8. Medicine bundles ("Add Package")

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-63 | Prescription open | Click `Add Package` and pick a bundle | All bundle items are added at once with their dose/duration defaults | P0 |
| B-64 | Bundle added | Verify each bundle item | Every item present with the correct form / dose / duration | P0 |
| B-65 | Bundle added | Add a **second different** bundle | Both bundles imported; no duplicates lost | P0 |
| B-66 | Bundle added | Add a bundle that overlaps an existing medicine | Overlapping drug is skipped (already-added rule) and the rest are added | P1 |
| B-67 | Bundle added | Edit one imported item then add the same bundle again | Edited item is not silently overwritten; no duplicate added | P2 |
| B-68 | No bundle exists | Open `Add Package` | Empty/message state; no crash | P2 |

### B9. Save draft, submit and print

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-69 | Prescription filled for P1 | Click `Save Draft` | Saved as `Draft`; success toast; page stays editable; a prescription number is generated | P0 |
| B-70 | Draft saved | Verify the OPD status | OPD moves to `In Progress` (or remains as defined) - record actual | P1 |
| B-71 | Draft saved | Reload the page | All entered data restored exactly (vitals, history, LMP, diagnosis, investigations, medicines, next visit, notes) | P0 |
| B-72 | Draft saved | Click `Print` | Print page opens in a new tab | P0 |
| B-73 | Print page | Verify the header | Clinic name / doctor name / patient name + MR / age / gender / date / prescription number | P0 |
| B-74 | Print page | Verify vitals block | Only the vitals actually saved (not placeholders) appear | P0 |
| B-75 | Print page | Verify history block | Only items flagged `print = true` appear | P0 |
| B-76 | Print page | Verify obstetric block for P1 | LMP + EDD-by-LMP + EDD-by-USG shown (block hidden if LMP null) | P0 |
| B-77 | Print page | Verify diagnosis + investigations blocks | Correct chips listed | P0 |
| B-78 | Print page | Verify medicines table | Each drug with strength, form, dose/slot, frequency, duration, quantity, remark | P0 |
| B-79 | Print page (admin/doctor) | Switch the print **template** (e.g. Classic / Modern / Minimal / Two-Column) | Preview re-renders in the chosen template without saving | P1 |
| B-80 | Print page | Click `Save as Default` (template) | Template saved as the clinic default; a fresh print uses it | P2 |
| B-81 | Print page | Click `Print` (the browser/print-copy button) | Print dialog / PDF renders; the sticky action bar is excluded from the printed copy | P0 |
| B-82 | Print page | Click `Close` | The print tab closes | P2 |
| B-83 | Print page | Print a prescription with a very long medicine list + long history | Page does not clip content; prints over multiple pages cleanly | P1 |
| B-84 | Draft with LMP not set | Open print page | The obstetric dates block is hidden (no empty/`null` rendering) | P1 |
| B-85 | Draft complete for P1 | Click `Submit` | Confirmation; on confirm the prescription becomes `Submitted`; success toast; OPD status becomes `Completed`; if the patient has an email, the prescription email is sent (check `IsEmailSent`) | P0 |
| B-86 | Submitted | Re-open the prescription | Header shows `Submitted`; `Save Draft` / `Submit` hidden | P0 |
| B-87 | Prescription for P1 submitted | Check the OPD list status | P1 appears under `Completed` | P0 |

### B10. Prescription immutability after submit (Phase C)

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| C-01 | Submitted prescription | Open it as the doctor | Read-only: vitals, history, LMP, diagnosis, medicines, notes are not editable | P0 |
| C-02 | Submitted prescription | Add a new **investigation** | Allowed and persisted (only investigations remain editable) | P0 |
| C-03 | Submitted prescription | Attempt to add / edit / delete a **medicine** via the UI or by re-posting the update payload | Rejected (`409/400`); no change is persisted | P0 |
| C-04 | Submitted prescription | Attempt `Save Draft` / re-`Submit` | Not possible / idempotent (no duplicate submit, no status regression) | P0 |
| C-05 | Submitted prescription | Upload a report file (PDF/JPG/PNG/BMP, <= 50 MB) | Uploaded and listed against the prescription | P1 |
| C-06 | Report upload | Upload a disallowed type (`.exe`) or a > 50 MB file | Rejected with a clear message | P0 |
| C-07 | Submitted prescription | Print it | Prints the submitted content with the added investigation | P1 |
| C-08 | Submitted prescription | Check the prescription list page | Status badge shows `Submitted`; search by patient/doctor/date works | P1 |

> **Repeat Phase B (B1-B10) for P2 and P4.** For P4 specifically, open a **second** visit and confirm history + previous medicines can be carried forward (`Add from previous visit` / `Add all`).

### B11. Second-visit / history carry-forward (P4)

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| B-88 | P4 has a submitted prior visit | Book a new OPD for P4 with the obstetric doctor, open a new prescription | The **Visit History** panel lists prior prescriptions, newest first, paginated | P0 |
| B-89 | History panel | Click `Add` on a prior medicine | That drug is added with its previous dose / frequency / instructions | P0 |
| B-90 | History panel | Click `Add all` on a prior visit | All its medicines are added; duplicates skipped | P1 |
| B-91 | History panel | Open a prior visit detail | Correct vitals, meds, diagnosis shown for that visit | P1 |
| B-92 | New prescription for P4 | Observe obstetric carry-forward | Past Medical / Family / obstetric history carried from the latest obstetric visit | P0 |

---

## 9. Phase D - Discount & Service Items

> A doctor can apply a discount to an appointment's total and can also add service items (injection, IV, dressing, etc.). Reception can add / edit / remove / collect service items too. When a discount reduces the total **below** an already-collected amount, a **refund becomes due** (`PendingRefund`) and reception must confirm the cash was handed back.

### D1. Adding service items (particulars)

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| D-01 | Reception, OPD list | Open the service-items popup for an OPD | `OPD Service Items` dialog opens listing existing items | P0 |
| D-02 | Popup open | Add `Injection` with the default amount | Item added with `Pending` payment status | P0 |
| D-03 | Popup open | Add `IV` and override the amount | Added with the overridden amount | P0 |
| D-04 | Popup open | Add `Dressing`, `ECG`, `Sugar Test` | All added; running total correct | P1 |
| D-05 | Items added | Remove one item | Removed; total updates | P1 |
| D-06 | Items added | Edit an item's amount | Updated amount reflected in the bill | P1 |
| D-07 | Items added | Try to add an item with a negative / zero amount | Rejected or accepted per rules - record actual | P2 |
| D-08 | Items pending | Click `Collect` on one item (with an active session) | That item becomes `Received`; session `Collected` increases | P0 |
| D-09 | Items pending | Click `Collect All Pending` | All pending items become `Received`; toast shows the total | P0 |
| D-10 | No active session | Try to collect | Blocked; toast offers `Open counter session` | P0 |
| D-11 | Any role | Confirm the doctor can also open the same popup and add items | Doctor can add items; behaviour matches reception for add | P1 |

### D2. Doctor applies a discount

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| D-12 | Doctor, OPD list | Open the discount dialog for an OPD | `Apply Discount` dialog shows Total and Current Discount | P0 |
| D-13 | Discount dialog | Enter a discount **less than** the total, apply | Discount saved; new final amount = total - discount; OPD list reflects it | P0 |
| D-14 | Discount dialog | Enter a discount **equal to** the total | Final amount = 0; recorded | P1 |
| D-15 | Discount dialog | Enter a discount **greater than** the total | Rejected / clamped (no negative final amount) - record actual | P1 |
| D-16 | Payment already **Received** | Apply a discount that drops the total below the collected amount | `RefundDue` = collected - final; refund status becomes `PendingRefund`; a refund banner appears in the dialog | P0 |
| D-17 | `PendingRefund` shown | Click `Confirm Refund Returned` | Collected amount reduced by the refund; status becomes `Refunded`; `RefundedAt` / `RefundedByUserId` set | P0 |
| D-18 | Payment **Pending** (not collected) | Apply a discount | No refund implied; final amount reduced only | P0 |
| D-19 | Discount applied | Apply a **second** discount (change the value) | New value replaces the old; refund recalculated correctly | P1 |
| D-20 | Discount applied + service items present | Click `Collect Full Bill` (in the service-items popup) | The remaining final amount (after discount) is collected in one step; combined total correct | P0 |

---

## 10. Phase E - Payments & Refunds (Reception)

> Login as **Reception**.

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| E-01 | OPD with a `Pending` payment (prepaid OFF) | Click `Accept Amount` and enter the amount | Payment becomes `Received`; collected = amount; session `Collected` increases | P0 |
| E-02 | Payment pending | Ensure the payment row exists (`Ensure` action) | Payment record created / returned without duplicates | P1 |
| E-03 | Payment received | Open the payment details | Collected, discount, refund status all correct | P1 |
| E-04 | Payment partial | Accept a **partial** amount | Recorded as partial; balance shown | P1 |
| E-05 | Payment received + refund PENDING (from D-16) | Process the refund | Collected reduced by refund; status `Refunded`; timestamp + user recorded | P0 |
| E-06 | Payment with refund already `Refunded` | Try to refund again | Rejected (`409`): refund not pending | P0 |
| E-07 | Payment with **no** pending refund | Try to refund | Rejected (`409`) | P0 |
| E-08 | OPD `Cancelled`, payment received, refund pending | Try to **delete** the OPD | Blocked until the refund is confirmed (`RefundStatus != Refunded`) | P0 |
| E-09 | OPD `Cancelled`, refund `Refunded` | Delete the OPD | Deleted (204); cascade removes related payment / items as designed | P0 |
| E-10 | OPD not cancelled | Try to delete | Rejected (`409`): only cancelled OPDs can be deleted | P0 |
| E-11 | Cancel appointment (P3) | Cancel the OPD, refund the full collected amount, then delete | Full cancel -> refund -> delete cycle succeeds | P0 |
| E-12 | Any payment action | Verify the session stats | `Collected` reflects all receipts and refunds; `Net Balance` = collected - expenses | P0 |

---

## 11. Phase F - Amount Handover & Session Close

> Business rule: reception **cannot close** the counter session while an amount handover is **pending**. Once the handover is accepted (or collected directly by the recipient), the session can be closed.

### F1. Create handover (reception)

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| F-01 | Session open with collections | Click `New Handover` (or `Handover` on the session page) | Handover dialog opens; amount pre-filled with the session **Net Balance** | P0 |
| F-02 | Handover dialog | Select a recipient (doctor / admin) from the list | Recipient required; list excludes the current user and only shows valid target roles | P0 |
| F-03 | Handover dialog | Submit with the pre-filled amount | Handover created with status `Pending`; session shows `Handover Pending` badge; `HandedOverAt` set | P0 |
| F-04 | Handover created | Try to submit with **no** recipient | Blocked with a warning | P1 |
| F-05 | Handover created | Enter an amount greater than the net balance | Rejected / accepted per rules - record actual | P1 |
| F-06 | Handover pending | Try to **close the session** | **Blocked** (`409` / error toast) - session cannot close while a handover is pending | P0 |
| F-07 | Handover pending | Open the handover list as reception | The pending handover appears (not yet actionable by reception) | P1 |

### F2. Doctor accepts / disputes (or collects)

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| F-08 | Handover pending, doctor logged in | Open `Amount Handovers` | The handover appears under `Handovers For Me` | P0 |
| F-09 | Handover for me | Click `Accept` | Status -> `Accepted`; `AcceptedAt` set | P0 |
| F-10 | After F-09, reception | Go to `Counter Session` and close the session | **Close now succeeds** (handover resolved) | P0 |
| F-11 | Another pending handover | Click `Dispute` with resolution notes | Status -> `Disputed`; notes stored | P0 |
| F-12 | Disputed handover | Try to accept / dispute it again | Rejected (`409`): only pending handovers can be resolved | P0 |
| F-13 | Accepted handover | Try to accept / dispute it again | Rejected (`409`) | P0 |
| F-14 | Session open with collections, doctor logged in | Click `Collect from Reception`, pick the session, enter amount | Handover created and auto-**Accepted** in one step; session collections recorded | P0 |
| F-15 | After F-14 | Reception closes the session | Close succeeds | P0 |
| F-16 | Handover accepted, session still open | Verify the session banner | Shows resolved / accepted handover state (no `Handover Pending`) | P1 |
| F-17 | Handover pending | Reload / refresh both pages | Status persists correctly; no phantom re-pending | P1 |

### F3. Session close

| ID | Precondition | Steps | Expected result | Priority |
|---|---|---|---|---|
| F-18 | Session open, all handovers resolved | Click close-session | Session closes; `Closed` state; totals frozen at close (TotalCollected / TotalExpenses / NetAmount) | P0 |
| F-19 | Session closed | Open `Counter Session` | No active session; `Open Session` available; previous session listed in history | P0 |
| F-20 | Session closed | Try to collect a payment or add an item | Blocked (needs an active session) | P1 |
| F-21 | Session closed | Try to close it again (deep-link the id) | Rejected (`409`) | P1 |
| F-22 | Session closed | Re-open a new session | New session opens; collections start at 0 | P1 |
| F-23 | Two receptions (if available) | Both try to open a session | Only one open session per clinic | P1 |
| F-24 | Session close | Verify close aggregates: TotalCollected = sum of received payments in the session; TotalExpenses = sum of expenses; NetAmount = collected - expenses | Numbers reconcile exactly with the Finance Dashboard | P0 |

---

## 12. Edge Cases & Negative Testing (cross-cutting)

| ID | Area | Scenario | Expected result |
|---|---|---|---|
| X-01 | Vitals | Open a new prescription for a patient with **no** OPD intake and never touch the vitals | Saved vitals must be blank, not the placeholder defaults |
| X-02 | Vitals | Re-open a saved draft | Defaults never re-applied; blank stays blank |
| X-03 | LMP | Future LMP | Rejected (or clearly recorded) |
| X-04 | LMP | LMP = today | Accepted; EDD = today + 280 |
| X-05 | Tenure | Overdue open tenure + new LMP | Blocked until tenure closed |
| X-06 | Tenure | Close an already-closed tenure | `409` rejected |
| X-07 | Tenure | Close with an invalid outcome enum value | `400` validation error |
| X-08 | Tenure | Overdue list with no overdue patients | Empty state, no error |
| X-09 | Prescription | Submit with **zero** medicines (only diagnosis) | Record whether allowed; if allowed, no crash |
| X-10 | Prescription | Submit with everything empty | Record behaviour (allowed vs validation error) |
| X-11 | Prescription | Two rapid `Submit` clicks | Single submit; no duplicate email / duplicate state |
| X-12 | Prescription | Two doctors open the same patient prescription simultaneously | Last-write / concurrency handled (RowVersion) - no silent corruption |
| X-13 | Prescription | Very long notes / diagnosis (at and over the max length) | At max passes; over is rejected (`400`) |
| X-14 | Bundle | Bundle with a deleted / inactive medicine | Handled gracefully (skipped) - no crash |
| X-15 | OPD | Delete an OPD that has a submitted prescription | Record behaviour (blocked vs cascades) |
| X-16 | OPD | Create OPD for an inactive / missing doctor | Rejected / doctor not selectable |
| X-17 | OPD | Search with special characters (`%`, `_`, `'`) | No SQL error; safe handling |
| X-18 | Payment | Collect with no active session | Blocked with a clear message |
| X-19 | Payment | Accept amount = 0 or negative | Rejected / recorded |
| X-20 | Payment | Refund when nothing was collected | Rejected (`409`) |
| X-21 | Handover | Dispute twice / accept after dispute | Rejected (`409`) |
| X-22 | Handover | Close session with **no** handover ever created | Allowed (baseline) |
| X-23 | Session | Add an expense then close | NetAmount = collected - expenses; can go negative and shows red |
| X-24 | Printing | Print a prescription with LMP null | Obstetric block hidden |
| X-25 | Printing | Print with `Hide Clinic Header` = ON | Header suppressed |
| X-26 | Multi-tenant | Log in as a user of clinic B and try to open clinic A prescription / OPD / handover by id | `404` (treated as not found) - never returns clinic A data |
| X-27 | Date range | Filter OPD / handover by a date range across time zones | Day boundaries correct (`utcOffset` respected) |
| X-28 | Session list | Open session, refresh mid-flight | State consistent; no duplicate session rows |

---

## 13. Security / Penetration Testing

> These are black-box tests a tester can run through the UI and (in dev) via Swagger/Postman. Record every result. Any confirmed issue must be raised with severity (Critical / High / Medium / Low).

### 13.1 Authentication

| ID | Test | Expected result |
|---|---|---|
| SEC-01 | Call any API without a token (`GET /api/opd-registrations`) | `401 Unauthorized` |
| SEC-02 | Call with a malformed / invalid JWT | `401` |
| SEC-03 | Call with an expired JWT | `401` |
| SEC-04 | Call with a valid token but **no** `X-Application-Id` header on a clinic-scoped route | `403` (clinic context cannot be resolved) |
| SEC-05 | Log in with wrong password repeatedly | Rate-limited / locked after N attempts (record) |
| SEC-06 | Access a deep-link page (e.g. `/prescriptions/new/{id}`) while logged out | Redirected to login; no data leaks before auth |

### 13.2 Authorization / role escalation

| ID | Test | Expected result |
|---|---|---|
| SEC-07 | Reception (no clinical role) calls `POST /api/prescriptions` / `{..}/submit` | `403` |
| SEC-08 | Reception calls `PUT /api/obstetric/prescriptions/{id}/lmp` | `403` |
| SEC-09 | Doctor calls `POST /api/opd-registrations` (create OPD) | `403` (reception-only) |
| SEC-10 | Doctor calls `POST /api/counter-sessions/open` or `/close` | `403` |
| SEC-11 | Doctor calls `POST /api/opd-payments/accept` / `refund` | `403` |
| SEC-12 | Reception calls `POST /api/amount-handovers/{id}/accept` / `dispute` | `403` (clinical-only) |
| SEC-13 | Any user manipulates the UI to reveal a hidden button then calls the endpoint | Same `403` - UI hiding is not the security boundary |
| SEC-14 | Doctor attempts to create a prescription for an OPD assigned to another doctor | Not permitted / no data exposure |

### 13.3 Tenant isolation (IDOR / cross-clinic)

| ID | Test | Expected result |
|---|---|---|
| SEC-15 | Using clinic B token, `GET` a clinic A prescription / OPD / payment / handover by its GUID | `404 Not Found` (never returns clinic A's data) |
| SEC-16 | Using clinic B token, `PUT`/`DELETE` a clinic A record by GUID | `404`; no modification |
| SEC-17 | Send an `X-Application-Id` for a clinic the user does not belong to | `403` (validated against the JWT `app_ids` claim) |
| SEC-18 | Send a **body** `ApplicationId` different from the header (e.g. in `CreateOpdRegistrationDto`) | Body value ignored; header/`GetApplicationId()` wins |
| SEC-19 | Global lookups (`States`, `Cities`, `MedicineDosageForms`, `DoctorSpecialities`) are readable by all but not writable by non-admins | Write -> `403` |
| SEC-20 | Vital presets: reception tries to delete a **global** (system-default) preset | Blocked by handler (only clinic-owned presets deletable) |
| SEC-21 | Handover recipient manipulation: pass another clinic's user id as `HandedOverToUserId` | Rejected / no cross-clinic handover |

### 13.4 Input validation & injection

| ID | Test | Expected result |
|---|---|---|
| SEC-22 | Post empty / oversized strings to name, diagnosis, notes, medicine name | `400` validation error; no server error |
| SEC-23 | Send a non-numeric vital (e.g. `VitalPPulse: "abc"`) | `400`; no crash |
| SEC-24 | Send an out-of-range enum (e.g. `MedicineForm: 999`, `Outcome: 99`) | `400` (`Enum.IsDefined` guard) |
| SEC-25 | SQL-injection payloads in search (``' OR 1=1 --``, `%`) and in text fields | No SQL error; parameterised; returns safe results |
| SEC-26 | XSS payload in a free-typed history item / medicine note (`<script>alert(1)</script>`) | Rendered as text (Blazor encodes); no script execution; check the print page too |
| SEC-27 | Oversized `pageSize` (e.g. 10000) on list endpoints | Capped (<= 100) |
| SEC-28 | Negative / zero `page` and `pageSize` | Handled gracefully (no crash) |
| SEC-29 | Malformed GUID in a route (`/api/prescriptions/not-a-guid`) | `400` / route not matched; no 500 |
| SEC-30 | Upload report file with a spoofed extension (`.jpg` that is actually an executable) and a > 50 MB file | Size/type checks reject; stored files are not executable on the server |

### 13.5 Business-logic abuse

| ID | Test | Expected result |
|---|---|---|
| SEC-31 | Replay a `Submit` request twice | Idempotent; no duplicate email / no double state change |
| SEC-32 | Replay a `Confirm Refund Returned` twice | Second call rejected (`409`) |
| SEC-33 | Apply a discount after a refund was already returned | Record: must not create a negative collected balance |
| SEC-34 | Collect an item twice (double-collect) | Second collect rejected/already-received handled |
| SEC-35 | Close a session twice concurrently | Second rejected (`409`) |
| SEC-36 | Delete an OPD that is not cancelled | `409` |
| SEC-37 | Delete a global/system vital preset or a shared lookup | `403` / blocked |
| SEC-38 | Confirm no secrets (connection strings, passwords, tokens) are exposed in API responses | None present |
| SEC-39 | Confirm error responses are `ProblemDetails` (no stack traces / internal paths) in production config | Generic error body |
| SEC-40 | Confirm every delete/clear in the UI is gated by the shared `ConfirmDialog` (medicines, history, OPD, presets, etc.) | Confirmation required before destructive action |

### 13.6 Print / export data exposure

| ID | Test | Expected result |
|---|---|---|
| SEC-41 | Open the print page of another clinic's prescription by URL | `404` / not found |
| SEC-42 | Open the print page while logged out | Login redirect; no data |
| SEC-43 | Inspect the print page network calls for leaked internal ids / PHI beyond what is shown | Only expected fields returned |

---

## 14. End-to-End Happy Path (single scripted run)

Run this once, in order, as a smoke test. Both accounts, one session.

1. **Reception** logs in -> `Counter Session` -> `Open Session`.
2. **Reception** -> `OPD Management` -> `+ New OPD Entry` -> select **Obstetric Test A** -> select the **obstetric doctor** -> fee auto-fills -> save. Note the registration number.
3. **Reception** repeats step 2 for **B**, **C**, **D**.
4. **Doctor** logs in -> `OPD Management` (sees the 4 patients) -> opens **A** -> `Start Prescription`.
5. **Doctor** confirms the OPD intake vitals; picks/enters the remaining vitals; leaves one vital untouched.
6. **Doctor** adds Past Medical + Family + Menstrual + Per Abdomen history; toggles one item off print.
7. **Doctor** sets LMP -> EDD auto-computes -> sets EDD-by-USG. Verifies the tenure is Active.
8. **Doctor** adds diagnosis + investigations.
9. **Doctor** adds medicines: 1 Tablet, 1 Capsule, 1 Syrup, 1 Injection, 1 Powder; then imports a **bundle**; then imports a **second** bundle.
10. **Doctor** `Save Draft` -> `Print` -> verifies vitals (untouched one is blank), history (toggled-off item absent), obstetric dates, medicines. Closes the print tab.
11. **Doctor** `Submit`. Confirms the prescription is now read-only and adds one more **investigation**.
12. **Reception** adds an `Injection` and an `IV` service item; `Collect All Pending`.
13. **Doctor** applies a discount that drops the total below the collected amount -> refund becomes `PendingRefund`.
14. **Reception** `Confirm Refund Returned`.
15. **Reception** creates an **Amount Handover** for the net balance to the doctor.
16. **Reception** tries to close the session -> **blocked** (handover pending).
17. **Doctor** `Accept`s the handover.
18. **Reception** closes the session -> **succeeds**.
19. **Reception** cancels **C**'s appointment, refunds, and deletes the cancelled OPD.

**Pass =** every step behaves as described, with no unexpected errors.

---

## 15. Traceability / Results Matrix

Fill this in while testing. `Status` = Pass / Fail / Blocked / N/A.

### 15.1 Created artefacts (record the real values)

| Artefact | Value |
|---|---|
| Counter session id (Phase A) | |
| P1 MR number / OPD number / prescription number | |
| P2 MR number / OPD number / prescription number | |
| P3 MR number / OPD number | |
| P4 MR number / OPD number / prescription numbers (2 visits) | |
| Bundle(s) used | |
| Handover id | |

### 15.2 Phase roll-up

| Phase | Cases | Passed | Failed | Blocked | Notes |
|---|---|---|---|---|---|
| A - Reception session + OPD | 22 | | | | |
| B - Doctor prescription | 92 | | | | |
| C - Post-submit | 8 | | | | |
| D - Discount + service items | 20 | | | | |
| E - Payments + refunds | 12 | | | | |
| F - Handover + session close | 24 | | | | |
| X - Edge cases | 28 | | | | |
| SEC - Penetration tests | 43 | | | | |

### 15.3 Defects found

| Defect id | Case id | Severity | Title | Steps | Actual | Expected | Evidence | Status |
|---|---|---|---|---|---|---|---|---|
| | | | | | | | | |

---

## 16. Defect Reporting Template

```
Defect ID      :
Title          :
Severity       : Critical / High / Medium / Low
Priority       : P0 / P1 / P2
Environment    : Shifa Clinic - <build / URL / date>
Role / Account : Reception (awezlala+1@gmail.com) | Doctor (awez.vtt+1@gmail.com)
Case ID        : e.g. B-23
Preconditions  :
Steps to repeat:
  1.
  2.
  3.
Actual result  :
Expected result:
Frequency      : Always / Intermittent (x of y)
Evidence       : screenshot / video / network log / correlation id (from API error response)
Notes          :
```

> When a defect produces a server error, capture the **correlation id** from the `ProblemDetails` response body and include it - support can trace it in the Azure Table `ApiErrorLogs` / `ActionLogs`.

---

## 17. Severity / Priority Definitions (quick reference)

| Severity | Meaning |
|---|---|
| Critical | Data loss, cross-clinic data exposure, money miscomputed, auth bypass |
| High | Feature broken for a whole role; prescription cannot be saved/submitted |
| Medium | Feature partially broken; workaround exists |
| Low | Cosmetic / wording / minor UX |

| Priority | Meaning |
|---|---|
| P0 | Must pass before release |
| P1 | Should pass before release |
| P2 | Nice to verify; can ship with a known issue |

---

*End of test plan.*
