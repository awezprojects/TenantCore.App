# TenantCore.App — Current State Snapshot

**Last verified:** 2026-09-28 (clinic-trial-razorpay-subscriptions executed — 4 new DbSets, 4 new repositories, durable workflow engine, Razorpay/Auth-internal HTTP clients, 2 new hosted services)
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
| VitalPresetLookupItems | VitalPresetLookupItem | Yes (with global system defaults) |
| ClinicOnboardingRequests | ClinicOnboardingRequest | No (user-scoped, not tenant-scoped — no clinic exists yet) |
| SubscriptionPayments | SubscriptionPayment | Partial (ApplicationId null until provisioning for onboarding payments; always set for renewals) |
| WorkflowTasks | WorkflowTask | No (internal durable-outbox job table) |
| PaymentWebhookEvents | PaymentWebhookEvent | No (internal webhook inbox) |

**Total DbSets: 44**

**ClinicSubscription additions (clinic-trial-razorpay-subscriptions):** `SubscriptionPaymentId` (Guid?, unique filtered), `OnboardingRequestId` (Guid?, unique filtered), `PurchasedByUserId` (Guid?). `Create()` factory gained 4 new optional trailing params — existing callers unaffected.

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
| IMedicineTypeRepository | CachedMedicineTypeRepository (wraps MedicineTypeRepository — served from `RefreshableCache<MedicineType>`, kept warm by `MedicineCacheWarmupService`; also reloads inline immediately after a write; see `plan/medicine-search-caching/PLAN.md`) |
| IMedicineDosageFormRepository | CachedMedicineDosageFormRepository (wraps MedicineDosageFormRepository — same pattern) |
| IMedicineRepository | CachedMedicineRepository (wraps MedicineRepository — caches only system-wide `ApplicationId == null` medicines via `RefreshableCache<Medicine>`, kept warm by `MedicineCacheWarmupService`; clinic-owned medicines always queried live) |
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
| IVitalPresetLookupItemRepository | VitalPresetLookupItemRepository |
| IClinicOnboardingRequestRepository | ClinicOnboardingRequestRepository |
| ISubscriptionPaymentRepository | SubscriptionPaymentRepository |
| IWorkflowTaskRepository | WorkflowTaskRepository |
| IPaymentWebhookEventRepository | PaymentWebhookEventRepository |

**Background services:** `MedicineCacheWarmupService` (`TenantCore.Infrastructure.Caching`) — hosted service, refreshes the three medicine-related caches on startup and every 30 minutes.

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
| IActionLogger | ActionLoggingService (business-action audit trail — Started/Completed/Failed rows in the `ActionLogs` Azure Table via `TenantCore.Logging`; fired by `ActionLoggingBehavior` MediatR pipeline behavior for commands implementing `IBusinessAction`; see `plan/action-audit-logging/PLAN.md`) |
| ICurrentUserContext | CurrentUserContext (resolves the authenticated user id from `IHttpContextAccessor` for `ActionLoggingBehavior` — Application layer never references `HttpContext` directly) |
| IPaymentGateway | RazorpayPaymentGateway (named HttpClient "RazorpayApi", Basic auth KeyId:KeySecret, `.AddStandardResilienceHandler()`) |
| IAuthProvisioningService | AuthProvisioningService (named HttpClient "AuthInternalApi", sends `X-Internal-Service-Key` — no bearer token) |

**Background services (added by clinic-trial-razorpay-subscriptions):** `WorkflowTaskProcessor` (polls every `Workflow:PollIntervalSeconds`, leases + dispatches `IWorkflowTask­Handler`s by `TaskType`, exponential backoff with jitter, ceiling `Workflow:MaxAttempts`), `PaymentReconciliationService` (every `Workflow:ReconciliationIntervalMinutes`, reconciles stuck `LinkCreated` payments directly against Razorpay + self-healing sweep for stuck non-terminal requests).

**Workflow task handlers (`IWorkflowTaskHandler`, Scoped, dispatched by `WorkflowTaskType`):** `SendEmailTaskHandler`, `CreatePaymentLinkTaskHandler`, `CancelPaymentLinkTaskHandler`, `ConfirmPaymentTaskHandler`, `ProvisionClinicTaskHandler`, `ActivateSubscriptionTaskHandler`, `ProcessWebhookEventTaskHandler`.

**New authentication scheme:** `"InternalService"` (`InternalServiceKeyAuthenticationHandler`, in `TenantCore.Api.Authentication` — not Infrastructure, since Infrastructure has no ASP.NET Core Authentication package reference) — validates `X-Internal-Service-Key` against `InternalApi:AdminPortalKey`, added additively alongside the existing JWT scheme in `Program.cs`/`ServiceCollectionExtensions.cs` (no pipeline reordering). Guards `api/internal/*` — used by the not-yet-built TenantCore.Admin portal.

---

## How to Verify This Snapshot

Run this mental check at the start of each plan session:

1. Read `ClinicDbContext.cs` — count the `DbSet<T>` lines
2. Compare count to "Total DbSets" above
3. If counts differ → update this file to match the actual DbContext before continuing
4. If counts match → trust this snapshot, skip reading the actual file again this session
