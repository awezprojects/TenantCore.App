# TenantCore.App — Current State Snapshot

**Last verified:** 2026-09-07 (centralized-logging-service executed — no DbSet change, Azure Table Storage is a separate store from ClinicDbContext; added IErrorLogger service registration)
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
| SubscriptionPlans | SubscriptionPlan | No (global catalogue) |
| ClinicSubscriptions | ClinicSubscription | Yes |
| SubscriptionAlertSettings | SubscriptionAlertSetting | No (global config) |
| MedicineBundles | MedicineBundle | Yes |
| MedicineBundleItems | MedicineBundleItem | Yes |
| HistoryLookupItems | HistoryLookupItem | Yes |
| States | State | No (lookup) |
| Cities | City | No (lookup) |
| ClinicLocations | ClinicLocation | Yes |

**Total DbSets: 39**

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
| ISubscriptionPlanRepository | SubscriptionPlanRepository |
| IClinicSubscriptionRepository | ClinicSubscriptionRepository |
| ISubscriptionAlertSettingRepository | SubscriptionAlertSettingRepository |
| IMedicineBundleRepository | MedicineBundleRepository |
| IHistoryLookupItemRepository | HistoryLookupItemRepository |
| IStateRepository | StateRepository |
| ICityRepository | CityRepository |
| IClinicLocationRepository | ClinicLocationRepository |

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
| IErrorLogger | ErrorLoggingService (writes to Azure Table Storage via the independent `TenantCore.Logging` project — `ApiErrorLogs`/`FrontendErrorLogs`, not `ClinicDbContext`) |

---

## How to Verify This Snapshot

Run this mental check at the start of each plan session:

1. Read `ClinicDbContext.cs` — count the `DbSet<T>` lines
2. Compare count to "Total DbSets" above
3. If counts differ → update this file to match the actual DbContext before continuing
4. If counts match → trust this snapshot, skip reading the actual file again this session
