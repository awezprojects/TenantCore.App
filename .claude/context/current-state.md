# TenantCore.App — Current State Snapshot

**Last verified:** 2026-08-31 (updated after prepaid-opd-fee-collection execution — added ClinicFeatureFlags DbSet/repo; no schema change to existing OpdPayments DbSet, only new columns)
**Verified against:**
- `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs`
- `src/TenantCore.Infrastructure/DependencyInjection.cs`

> This file is auto-maintained by the plan and execute commands.
> Do NOT edit manually. If you make a manual change to DbContext or DI outside the command workflow,
> the next plan session will detect the drift and auto-correct this file.

---

## DbContext: ClinicDbContext

**File:** `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs`

| DbSet Property | Entity Type | Tenant-Scoped |
|----------------|-------------|---------------|
| Patients | Patient | Yes |
| OpdRegistrations | OpdRegistration | Yes |
| IpdRegistrations | IpdRegistration | Yes |
| ClinicFeeConfigs | ClinicFeeConfig | Yes |
| MedicineTypes | MedicineType | Yes |
| MedicineDosageForms | MedicineDosageForm | No (lookup) |
| Medicines | Medicine | Yes |
| Prescriptions | Prescription | Yes |
| PrescriptionItems | PrescriptionItem | Yes |
| PrescriptionReports | PrescriptionReport | Yes |
| ObstetricPrescriptionData | ObstetricPrescriptionData | Yes |
| DosageRemarks | DosageRemark | Yes |
| PrescriptionConfigs | PrescriptionConfig | Yes |
| DoctorProfiles | DoctorProfile | Yes |
| DoctorSpecialities | DoctorSpeciality | No (lookup) |
| Wards | Ward | Yes |
| Rooms | Room | Yes |
| Beds | Bed | Yes |
| ClinicUsgTemplates | ClinicUsgTemplate | Yes |
| PregnancyTenures | PregnancyTenure | Yes |
| UsgTemplateRows | UsgTemplateRow | Yes |
| DoctorFeeConfigs | DoctorFeeConfig | Yes |
| Particulars | Particular | Yes |
| OpdParticulars | OpdParticular | Yes |
| OpdPayments | OpdPayment | Yes |
| ExpenseCategories | ExpenseCategory | Yes |
| ExpenseRecords | ExpenseRecord | Yes |
| CounterSessions | CounterSession | Yes |
| AmountHandovers | AmountHandover | Yes |
| ClinicFeatureFlags | ClinicFeatureFlags | Yes |

**Total DbSets: 30**

---

## Infrastructure DI Registrations

**File:** `src/TenantCore.Infrastructure/DependencyInjection.cs`

### Repositories (Scoped)

| Interface | Implementation |
|-----------|---------------|
| IPatientRepository | PatientRepository |
| IOpdRegistrationRepository | OpdRegistrationRepository |
| IIpdRegistrationRepository | IpdRegistrationRepository |
| IClinicFeeConfigRepository | ClinicFeeConfigRepository |
| IMedicineTypeRepository | MedicineTypeRepository |
| IMedicineDosageFormRepository | MedicineDosageFormRepository |
| IMedicineRepository | MedicineRepository |
| IPrescriptionRepository | PrescriptionRepository |
| IObstetricPrescriptionDataRepository | ObstetricPrescriptionDataRepository |
| IDosageRemarkRepository | DosageRemarkRepository |
| IPrescriptionConfigRepository | PrescriptionConfigRepository |
| IDoctorProfileRepository | DoctorProfileRepository |
| IDoctorSpecialityRepository | DoctorSpecialityRepository |
| IWardRepository | WardRepository |
| IRoomRepository | RoomRepository |
| IBedRepository | BedRepository |
| IClinicUsgTemplateRepository | ClinicUsgTemplateRepository |
| IPregnancyTenureRepository | PregnancyTenureRepository |
| IDoctorFeeConfigRepository | DoctorFeeConfigRepository |
| IParticularRepository | ParticularRepository |
| IOpdParticularRepository | OpdParticularRepository |
| IOpdPaymentRepository | OpdPaymentRepository |
| IExpenseCategoryRepository | ExpenseCategoryRepository |
| IExpenseRecordRepository | ExpenseRecordRepository |
| ICounterSessionRepository | CounterSessionRepository |
| IAmountHandoverRepository | AmountHandoverRepository |
| IClinicFeatureFlagsRepository | ClinicFeatureFlagsRepository |

### Services (Scoped)

| Interface | Implementation |
|-----------|---------------|
| IFileStorageService | LocalFileStorageService |
| IBlobStorageService | AzureBlobStorageService |
| IEmailService | EmailService |
| IPdfConversionService | PdfConversionService |
| IAuthApplicationService | AuthApplicationService |
| IAuthClinicService | AuthClinicService |
| IApplicationAccessValidator | ApplicationAccessValidator |

---

## How to Verify This Snapshot

Run this mental check at the start of each plan session:

1. Read `ClinicDbContext.cs` — count the `DbSet<T>` lines
2. Compare count to "Total DbSets" above
3. If counts differ → update this file to match the actual DbContext before continuing
4. If counts match → trust this snapshot, skip reading the actual file again this session
