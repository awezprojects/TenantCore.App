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
| Obstetric History Print Control | — (extends ObstetricPrescriptionData JSON shape) | Executed | Per-item "print on prescription" flag (HistoryItemSelectionDto: Value + PrintOnPrescription) on the 7 history sections; tri-state "Print all" checkbox per section in HistoryChipField.razor (ShowPrintToggle/PrintFlags params); PrescriptionPrint.razor filters by the flag. Past Medical History and Family History moved out of the Gynecologist-only "Obstetric Details" panel into an always-visible "Medical History" section for every doctor; the other 5 sections (Menstrual History, Surgical History, Per Abdomen, Per Vaginum, Per Speculum) remain Gynecologist-only via the existing `_isGynecologist` check. No EF migration — same JSON-blob columns, backward-compatible with prescriptions saved in the old plain-string-array format (read as PrintOnPrescription=true) |
| Vitals Presets Lookup | VitalPresetLookupItem | Executed | Same global/per-clinic pattern as HistoryLookupItem (nullable ApplicationId — null = system default seen by every clinic, set = one clinic's own addition). Replaces the hardcoded 5-values-per-vital dictionary previously baked into PrescriptionForm.razor; migration seeds the same 35 values (7 vitals × 5 each) as global defaults so nothing changes visually. Quick-pick dropdown UX (previously prescription-only) now also added to the OPD Registration "New OPD Registration" modal. New `/reception/vitals-lookup` admin page (Reception nav panel) lets reception add/delete their clinic's own preset values; global defaults are read-only in the UI and protected at the handler level (delete requires `preset.ApplicationId == request.ApplicationId`, which a null global ApplicationId can never satisfy) |
| Pregnancy Tenure | PregnancyTenure | Executed | Lifecycle tracking (Active/Closed) for each pregnancy; EDD overdue tab; close-tenure workflow; blocks new LMP when overdue tenure is open |
| Patient LMP Tenure View | — (no new entity) | Executed | "View LMP" button on patient list; active status badge; full tenure history popup per patient |
| Doctor Fee Config | DoctorFeeConfig | Executed | Per-doctor visit fee set by admin; auto-populates OPD registration |
| Particulars | Particular, OpdParticular | Executed | Clinic-defined OPD service items + per-appointment applied particulars |
| OPD Payments | OpdPayment | Executed | 1:1 payment record per OPD; visit fee + particulars total + discount + status; auto-created on OPD registration |
| Expense Categories | ExpenseCategory | Executed | Admin-defined expense types for reception to record against |
| Expense Records | ExpenseRecord | Executed | Reception-recorded expense instances linked to category + session |
| Counter Sessions | CounterSession | Executed | Reception daily shift session tracking collections and expenses |
| Amount Handovers | AmountHandover | Executed | Handover workflow from reception to doctor/admin with acceptance status |
| Finance Reports | — (no new entity) | Executed | Day/week/month/date-range/reception-wise/expense-summary reports dashboard |
| Clinic Feature Flags | ClinicFeatureFlags | Executed | Extensible per-clinic settings/toggle area; ships with PrepaidOpdEnabled and BillingEnabled (both default true) |
| Billing Feature Flag | — (extends ClinicFeatureFlags) | Executed | BillingEnabled toggle (Clinic Admin, Settings > Feature Flags) gates OPD payments/particulars, finance reports, counter sessions, expenses, and amount handovers via BillingFeatureGuardMiddleware (403 on all eight route prefixes when off); NavMenu/OPD/IPD UI hides fee, payment and cash-management elements accordingly; CreateOpdRegistrationHandler skips the active-counter-session requirement and OpdPayment auto-creation entirely when billing is disabled so OPD registration keeps working |
| Prepaid OPD Fee Collection & Refund | — (extends OpdPayment) | Executed | When PrepaidOpdEnabled, visit fee auto-collected at booking; RefundDue/RefundStatus on OpdPayment track post-collection discount refunds; reception confirms refund via new endpoint; cancelled OPD registrations can be hard-deleted once refund-clear |
| Clinic Subscription & Access Gating | SubscriptionPlan, ClinicSubscription, SubscriptionAlertSetting | Executed | Every clinic needs an active subscription to be used — SubscriptionGuardMiddleware blocks all clinic-scoped API requests (402) when none is active; Blazor AuthorizedLayout renders the plan picker (Clinic Admin) or a locked screen (everyone else) in place of @Body. 4 seeded plans (Trial/Monthly/Quarterly/Yearly); Clinic Admin only may subscribe; trial is once-per-clinic forever (including a cancelled trial); renewal before expiry starts the day after the current term ends. Dashboard banner + top-bar pill show days remaining. Email sending and the twice-daily reminder sweep are OUT OF SCOPE — deferred to a separate Azure Function; SubscriptionAlertSetting ships now as configuration-only (seeded 10/5/2/0-day thresholds, admin CRUD endpoints) with no consumer in this repo yet |
| Centralized Logging Service | — (no ClinicDbContext entities — Azure Table Storage, separate store) | Executed | Independent `TenantCore.Logging` project (Azure Table Storage writer today, swappable later via config) behind `IErrorLogger` in `Application/Services`; reachable from any Application handler or Infrastructure service. Wired into `ExceptionHandlingMiddleware` (ApiErrorLogs) and `AuthApplicationService`'s outbound-call failures (ApiErrorLogs, App-side only — TenantCore.Auth repo untouched). Frontend crashes (Blazor `AppErrorBoundary` + JS `window.onerror`/`unhandledrejection`) POST to a new anonymous `LogsController` endpoint → `FrontendErrorLogs`, via a CQRS command/handler (`LogFrontendErrorCommand`) so the controller still only calls `sender.Send()`. No retry/queueing, no rate limiting on the anonymous endpoint — both flagged as future work |
| Medicine Search Caching | — (no new entity — `Medicine`, `MedicineType`, `MedicineDosageForm` all pre-existing) | Executed | `IMedicineRepository`/`IMedicineTypeRepository`/`IMedicineDosageFormRepository` now resolve to `Cached*Repository` decorators in Infrastructure DI. Revised 2026-09-13: the original `IMemoryCache`-with-TTL design caused the first request after a cold start (or any 30-min expiry) to block on a ~2.5 lakh-row query and time out. Replaced with `RefreshableCache<T>` (a singleton snapshot with no expiration, swapped via one atomic reference assignment) kept warm by `MedicineCacheWarmupService`, a `BackgroundService` that refreshes on startup and every 30 minutes thereafter — cache population/rebuild never happens on a live request. A request that arrives before the first warm-up completes falls back to a live, uncached query for that one call. Only system-wide medicines (`ApplicationId == null`) and the two lookup tables are cached — a clinic's own medicines are always queried live and merged in memory. `MedicineType`/`MedicineDosageForm` caches also get an immediate inline reload right after a successful write (small tables, cheap). Duplicate-name/duplicate-medicine guard methods (`GetByNameAsync`, `FindSimilarAsync`) and any fetch-then-mutate path (`GetByIdAsync` on the two lookups) deliberately bypass the cache and stay live. No Domain/Application/API/Shared/Web.Client changes — every page (MedicineList, PrescriptionForm, MedicineBundleList) already went through these repository interfaces, so caching is transparent. See `plan/medicine-search-caching/PLAN.md` |
| Action & Audit Logging | — (no ClinicDbContext entities — extends the `TenantCore.Logging` Azure Table Storage project with a new `ActionLogs` table) | Executed | Business-action audit trail distinct from the existing `IErrorLogger` (exception logging). New `IBusinessAction` marker interface (Application/Common) — a command opts in with one line (`: IBusinessAction` + an `ActionName` property); `ActionLoggingBehavior` (new MediatR pipeline behavior, registered after `ValidationBehavior`) picks it up automatically and writes a "Started" row before the handler runs and a "Completed"/"Failed" row after, tied together by a `CorrelationId` — no handler bodies touched. `IActionLogger`/`ActionLoggingService` (Application/Infrastructure) reuse the existing `IAppLogWriter`/`AzureTableLogWriter` write path from `centralized-logging-service`; `LogEntry` gained `CorrelationId`/`Status`/`DurationMs` fields and `LogCategory` gained `Action`. New `ICurrentUserContext`/`CurrentUserContext` resolves the authenticated user id via `IHttpContextAccessor` in Infrastructure so Application code never references `HttpContext` directly (per ADR-003). Instrumented 22 existing commands: `RegisterPatientCommand`, `DeletePatientCommand`, `CreateOpdRegistrationCommand`, `DeleteOpdRegistrationCommand`, `SubmitPrescriptionCommand`, `CreateAmountHandoverCommand`, `AcceptAmountHandoverCommand`, `DisputeAmountHandoverCommand`, `ProcessOpdRefundCommand`, `ApplyOpdDiscountCommand`, and 12 more `Delete*Command`s (VitalPreset, MedicineBundle, Particular, ExpenseRecord, ExpenseCategory, DoctorFeeConfig, Ward, Room, DosageRemark, Bed, Invitation, Application, MedicineDosageForm). Login (Admin/Doctor/Reception) and "flag actions" are explicitly out of scope — login lives in the separate TenantCore.Auth repo; no "flag" feature exists anywhere in TenantCore.App today. See `plan/action-audit-logging/PLAN.md` |

---

## Executed Features (via this workflow)

| Feature | Plan Date | Execute Date | New Entities | New DbSets | Files Created | Files Modified |
|---------|-----------|-------------|-------------|------------|--------------|----------------|
| obstetric-lmp-usg-template | 2026-06-16 | 2026-06-16 | ClinicUsgTemplate, UsgTemplateRow | ClinicUsgTemplates, UsgTemplateRows | 45 | 10 |
| lmp-edd-pregnancy-tenure | 2026-06-16 | 2026-06-16 | PregnancyTenure | PregnancyTenures | 18 | 8 |
| patient-lmp-tenure-view | 2026-06-16 | 2026-06-16 | — | — | 3 | 8 |
| remove-old-unused-pages | 2026-06-17 | 2026-06-17 | — | — | 0 (9 deleted) | 4 |
| auth-screens-redesign | 2026-06-18 | 2026-06-18 | — | — | 0 | 9 |
| clinic-admin-role-ui-fixes | 2026-06-18 | 2026-06-18 | — | — | 0 | 3 |
| role-authorization-hardening | 2026-06-18 | 2026-06-18 | — | — | 0 | 11 |
| remove-mudblazor | 2026-06-22 | 2026-06-22 | — | — | 0 | 10 |
| prepaid-opd-fee-collection | 2026-08-31 | 2026-08-31 | ClinicFeatureFlags | ClinicFeatureFlags | 19 | 18 |
| clinic-subscription-gating | 2026-09-04 | 2026-09-04 | SubscriptionPlan, ClinicSubscription, SubscriptionAlertSetting | SubscriptionPlans, ClinicSubscriptions, SubscriptionAlertSettings | 61 | 13 |
| centralized-logging-service | 2026-09-07 | 2026-09-07 | — (Azure Table Storage, not ClinicDbContext) | — | 16 | 7 |
| billing-feature-flag | 2026-09-08 | 2026-09-08 | — (extends ClinicFeatureFlags) | — | 4 | 15 |
| obstetric-history-print-control | 2026-09-08 | 2026-09-08 | — (extends ObstetricPrescriptionData JSON shape) | — | 4 | 6 |
| vitals-presets-lookup | 2026-09-09 | 2026-09-09 | VitalPresetLookupItem | VitalPresetLookupItems | 19 | 6 |
| medicine-search-caching | 2026-09-13 | 2026-09-13 | — (no new entities) | — (no new DbSets) | 6 (3 decorators + 3 test files, plus 1 new test project file) | 2 |
| action-audit-logging | 2026-09-13 | 2026-09-13 | — (no new entities — new `ActionLogs` Azure Table) | — (no new DbSets) | 6 (IBusinessAction, ICurrentUserContext, IActionLogger, ActionLoggingService, CurrentUserContext, ActionLoggingBehavior) + 3 test files | 28 (LogEntry, AzureTableLogWriter, AppLoggingOptions, LogCategory, Application/Infrastructure DependencyInjection, appsettings.json, 22 command records) |

---

## Planned (not yet executed)

| Feature | Plan Date | Plan File |
|---------|-----------|-----------|
| counter-expenses-management | 2026-06-18 | [plan/counter-expenses-management/PLAN.md](counter-expenses-management/PLAN.md) |
| auth-consolidation-monolith | 2026-07-21 | [plan/auth-consolidation-monolith/PLAN.md](auth-consolidation-monolith/PLAN.md) — merges TenantCore.Auth into App as internal projects (single deployment, single DB, two DbContexts) |

---

## Update Instructions (for execute command)

After executing a feature, append a row to "Executed Features" with:
- Feature name (kebab-case)
- Today's date for Execute Date
- New entity names added
- New DbSet names added
- Count of files created and modified
