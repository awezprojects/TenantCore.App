# Feature Plan: Print-on-Prescription Control for Obstetric History Items

**Repo:** TenantCore.App
**Date:** 2026-09-08
**Domain area:** Prescriptions / Obstetric Details
**Status:** Approved — ready for execution

---

## Overview

Doctors currently have no control over which selected findings in the Obstetric Details tab (Menstrual History, Past Medical History, Family History, Surgical History, Per Abdomen, Per Vaginum, Per Speculum) actually appear on the printed prescription — every selected item is printed unconditionally. This feature adds a per-item "print on prescription" flag to each selected finding, plus a tri-state "select all" checkbox per section so a doctor can flip every item in a section on or off in one click, or mix and match individual items. The flag travels with the prescription record itself (not the shared item catalogue), defaults to on so nothing currently printed disappears, and only affects the printed PDF — the doctor's full clinical selection is always retained in the chart regardless of what's marked to print.

Scope is limited to the 7 Obstetric Details sections. `Diagnosis` and `Investigation` use the same underlying chip-picker component but store their data in an entirely different shape (a comma-joined string on `Prescription.Diagnosis`, and a separate modal/toggle flow for Investigation) and are explicitly out of scope for this change.

**Added scope — specialty-based visibility.** Of the 7 sections, two (**Past Medical History** and **Family History**) are generic and clinically relevant to every doctor, not just obstetricians — they should be available on every prescription regardless of the writing doctor's speciality. The other 5 (Menstrual History, Surgical History, Per Abdomen, Per Vaginum, Per Speculum) stay obstetric-only, visible only to Gynecologists, exactly as today. This requires moving Past Medical History and Family History out of the existing Gynecologist-only "Obstetric Details" panel into a new section visible to every doctor, and relaxing the save/carry-forward logic (currently gynecologist-only end to end) so these two fields work for General Physicians too.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Shared | New `HistoryItemSelectionDto`; the 7 obstetric list fields on both `ObstetricPrescriptionDataDto` and `UpsertObstetricPrescriptionDataDto` change from `IReadOnlyList<string>` to `IReadOnlyList<HistoryItemSelectionDto>` |
| Domain | `ObstetricPrescriptionData` entity's JSON serialize/deserialize logic for the 7 history fields changes shape; backward-compatible with data already saved in the old plain-string-array format |
| Application | `PrescriptionTranslator`'s entity↔DTO mapping for the 7 fields |
| Blazor Client | `HistoryChipField.razor` gains an opt-in print-toggle mode; `PrescriptionForm.razor`'s 7 obstetric call sites + save/carry-forward logic + a new specialty-agnostic section for the 2 generic fields; `PrescriptionPrint.razor` filters by the flag |

No new entity, no new repository, no new controller, no new DbSet, **no EF migration** — the 7 fields are already `string?` JSON-blob columns; only the JSON shape stored inside them changes.

---

## Data Shape: `HistoryItemSelectionDto` (new)

Replaces a bare `string` wherever a selected history item is stored or transmitted for the 7 in-scope sections.

| Property | Type | Notes |
|----------|------|-------|
| Value | string | The finding text — same value that's shown today (catalogue-selected or free-typed) |
| PrintOnPrescription | bool | Whether this item is included on the printed PDF. Defaults to `true` for newly added items and for any item read back from a prescription saved before this feature shipped |

---

## Section Visibility by Doctor Speciality

