# TenantCore.App — Feature Registry

> Auto-maintained by execute-app-feature command. Read by plan-app-feature before planning.
> Purpose: tells Claude what already exists so it never duplicates or conflicts with prior work.

---

## How to Read This

- **Pre-existing** — was in the codebase before this registry was set up (2026-06-16)
- **Planned** — PLAN.md written, not yet executed
- **Executed** — fully implemented through the execute command

---

## Domain Coverage Map

This table is the fastest way for the plan command to check "does this already exist?"

| Domain Area | Entities | Status | Notes |
|-------------|----------|--------|-------|
| Patients | Patient | Pre-existing | Full CRUD, tenant-scoped, MR number lookup |
| OPD Registrations | OpdRegistration | Pre-existing | Linked to Patient |
| IPD Registrations | IpdRegistration | Pre-existing | Linked to Patient |
| Medicines | Medicine | Pre-existing | Tenant-scoped |
| Medicine Types | MedicineType | Pre-existing | Tenant-scoped |
| Medicine Dosage Forms | MedicineDosageForm | Pre-existing | Lookup data, not tenant-scoped |
| Prescriptions | Prescription, PrescriptionItem, PrescriptionConfig | Pre-existing | Full prescription flow incl. PDF |
| Prescription Reports | PrescriptionReport, ObstetricPrescriptionData | Pre-existing | Report storage |
| Dosage Remarks | DosageRemark | Pre-existing | Tenant-scoped |
| Doctor Profiles | DoctorProfile | Pre-existing | Tenant-scoped |
| Doctor Specialities | DoctorSpeciality | Pre-existing | Lookup data |
| Wards | Ward | Pre-existing | Tenant-scoped |
| Rooms | Room | Pre-existing | Tenant-scoped |
| Beds | Bed | Pre-existing | Tenant-scoped |
| Clinic Fee Config | ClinicFeeConfig | Pre-existing | Tenant-scoped |
| Obstetric LMP & USG Templates | ClinicUsgTemplate, UsgTemplateRow | Executed | Adds Lmp/EddByLmp/EddByUsg to ObstetricPrescriptionData; clinic-customizable USG schedule |
| Pregnancy Tenure | PregnancyTenure | Executed | Lifecycle tracking (Active/Closed) for each pregnancy; EDD overdue tab; close-tenure workflow; blocks new LMP when overdue tenure is open |
| Patient LMP Tenure View | — (no new entity) | Executed | "View LMP" button on patient list; active status badge; full tenure history popup per patient |

---

## Executed Features (via this workflow)

| Feature | Plan Date | Execute Date | New Entities | New DbSets | Files Created | Files Modified |
|---------|-----------|-------------|-------------|------------|--------------|----------------|
| obstetric-lmp-usg-template | 2026-06-16 | 2026-06-16 | ClinicUsgTemplate, UsgTemplateRow | ClinicUsgTemplates, UsgTemplateRows | 45 | 10 |
| lmp-edd-pregnancy-tenure | 2026-06-16 | 2026-06-16 | PregnancyTenure | PregnancyTenures | 18 | 8 |
| patient-lmp-tenure-view | 2026-06-16 | 2026-06-16 | — | — | 3 | 8 |
| remove-old-unused-pages | 2026-06-17 | 2026-06-17 | — | — | 0 (9 deleted) | 4 |

---

## Planned (not yet executed)

| Feature | Plan Date | Plan File |
|---------|-----------|-----------|
| *(none)* | — | — |

---

## Update Instructions (for execute command)

After executing a feature, append a row to "Executed Features" with:
- Feature name (kebab-case)
- Today's date for Execute Date
- New entity names added
- New DbSet names added
- Count of files created and modified
