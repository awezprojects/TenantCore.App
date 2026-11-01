# TenantCore.App — Current State Snapshot

**Last verified:** 2026-10-02 (subscription-billing-activation executed — OtSurgery=4 added to BillingItem, 8 billing-intent columns on ClinicOnboardingRequest, DeleteSubscriptionPlan command, SubscriptionPlans PAYG awareness, Usage per-item stat cards + Excel export)
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
| ClinicAccounts | ClinicAccount | Yes (at most one row per clinic; created on demand — absence means Active + public catalogue) |
| SubscriptionPlans | SubscriptionPlan | No (global catalogue; admin-managed, Code=Custom for admin-created packages) |
| ClinicPlanOffers | ClinicPlanOffer | Yes (per-clinic plan visibility / special price) |
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

**Total DbSets: 46**

**ClinicSubscription additions (clinic-trial-razorpay-subscriptions):** `SubscriptionPaymentId` (Guid?, unique filtered), `OnboardingRequestId` (Guid?, unique filtered), `PurchasedByUserId` (Guid?). `Create()` factory gained 4 new optional trailing params — existing callers unaffected.

**Subscription timeline (clinic-subscription-admin-controls) — read this before touching any subscription query:**
- `IsCurrentlyActive(utcNow)` now also requires `StartDate <= utcNow`. A term bought mid-term is **upcoming**, not current, and does NOT unlock the clinic.
- `GetActiveForClinicAsync` filters on StartDate too and orders by StartDate descending; `GetUpcomingForClinicAsync` and `GetCoverageEndAsync` are the timeline reads.
- **Every new term starts at coverage end (or now)** — never `EndDate.AddDays(1)`, which used to leave a 24-hour lock-out between terms.
- `ClinicSubscription` additions: `GrantedByAdminEmail`, `GrantReason`, `CancellationReason`; new `CreateAdminGrant`, `CancelUpcoming`, `Reschedule`, `IsUpcoming`.
- `SubscriptionPlan` additions: `IsPublic`; new `CreateCustom`, `UpdateDetails`, `Activate`, `Deactivate`. The unique index on `Code` is now **filtered** (`[Code] <> 5`) so any number of `Custom` plans can coexist while `GetByCodeAsync(Trial)` stays unambiguous.
- `SubscriptionPayment` additions: `InitiatedByAdminEmail`, `AmountReason`, `ClinicName`, `LastCheckAt`; new `CreateForAdminAssignment`, `RecordCheck`, `IsClinicLink`, `IsOpen`. `GetOpenRenewalForClinicAsync` was **renamed** to `GetOpenLinkForClinicAsync` (now covers Renewal *and* AdminAssigned).

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
| IClinicAccountRepository | ClinicAccountRepository |
| ISubscriptionPlanRepository | SubscriptionPlanRepository |
| IClinicPlanOfferRepository | ClinicPlanOfferRepository |
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

### Application-layer services (Scoped, registered in `TenantCore.Application/DependencyInjection.cs`)

| Interface | Implementation |
|-----------|---------------|
| IWorkflowEnqueuer | WorkflowEnqueuer |
| IOnboardingSelfHealer | OnboardingSelfHealer |
| IClinicPlanCatalog | ClinicPlanCatalog (the single source of truth for which plans a clinic may see and at what price — public catalogue + its own `ClinicPlanOffer`s, or offers only when `ClinicAccount.RestrictToOfferedPlans`. Used by the plan list, the renewal link and the admin price comparison; see `plan/clinic-subscription-admin-controls/PLAN.md`) |

**New feature area:** `Features/PlatformAdmin/` — 13 internal commands the TenantCore.Admin portal calls (suspend/reactivate a clinic, grant a plan, send a plan payment link, cancel an upcoming term, cancel an open link, plan visibility, plan offers, plan CRUD + **DeleteSubscriptionPlan** added by subscription-billing-activation). Every one is `IBusinessAction` + `IActionLogContext` and carries `AdminUserId`/`AdminEmail`; `ApplicationId` always comes from the route, never a body.

**subscription-billing-activation changes (2026-10-02):**
- `BillingItem` enum: `OtSurgery = 4` added (was Phase 3 placeholder, now live). `Usage.razor` and `UsagePrint.razor` labelled.
- `ClinicOnboardingRequest` entity: 8 nullable billing-intent columns (`ApprovalBillingMode`, `ApprovalRegistrationFee`, `ApprovalFreeTestDays`, `ApprovalOpdRate`, `ApprovalIpdAdmissionRate`, `ApprovalOtSurgeryRate`, `ApprovalIpdDischargeRate`, `ApprovalInitialWalletCredit`); `SetBillingApprovalIntent` and `ActivatePayAsYouGo` methods added. Migration: `20261002072507_AddBillingIntentToOnboardingRequest`.
- `ApproveClinicOnboardingCommand` + `Validator` + `Handler` extended with 8 optional billing fields; PAYG path bypasses payment/plan requirement and defers billing setup to `ActivateSubscriptionTaskHandler.ApplyBillingIntentAsync`.
- `DeleteSubscriptionPlanCommand` + `DeleteSubscriptionPlanHandler` + `DeleteSubscriptionPlanCommandValidator` added; endpoint: `DELETE api/internal/plans/{planId}`. Blocked if any clinic has active/upcoming subscription on that plan.
- `IClinicSubscriptionRepository.AnyActiveOrUpcomingForPlanAsync` added.
- `SubscriptionPlans.razor`: PAYG branch shows wallet balance, live rates, links; subscription history table inlined for both modes.
- `Usage.razor`: wallet balance stat card (PAYG only); per-item stat cards section added after main summary.
- Excel export: `POST api/usage-billing/usage/export/excel` (ClosedXML, 3-sheet workbook); `UsageExcelExporter` in `TenantCore.Api/Export/`; `ExportUsageRequest` DTO; `IUsageBillingApiClient.ExportExcelAsync`; `ccDownloadBinaryFile` JS helper.

**New authentication scheme:** `"InternalService"` (`InternalServiceKeyAuthenticationHandler`, in `TenantCore.Api.Authentication` — not Infrastructure, since Infrastructure has no ASP.NET Core Authentication package reference) — validates `X-Internal-Service-Key` against `InternalApi:AdminPortalKey`, added additively alongside the existing JWT scheme in `Program.cs`/`ServiceCollectionExtensions.cs` (no pipeline reordering). Guards `api/internal/*` — used by the not-yet-built TenantCore.Admin portal.

---

## How to Verify This Snapshot

Run this mental check at the start of each plan session:

1. Read `ClinicDbContext.cs` — count the `DbSet<T>` lines
2. Compare count to "Total DbSets" above
3. If counts differ → update this file to match the actual DbContext before continuing
4. If counts match → trust this snapshot, skip reading the actual file again this session