**Existing state (found while investigating):** the entire "Obstetric Details" panel — all 3 sub-tabs, including all 7 history sections — is already gated behind a single `_isGynecologist` flag in `PrescriptionForm.razor` (computed from the logged-in doctor's profile speciality, matched by substring on "Gynec"). General Physicians see none of it today, and no `ObstetricPrescriptionData` row is even created for their prescriptions (`BuildObstetricDto()` returns `null` immediately when `!_isGynecologist`).

**New behavior:**

| Section | Visible to |
|---------|-----------|
| Past Medical History | Every doctor (General Physician, Gynecologist, any other speciality) |
| Family History | Every doctor |
| Menstrual History, Surgical History, Per Abdomen, Per Vaginum, Per Speculum | Gynecologists only (unchanged from today) |

**Design decision:** Past Medical History and Family History are removed from inside the Gynecologist-gated "Obstetric Details → History & Examination" sub-tab and placed in a new, always-visible section on the prescription form (rendered once, outside the `_isGynecologist` gate). This avoids showing the same two fields in two different places for Gynecologists — there is exactly one location for each field, and which doctor sees which fields is the only thing that varies. The remaining 5 sections stay exactly where they are today, inside the Gynecologist-only "Obstetric Details" panel.

**Data storage is unaffected by this change** — Past Medical History and Family History continue to be stored as fields on `ObstetricPrescriptionData` (the same 1:1-with-prescription table used for the other 5 sections) regardless of the writing doctor's speciality. This is a presentation-layer change only; a General Physician's prescription will now get an `ObstetricPrescriptionData` row created purely to hold these two fields when either has content, same mechanism as today, just no longer gated on speciality.

**Reused existing convention:** the plan reuses the existing `_isGynecologist` flag (substring match on `SpecialityName` containing "Gynec") rather than introducing a new specialty flag or enum — this matches the one existing gating pattern already used elsewhere on this same page (pregnancy tenure loading, obstetric data carry-forward).

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Dtos/HistoryItemSelectionDto.cs` | `{ Value, PrintOnPrescription }` — the new per-item shape used across the 7 obstetric history fields |

### Blazor Client (`src/TenantCore.Web.Client/`)

| File | Purpose |
|------|---------|
| `Services/TriStateCheckboxState.cs` (or similar static helper) | Pure function: given a list of per-item print flags, returns Checked / Unchecked / Indeterminate — drives the section header checkbox. Kept as a standalone pure function so it's independently unit-testable without a component test harness |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Shared/Dtos/ObstetricPrescriptionDataDto.cs` | The 7 section fields on both `ObstetricPrescriptionDataDto` and `UpsertObstetricPrescriptionDataDto` change from `IReadOnlyList<string>` / `IReadOnlyList<string>?` to `IReadOnlyList<HistoryItemSelectionDto>` / `IReadOnlyList<HistoryItemSelectionDto>?` |
| `src/TenantCore.Domain/Entities/ObstetricPrescriptionData.cs` | `SerializeList` (private helper) now serializes `HistoryItemSelectionDto` objects instead of bare strings; a paired deserialize helper (used by the translator) inspects the stored JSON and transparently handles both the new object-array shape and the old plain-string-array shape, defaulting `PrintOnPrescription = true` for anything in the legacy shape |
| `src/TenantCore.Application/Features/Prescriptions/Translators/PrescriptionTranslator.cs` | `ObstetricDataToDto` / `DeserializeStringList` updated to produce `IReadOnlyList<HistoryItemSelectionDto>` per section using the entity's new backward-compatible deserialize helper |
| `src/TenantCore.Web.Client/Components/HistoryChipField.razor` | New opt-in parameters: `ShowPrintToggle` (bool, default `false` — Diagnosis/Investigation call sites pass nothing and are unaffected), `PrintFlags` (`List<bool>`, index-aligned with `SelectedItems`). When `ShowPrintToggle` is on: renders a small "Print" checkbox on each chip, and a tri-state "Print all" checkbox next to the section label (using the new helper) that sets every flag on/off in one click. The component keeps `SelectedItems` and `PrintFlags` in lockstep internally for every add/remove path (suggestion click, free-type submit, chip removal) so no external synchronization code is needed at the call sites |
| `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionForm.razor` | All 7 `<HistoryChipField>` call sites turn on `ShowPrintToggle` and bind a new parallel `List<bool>` field per section (e.g. `_menstrualHistoryPrint`); `BuildObstetricDto()` zips each section's value-list and print-flag-list into `HistoryItemSelectionDto` entries for save; `CarryForwardObstetricData()` unpacks the read DTO's entries back into the paired value/flag lists. **Additionally:** the Past Medical History and Family History `<HistoryChipField>` instances move out of the `@if (_isGynecologist)`-gated "Obstetric Details" panel into a new always-visible card rendered for every doctor; the remaining 5 stay inside the existing gated panel. `BuildObstetricDto()`'s `if (!_isGynecologist) return null;` early return is removed so the method runs for every doctor — the `hasData` check already correctly returns `null` when everything is empty, and General Physicians' backing fields for the 5 gynecologist-only sections simply stay empty since those inputs never render for them. The `_isGynecologist &&` condition on the two `CarryForwardObstetricData(...)` call sites is removed so carry-forward also works for General Physicians |
| `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionPrint.razor` | The `historySection` render fragment filters each of the 7 lists to `PrintOnPrescription == true` before joining into the printed "PATIENT HISTORY" block; a section is skipped entirely (as it already is today for empty lists) if nothing in it is marked to print |

---

## UI Behavior — Print Toggle & Select-All

| Scenario | Behavior |
|----------|----------|
| Doctor adds a new finding (catalogue suggestion or free-typed) | Added with `Print = true` by default |
| Doctor unchecks the "Print" box on one chip | That item stays selected clinically but is excluded from the printed PDF; header checkbox becomes indeterminate if it was previously checked |
| Doctor checks the section header checkbox | Every item in that section is set to `Print = true` |
| Doctor unchecks the section header checkbox | Every item in that section is set to `Print = false` |
| All items in a section are individually checked | Header checkbox shows checked |
| All items in a section are individually unchecked | Header checkbox shows unchecked |
| Some checked, some not | Header checkbox shows indeterminate (dash), not checked and not unchecked |
| Prescription saved before this feature ships is opened/printed | All items in the 7 sections read as `Print = true` (legacy format defaults to on — nothing that used to print stops printing) |

Labeling: each chip gets a short "Print" checkbox label; each section header gets a "Print all" checkbox — short, consistent wording per your request, distinct from the existing item-selection UI so doctors don't confuse "is this finding selected" with "does this finding print."

---

## Business Rules

1. `PrintOnPrescription` only affects the generated PDF/print view — it never removes an item from the doctor's clinical record or from what's shown while editing the prescription.
2. New items (from the catalogue or free-typed) always start with `PrintOnPrescription = true`.
3. Data saved before this feature shipped (plain string arrays) is read as `PrintOnPrescription = true` for every item — fully backward compatible, no backfill/migration needed.
4. A section with zero items marked to print is omitted from the printed output entirely, same as an empty section is today.
5. Past Medical History and Family History are available to every doctor regardless of speciality; the other 5 obstetric sections remain visible only to doctors whose profile speciality matches "Gynec" (the existing `_isGynecologist` check) — a General Physician never sees Menstrual History, Surgical History, Per Abdomen, Per Vaginum, or Per Speculum, on the form or in the printed output, since no data is ever entered into them for that doctor's prescriptions.

---

## Multi-Tenancy Checklist

Not applicable — no new entity, no new repository or controller. The changed data already lives inside the existing tenant-scoped `ObstetricPrescriptionData` record, reached only through the existing prescription create/update flow, which already carries `ApplicationId` end-to-end.

---

## EF Migration

**None required.** The 7 fields remain `string?` JSON-blob columns at the database level; only the JSON shape stored inside them changes, and the read path is backward-compatible with data already in the old shape.

---

## Implementation Order

1. Shared — `HistoryItemSelectionDto`
2. Shared — update `ObstetricPrescriptionDataDto` / `UpsertObstetricPrescriptionDataDto` field types
3. Domain — update `ObstetricPrescriptionData`'s serialize helper + add the backward-compatible deserialize helper
4. Application — update `PrescriptionTranslator`'s obstetric mapping to use the new deserialize helper
5. Blazor Client — add the tri-state helper function + its unit test
6. Blazor Client — extend `HistoryChipField.razor` with the opt-in print-toggle mode
7. Blazor Client — wire the 7 obstetric call sites in `PrescriptionForm.razor` (new backing fields, `BuildObstetricDto()`, `CarryForwardObstetricData()`)
8. Blazor Client — move Past Medical History and Family History out of the Gynecologist-gated panel into the new always-visible section; remove the `_isGynecologist` gating on `BuildObstetricDto()`'s early return and on the `CarryForwardObstetricData()` call sites
9. Blazor Client — update `PrescriptionPrint.razor`'s filtering
10. Unit tests — translator round-trip (new format + legacy-format backward compatibility), entity serialize/deserialize, tri-state helper
11. Manual smoke test: as a Gynecologist, confirm all 7 sections still work with print toggles; as a General Physician, confirm only Past Medical History and Family History are visible/editable and save correctly, and confirm the print output for a GP's prescription shows only those two sections when populated; add several findings across two sections, uncheck one item and confirm print output excludes it while the item stays selected in the editor; toggle a section's header checkbox both ways and confirm all items follow; open an existing (pre-feature) prescription and confirm everything still prints as before

---

## Test Files to Create

| File | What it covers |
|------|---------------|
| `tests/TenantCore.Application.Tests/Features/Prescriptions/Translators/PrescriptionTranslatorTests.cs` (extend existing, or add if absent) | `ObstetricDataToDto` maps `HistoryItemSelectionDto` entries correctly for the new format; reading a legacy plain-string-array JSON value returns every item with `PrintOnPrescription = true`; round-trip (serialize new format via entity, deserialize via translator) preserves `Value` and `PrintOnPrescription` exactly |
| `tests/TenantCore.Domain.Tests/Entities/ObstetricPrescriptionDataTests.cs` (extend existing, or add if absent) | `CreateOrUpdate`/`Update` correctly serialize mixed print-flag values per section; legacy-format JSON string is still readable after the shape change |
| `tests/TenantCore.Web.Client.Tests/.../TriStateCheckboxStateTests.cs` | All-true → Checked; all-false → Unchecked; mixed → Indeterminate; empty list → Unchecked (no items to be "all checked") |

---

## Open Questions / Risks

- Scope is intentionally limited to the 7 Obstetric Details sections — Diagnosis and Investigation, which reuse the same chip component, are untouched since their storage doesn't match this pattern. A future ask to extend print-control there would be a separate, larger effort.
- The tri-state header checkbox needs the native `indeterminate` DOM property, which Blazor's `@bind` doesn't expose directly — implementation will need a small `IJSRuntime` interop call (consistent with existing localStorage-interop patterns already used elsewhere in the client), set after render whenever the computed state is Indeterminate.
- No backend endpoint changes and no new authorization surface — this entire feature rides inside the existing prescription create/update request/response, so no new security review surface beyond what a normal PR review already covers.
- Speciality gating relies entirely on `DoctorProfile.SpecialityName` containing "Gynec" — a doctor whose profile speciality is left blank, misspelled, or uses a synonym (e.g. "OB/GYN", "Obstetrician") that doesn't contain "Gynec" will be treated as a General Physician and won't see the 5 obstetric-only sections. This is the same risk the existing `_isGynecologist` check already carries today for the whole panel — not a new risk introduced by this plan, but worth a quick check during execution of what speciality values are actually seeded/in use.
- Removing the `_isGynecologist` gate from `BuildObstetricDto()`'s early return means every doctor's prescription now potentially creates an `ObstetricPrescriptionData` row (whenever Past Medical History or Family History has content) where previously only Gynecologists' prescriptions did. This is intended and matches "generic field available for all types of prescription," but it does mean the `ObstetricPrescriptionData` table stops being an exclusively-Gynecologist-authored table — worth confirming no other code path assumes "a row in this table implies a Gynecologist wrote it" (the research pass found no such assumption, but execution should double-check `CreatePrescriptionHandler`/`UpdatePrescriptionHandler` and anything reading this table for reporting).
