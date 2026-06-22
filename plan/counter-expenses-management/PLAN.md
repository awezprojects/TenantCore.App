# Feature Plan: Counter Management & Expenses Management

**Repo:** TenantCore.App
**Date:** 2026-06-18
**Domain area:** Finance / Counter / Billing
**Status:** Approved — ready for execution

---

## Overview

This feature introduces a complete financial management system for the clinic. Admins configure per-doctor visit fees that auto-populate (read-only) on OPD registration; reception can add OPD particulars (injection, dressing, ECG, IV, sugar test, etc.) via an inline popup on the OPD list page, and click "Accept Amount" once payment is received from the patient. Doctors can apply a discount to an OPD appointment's total. Reception manages a daily counter session — recording clinic expenses within the session, then handing over the net amount to a doctor or admin who accepts it from their panel with a proper status. A separate Finance Dashboard page provides day-wise, weekly, monthly, date-range, reception-wise, and expense breakdown reports for full financial visibility.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Domain | 8 new entities, 8 repository interfaces, 3 new enums |
| Infrastructure | 8 EF Fluent API configurations, 8 repository implementations, 1 EF migration |
| Application | 26 commands, 18 queries, 44 handlers, 18 validators, 9 translators |
| API | 9 new controllers (~44 endpoints total) |
| Shared | ~32 new DTOs, 3 new enums |
| Blazor Client | 9 typed HTTP clients, 8 new pages (+.cs code-behind), 2 components, nav menu update |

---

## New Enums (`src/TenantCore.Shared/Enums/`)

| Enum | Values | Purpose |
|------|--------|---------|
| `PaymentStatus` | Pending, Received | OPD appointment payment state |
| `CounterSessionStatus` | Open, Closed | Reception daily session lifecycle |
| `HandoverStatus` | Pending, Accepted, Disputed | Amount handover lifecycle |

---

## Entity: DoctorFeeConfig

**Purpose:** Stores admin-configured visit fee per doctor. Auto-populates (read-only) on OPD registration form when doctor is selected.
**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, auto-generated |
| ApplicationId | Guid | FK to clinic — required |
| DoctorProfileId | Guid | FK to DoctorProfile — required; unique per (DoctorProfileId + ApplicationId) |
| VisitFee | decimal | required, >= 0, precision(18,2) |
| IsActive | bool | default true |

---

## Entity: Particular

**Purpose:** Clinic-defined OPD service items with a default amount (e.g., injection, dressing, ECG, IV, sugar test). Admin-managed; reception selects from these when adding particulars to an OPD.
**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| Name | string | required, maxlength(150) |
| DefaultAmount | decimal | required, >= 0, precision(18,2) |
| IsActive | bool | default true |

---

## Entity: OpdParticular

**Purpose:** A particular applied to a specific OPD appointment, with the actual amount charged (reception may override the default). Reception-managed.
**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| OpdRegistrationId | Guid | FK to OpdRegistration — required |
| ParticularId | Guid | FK to Particular — required |
| ParticularName | string | snapshot of name at time of adding, maxlength(150) |
| Amount | decimal | required, >= 0, precision(18,2) |

---

## Entity: OpdPayment

**Purpose:** 1:1 payment record for each OPD registration. Tracks visit fee, particulars total, discount, final amount, and payment status. Created automatically when an OPD is registered.
**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| OpdRegistrationId | Guid | FK to OpdRegistration — required; unique |
| VisitFee | decimal | precision(18,2), default 0 |
| ParticularsTotal | decimal | precision(18,2), default 0 — recalculated on particular add/update/remove |
| TotalAmount | decimal | precision(18,2) — VisitFee + ParticularsTotal, updated in handler |
| Discount | decimal | precision(18,2), default 0 — doctor-only |
| FinalAmount | decimal | precision(18,2) — TotalAmount - Discount, updated in handler |
| PaymentStatus | PaymentStatus | default Pending |
| AmountReceivedAt | DateTime? | nullable — set when reception accepts payment |
| ReceivedByUserId | Guid? | nullable — user ID of reception who accepted |
| CounterSessionId | Guid? | nullable FK to CounterSession — links payment to active session |

---

## Entity: ExpenseCategory

**Purpose:** Admin-defined expense types (e.g., "Medicines Purchased", "Equipment", "Utilities"). Reception selects from these when recording a clinic expense.
**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| Name | string | required, maxlength(150) |
| Description | string? | nullable, maxlength(500) |
| IsActive | bool | default true |

---

## Entity: ExpenseRecord

**Purpose:** A specific expense instance recorded by reception, linked to an ExpenseCategory and optionally a CounterSession.
**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| ExpenseCategoryId | Guid | FK to ExpenseCategory — required |
| CategoryName | string | snapshot of category name at time of recording, maxlength(150) |
| Amount | decimal | required, > 0, precision(18,2) |
| Notes | string? | nullable, maxlength(500) |
| RecordedByUserId | Guid | user ID of reception who recorded — required |
| RecordedAt | DateTime | set by handler to UtcNow |
| CounterSessionId | Guid? | nullable FK to CounterSession |

---

## Entity: CounterSession

**Purpose:** Reception daily shift session. Aggregates total collections and expenses for the session period. A handover is submitted when the session is closed.
**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| SessionDate | DateTime | represents the session day |
| OpenedByUserId | Guid | user ID of reception who opened — required |
| OpenedAt | DateTime | set at creation |
| ClosedAt | DateTime? | nullable — set when session is closed |
| Status | CounterSessionStatus | default Open |
| TotalCollected | decimal | precision(18,2), recalculated on close |
| TotalExpenses | decimal | precision(18,2), recalculated on close |
| NetAmount | decimal | precision(18,2) — TotalCollected - TotalExpenses, set on close |

---

## Entity: AmountHandover

**Purpose:** Handover record submitted by reception to a doctor or admin after session close. Tracks acceptance status.
**Tenant-scoped:** Yes
**Base class:** AuditableEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| ApplicationId | Guid | required |
| CounterSessionId | Guid | FK to CounterSession — required |
| HandedOverByUserId | Guid | reception user ID — required |
| HandedOverToUserId | Guid | doctor/admin user ID — required |
| Amount | decimal | required, >= 0, precision(18,2) |
| Notes | string? | nullable, maxlength(500) |
| Status | HandoverStatus | default Pending |
| HandedOverAt | DateTime | set at creation |
| AcceptedAt | DateTime? | nullable — set when accepted |
| ResolutionNotes | string? | nullable, maxlength(500) — filled on dispute |

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

#### Enums
| File | Purpose |
|------|---------|
| `Enums/PaymentStatus.cs` | Pending = 0, Received = 1 |
| `Enums/CounterSessionStatus.cs` | Open = 0, Closed = 1 |
| `Enums/HandoverStatus.cs` | Pending = 0, Accepted = 1, Disputed = 2 |

#### Doctor Fee Config DTOs
| File | Purpose |
|------|---------|
| `Dtos/DoctorFeeConfigDto.cs` | Full read response: Id, DoctorProfileId, DoctorName, VisitFee, IsActive |
| `Dtos/DoctorFeeConfigSummaryDto.cs` | Lean list: Id, DoctorProfileId, DoctorName, VisitFee, IsActive |
| `Dtos/CreateDoctorFeeConfigRequest.cs` | POST body: DoctorProfileId, VisitFee |
| `Dtos/UpdateDoctorFeeConfigRequest.cs` | PUT body: VisitFee, IsActive |

#### Particular DTOs
| File | Purpose |
|------|---------|
| `Dtos/ParticularDto.cs` | Full read response: Id, Name, DefaultAmount, IsActive |
| `Dtos/ParticularSummaryDto.cs` | Lean list: Id, Name, DefaultAmount, IsActive |
| `Dtos/CreateParticularRequest.cs` | POST body: Name, DefaultAmount |
| `Dtos/UpdateParticularRequest.cs` | PUT body: Name, DefaultAmount, IsActive |

#### OPD Particular DTOs
| File | Purpose |
|------|---------|
| `Dtos/OpdParticularDto.cs` | Full record: Id, OpdRegistrationId, ParticularId, ParticularName, Amount |
| `Dtos/AddOpdParticularRequest.cs` | POST body: OpdRegistrationId, ParticularId, Amount |
| `Dtos/UpdateOpdParticularRequest.cs` | PUT body: Amount |

#### OPD Payment DTOs
| File | Purpose |
|------|---------|
| `Dtos/OpdPaymentDto.cs` | Full payment record: all fees, totals, discount, FinalAmount, PaymentStatus, timestamps |
| `Dtos/AcceptOpdPaymentRequest.cs` | POST body: OpdRegistrationId, ReceivedByUserId (Guid), CounterSessionId (optional) |
| `Dtos/ApplyOpdDiscountRequest.cs` | POST body: OpdRegistrationId, Discount |

#### Expense Category DTOs
| File | Purpose |
|------|---------|
| `Dtos/ExpenseCategoryDto.cs` | Full read response: Id, Name, Description, IsActive |
| `Dtos/ExpenseCategorySummaryDto.cs` | Lean list: Id, Name, IsActive |
| `Dtos/CreateExpenseCategoryRequest.cs` | POST body: Name, Description |
| `Dtos/UpdateExpenseCategoryRequest.cs` | PUT body: Name, Description, IsActive |

#### Expense Record DTOs
| File | Purpose |
|------|---------|
| `Dtos/ExpenseRecordDto.cs` | Full record: Id, CategoryName, Amount, Notes, RecordedByUserId, RecordedAt |
| `Dtos/ExpenseRecordSummaryDto.cs` | Lean list: Id, CategoryName, Amount, RecordedAt |
| `Dtos/CreateExpenseRecordRequest.cs` | POST body: ExpenseCategoryId, Amount, Notes, RecordedByUserId (Guid), CounterSessionId (optional) |

#### Counter Session DTOs
| File | Purpose |
|------|---------|
| `Dtos/CounterSessionDto.cs` | Full session: Id, SessionDate, OpenedByUserId, OpenedAt, ClosedAt, Status, TotalCollected, TotalExpenses, NetAmount |
| `Dtos/CounterSessionSummaryDto.cs` | Lean: Id, SessionDate, Status, OpenedByUserId, NetAmount |
| `Dtos/OpenCounterSessionRequest.cs` | POST body: SessionDate |
| `Dtos/CloseCounterSessionRequest.cs` | POST body: (empty — session ID is in route) |

#### Amount Handover DTOs
| File | Purpose |
|------|---------|
| `Dtos/AmountHandoverDto.cs` | Full record: all fields including Status, timestamps, ResolutionNotes |
| `Dtos/AmountHandoverSummaryDto.cs` | Lean: Id, Amount, Status, HandedOverAt, HandedOverByUserId |
| `Dtos/CreateAmountHandoverRequest.cs` | POST body: CounterSessionId, HandedOverByUserId (Guid), HandedOverToUserId (Guid), Amount, Notes |
| `Dtos/ResolveAmountHandoverRequest.cs` | POST body for accept/dispute: ResolutionNotes (optional) |

#### Finance Report DTOs
| File | Purpose |
|------|---------|
| `Dtos/FinanceDashboardSummaryDto.cs` | Dashboard: today's TotalCollected, TotalExpenses, NetAmount, ActiveSession info |
| `Dtos/DailyCollectionReportDto.cs` | Date, list of payment line items (patient name, visit fee, particulars, final amount), grand total |
| `Dtos/WeeklyCollectionReportDto.cs` | WeekStart, per-day totals (DayOfWeek + amount), grand total |
| `Dtos/MonthlyCollectionReportDto.cs` | Year, Month, per-day totals, per-week subtotals, grand total |
| `Dtos/DateRangeCollectionReportDto.cs` | From, To, per-day totals, grand total |
| `Dtos/ReceptionWiseReportDto.cs` | UserId, total sessions, TotalCollected, TotalExpenses per reception staff |
| `Dtos/ExpenseSummaryReportDto.cs` | Per-category totals (CategoryName, Amount), grand total |

---

### Domain Layer (`src/TenantCore.Domain/`)

#### Entities
| File | Purpose |
|------|---------|
| `Entities/DoctorFeeConfig.cs` | Inherits AuditableEntity — per-doctor visit fee |
| `Entities/Particular.cs` | Inherits AuditableEntity — clinic OPD service item |
| `Entities/OpdParticular.cs` | Inherits AuditableEntity — particular applied to OPD visit |
| `Entities/OpdPayment.cs` | Inherits AuditableEntity — payment record per OPD visit |
| `Entities/ExpenseCategory.cs` | Inherits AuditableEntity — expense type |
| `Entities/ExpenseRecord.cs` | Inherits AuditableEntity — recorded expense instance |
| `Entities/CounterSession.cs` | Inherits AuditableEntity — reception shift session |
| `Entities/AmountHandover.cs` | Inherits AuditableEntity — handover record |

#### Repository Interfaces
| File | Purpose |
|------|---------|
| `Interfaces/IDoctorFeeConfigRepository.cs` | Extends IRepository — adds `GetByDoctorProfileIdAsync(doctorProfileId, applicationId)` |
| `Interfaces/IParticularRepository.cs` | Extends IRepository — adds `GetActiveAsync(applicationId)` |
| `Interfaces/IOpdParticularRepository.cs` | Extends IRepository — adds `GetByOpdRegistrationIdAsync(opdId, applicationId)` and `GetTotalByOpdRegistrationIdAsync(opdId, applicationId)` returning decimal sum |
| `Interfaces/IOpdPaymentRepository.cs` | Extends IRepository — adds `GetByOpdRegistrationIdAsync(opdId, applicationId)`, `GetBySessionIdAsync(sessionId, applicationId)`, `GetByDateRangeAsync(from, to, applicationId)` |
| `Interfaces/IExpenseCategoryRepository.cs` | Extends IRepository — adds `GetActiveAsync(applicationId)` |
| `Interfaces/IExpenseRecordRepository.cs` | Extends IRepository — adds `GetByDateRangeAsync(from, to, applicationId)`, `GetBySessionIdAsync(sessionId, applicationId)` |
| `Interfaces/ICounterSessionRepository.cs` | Extends IRepository — adds `GetActiveSessionAsync(applicationId)`, `GetByDateAsync(date, applicationId)` |
| `Interfaces/IAmountHandoverRepository.cs` | Extends IRepository — adds `GetPendingAsync(applicationId)`, `GetBySessionIdAsync(sessionId, applicationId)` |

---

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

#### EF Configurations (`Persistence/Configurations/`)
| File | Purpose |
|------|---------|
| `DoctorFeeConfigConfiguration.cs` | PK, FK to DoctorProfile, unique index on (DoctorProfileId + ApplicationId), decimal(18,2) for VisitFee |
| `ParticularConfiguration.cs` | PK, required Name maxlength(150), decimal(18,2) for DefaultAmount |
| `OpdParticularConfiguration.cs` | PK, FK to OpdRegistration, FK to Particular, decimal(18,2) for Amount, maxlength(150) for ParticularName |
| `OpdPaymentConfiguration.cs` | PK, unique FK to OpdRegistration, decimal(18,2) on all monetary fields, enum conversion for PaymentStatus, nullable FK to CounterSession |
| `ExpenseCategoryConfiguration.cs` | PK, required Name maxlength(150), optional Description maxlength(500) |
| `ExpenseRecordConfiguration.cs` | PK, FK to ExpenseCategory, decimal(18,2) for Amount, nullable FK to CounterSession |
| `CounterSessionConfiguration.cs` | PK, enum conversion for Status, decimal(18,2) on all monetary fields |
| `AmountHandoverConfiguration.cs` | PK, FK to CounterSession, enum conversion for Status, decimal(18,2) for Amount, maxlength(500) for Notes/ResolutionNotes |

#### Repositories (`Repositories/`)
| File | Purpose |
|------|---------|
| `DoctorFeeConfigRepository.cs` | Extends ClinicRepository — implements `GetByDoctorProfileIdAsync` (filter by doctorProfileId + applicationId, AsNoTracking) |
| `ParticularRepository.cs` | Extends ClinicRepository — implements `GetActiveAsync` (IsActive == true + applicationId filter, AsNoTracking) |
| `OpdParticularRepository.cs` | Extends ClinicRepository — implements `GetByOpdRegistrationIdAsync` and `GetTotalByOpdRegistrationIdAsync` (sum of Amount) |
| `OpdPaymentRepository.cs` | Extends ClinicRepository — implements by-OPD (single), by-session (list), by-date-range (list with received status filter) |
| `ExpenseCategoryRepository.cs` | Extends ClinicRepository — implements `GetActiveAsync` |
| `ExpenseRecordRepository.cs` | Extends ClinicRepository — implements date range and session queries |
| `CounterSessionRepository.cs` | Extends ClinicRepository — implements `GetActiveSessionAsync` (Status == Open), `GetByDateAsync` |
| `AmountHandoverRepository.cs` | Extends ClinicRepository — implements `GetPendingAsync` (Status == Pending), `GetBySessionIdAsync` |

---

### Application Layer (`src/TenantCore.Application/Features/`)

#### Group A: DoctorFeeConfigs (`Features/DoctorFeeConfigs/`)

**Commands:**
| File | Purpose |
|------|---------|
| `Commands/CreateDoctorFeeConfigCommand.cs` | Carries DoctorProfileId, VisitFee, ApplicationId → IRequest\<Guid\> |
| `Commands/UpdateDoctorFeeConfigCommand.cs` | Carries Id, VisitFee, IsActive, ApplicationId → IRequest\<DoctorFeeConfigDto\> |
| `Commands/DeleteDoctorFeeConfigCommand.cs` | Carries Id, ApplicationId → IRequest |

**Queries:**
| File | Purpose |
|------|---------|
| `Queries/GetDoctorFeeConfigsQuery.cs` | Carries ApplicationId → IRequest\<IEnumerable\<DoctorFeeConfigSummaryDto\>\> |
| `Queries/GetDoctorFeeConfigByIdQuery.cs` | Carries Id, ApplicationId → IRequest\<DoctorFeeConfigDto\> |
| `Queries/GetDoctorFeeConfigByDoctorIdQuery.cs` | Carries DoctorProfileId, ApplicationId → IRequest\<DoctorFeeConfigDto?\> — returns null if not configured (no exception) |

**Handlers:**
| File | Purpose |
|------|---------|
| `Handlers/CreateDoctorFeeConfigHandler.cs` | Checks no duplicate for same DoctorProfileId + ApplicationId (throws InvalidOperationException if exists), creates, returns Id |
| `Handlers/UpdateDoctorFeeConfigHandler.cs` | Fetches by Id + ApplicationId (throws EntityNotFoundException), updates VisitFee + IsActive, saves |
| `Handlers/DeleteDoctorFeeConfigHandler.cs` | Fetches or throws EntityNotFoundException, deletes, saves |
| `Handlers/GetDoctorFeeConfigsHandler.cs` | Returns mapped summary DTOs for tenant |
| `Handlers/GetDoctorFeeConfigByIdHandler.cs` | Returns full DTO or throws EntityNotFoundException |
| `Handlers/GetDoctorFeeConfigByDoctorIdHandler.cs` | Returns full DTO or null — safe for OPD form auto-populate |

**Validators:**
| File | Purpose |
|------|---------|
| `Validators/CreateDoctorFeeConfigCommandValidator.cs` | DoctorProfileId NotEmpty, VisitFee >= 0, ApplicationId NotEmpty |
| `Validators/UpdateDoctorFeeConfigCommandValidator.cs` | Id NotEmpty, VisitFee >= 0, ApplicationId NotEmpty |

**Translators:**
| File | Purpose |
|------|---------|
| `Translators/DoctorFeeConfigTranslator.cs` | Static: ToEntity(command, applicationId), ToDto(entity), ToSummaryDto(entity) |

---

#### Group B: Particulars (`Features/Particulars/`)

**Commands:**
| File | Purpose |
|------|---------|
| `Commands/CreateParticularCommand.cs` | Carries Name, DefaultAmount, ApplicationId → IRequest\<Guid\> |
| `Commands/UpdateParticularCommand.cs` | Carries Id, Name, DefaultAmount, IsActive, ApplicationId → IRequest\<ParticularDto\> |
| `Commands/DeleteParticularCommand.cs` | Carries Id, ApplicationId → IRequest |

**Queries:**
| File | Purpose |
|------|---------|
| `Queries/GetParticularsQuery.cs` | Carries ApplicationId → IRequest\<IEnumerable\<ParticularSummaryDto\>\> |
| `Queries/GetParticularByIdQuery.cs` | Carries Id, ApplicationId → IRequest\<ParticularDto\> |

**Handlers (5):** CreateParticularHandler, UpdateParticularHandler, DeleteParticularHandler, GetParticularsHandler, GetParticularByIdHandler — standard CRUD pattern

**Validators:**
| File | Purpose |
|------|---------|
| `Validators/CreateParticularCommandValidator.cs` | Name NotEmpty MaxLength(150), DefaultAmount >= 0, ApplicationId NotEmpty |
| `Validators/UpdateParticularCommandValidator.cs` | Id NotEmpty, Name NotEmpty MaxLength(150), DefaultAmount >= 0 |

**Translators:**
| File | Purpose |
|------|---------|
| `Translators/ParticularTranslator.cs` | Static: ToEntity, ToDto, ToSummaryDto |

---

#### Group C: OpdParticulars (`Features/OpdParticulars/`)

**Commands:**
| File | Purpose |
|------|---------|
| `Commands/AddOpdParticularCommand.cs` | Carries OpdRegistrationId, ParticularId, Amount, ApplicationId → IRequest\<OpdParticularDto\> |
| `Commands/UpdateOpdParticularCommand.cs` | Carries Id, Amount, ApplicationId → IRequest\<OpdParticularDto\> |
| `Commands/RemoveOpdParticularCommand.cs` | Carries Id, OpdRegistrationId, ApplicationId → IRequest |

**Queries:**
| File | Purpose |
|------|---------|
| `Queries/GetOpdParticularsQuery.cs` | Carries OpdRegistrationId, ApplicationId → IRequest\<IEnumerable\<OpdParticularDto\>\> |

**Handlers:**
| File | Purpose |
|------|---------|
| `Handlers/AddOpdParticularHandler.cs` | Fetches Particular for name snapshot, adds OpdParticular, recalculates OpdPayment.ParticularsTotal and TotalAmount via IOpdPaymentRepository |
| `Handlers/UpdateOpdParticularHandler.cs` | Updates Amount, recalculates OpdPayment totals |
| `Handlers/RemoveOpdParticularHandler.cs` | Deletes OpdParticular, recalculates OpdPayment totals |
| `Handlers/GetOpdParticularsHandler.cs` | Returns list for OPD ID |

**Validators:**
| File | Purpose |
|------|---------|
| `Validators/AddOpdParticularCommandValidator.cs` | OpdRegistrationId NotEmpty, ParticularId NotEmpty, Amount >= 0, ApplicationId NotEmpty |
| `Validators/UpdateOpdParticularCommandValidator.cs` | Id NotEmpty, Amount >= 0, ApplicationId NotEmpty |

**Translators:**
| File | Purpose |
|------|---------|
| `Translators/OpdParticularTranslator.cs` | Static: ToEntity(command, particularName), ToDto(entity) |

---

#### Group D: OpdPayments (`Features/OpdPayments/`)

**Commands:**
| File | Purpose |
|------|---------|
| `Commands/EnsureOpdPaymentCommand.cs` | Carries OpdRegistrationId, DoctorProfileId, ApplicationId → IRequest\<Guid\> — idempotent: creates OpdPayment with VisitFee from DoctorFeeConfig if exists, else 0; no-op if already exists |
| `Commands/AcceptOpdPaymentCommand.cs` | Carries OpdRegistrationId, ReceivedByUserId (Guid), CounterSessionId (optional), ApplicationId → IRequest\<OpdPaymentDto\> |
| `Commands/ApplyOpdDiscountCommand.cs` | Carries OpdRegistrationId, Discount, ApplicationId → IRequest\<OpdPaymentDto\> — doctor-only at controller level |

**Queries:**
| File | Purpose |
|------|---------|
| `Queries/GetOpdPaymentByOpdIdQuery.cs` | Carries OpdRegistrationId, ApplicationId → IRequest\<OpdPaymentDto?\> |

**Handlers:**
| File | Purpose |
|------|---------|
| `Handlers/EnsureOpdPaymentHandler.cs` | Checks if OpdPayment exists for OpdRegistrationId; if not, fetches DoctorFeeConfig for doctor (may be null), creates OpdPayment with VisitFee or 0 |
| `Handlers/AcceptOpdPaymentHandler.cs` | Fetches payment, throws InvalidOperationException if already Received; sets Status=Received, AmountReceivedAt=UtcNow, ReceivedByUserId, CounterSessionId; saves |
| `Handlers/ApplyOpdDiscountHandler.cs` | Fetches payment, throws InvalidOperationException if Discount > TotalAmount; updates Discount, recalculates FinalAmount; saves |
| `Handlers/GetOpdPaymentByOpdIdHandler.cs` | Returns OpdPaymentDto or null |

**Validators:**
| File | Purpose |
|------|---------|
| `Validators/AcceptOpdPaymentCommandValidator.cs` | OpdRegistrationId NotEmpty, ApplicationId NotEmpty |
| `Validators/ApplyOpdDiscountCommandValidator.cs` | OpdRegistrationId NotEmpty, Discount >= 0, ApplicationId NotEmpty |

**Translators:**
| File | Purpose |
|------|---------|
| `Translators/OpdPaymentTranslator.cs` | Static: ToEntity(command), ToDto(entity) |

---

#### Group E: ExpenseCategories (`Features/ExpenseCategories/`)

Standard CRUD. 3 commands, 2 queries, 5 handlers, 2 validators, 1 translator.

**Commands:** CreateExpenseCategoryCommand (Name, Description, ApplicationId → IRequest\<Guid\>), UpdateExpenseCategoryCommand (Id, Name, Description, IsActive, ApplicationId → IRequest\<ExpenseCategoryDto\>), DeleteExpenseCategoryCommand (Id, ApplicationId → IRequest)

**Queries:** GetExpenseCategoriesQuery (ApplicationId → IRequest\<IEnumerable\<ExpenseCategorySummaryDto\>\>), GetExpenseCategoryByIdQuery (Id, ApplicationId → IRequest\<ExpenseCategoryDto\>)

**Handlers (5):** CreateExpenseCategoryHandler, UpdateExpenseCategoryHandler, DeleteExpenseCategoryHandler, GetExpenseCategoriesHandler, GetExpenseCategoryByIdHandler — standard CRUD pattern

**Validators:**
| File | Purpose |
|------|---------|
| `Validators/CreateExpenseCategoryCommandValidator.cs` | Name NotEmpty MaxLength(150), ApplicationId NotEmpty |
| `Validators/UpdateExpenseCategoryCommandValidator.cs` | Id NotEmpty, Name NotEmpty MaxLength(150) |

**Translators:**
| File | Purpose |
|------|---------|
| `Translators/ExpenseCategoryTranslator.cs` | Static: ToEntity, ToDto, ToSummaryDto |

---

#### Group F: ExpenseRecords (`Features/ExpenseRecords/`)

**Commands:**
| File | Purpose |
|------|---------|
| `Commands/CreateExpenseRecordCommand.cs` | Carries ExpenseCategoryId, Amount, Notes, RecordedByUserId (Guid), CounterSessionId (optional), ApplicationId → IRequest\<Guid\> |
| `Commands/DeleteExpenseRecordCommand.cs` | Carries Id, ApplicationId → IRequest — admin-only at controller level |

**Queries:**
| File | Purpose |
|------|---------|
| `Queries/GetExpenseRecordsQuery.cs` | Carries ApplicationId, From (optional), To (optional) → IRequest\<IEnumerable\<ExpenseRecordSummaryDto\>\> |
| `Queries/GetExpenseRecordByIdQuery.cs` | Carries Id, ApplicationId → IRequest\<ExpenseRecordDto\> |

**Handlers:**
| File | Purpose |
|------|---------|
| `Handlers/CreateExpenseRecordHandler.cs` | Fetches ExpenseCategory for name snapshot and to verify IsActive (throws InvalidOperationException if inactive); sets RecordedAt=UtcNow; saves; returns Id |
| `Handlers/DeleteExpenseRecordHandler.cs` | Fetches or throws EntityNotFoundException; deletes; saves |
| `Handlers/GetExpenseRecordsHandler.cs` | Returns summary DTOs, applies date range filter if provided |
| `Handlers/GetExpenseRecordByIdHandler.cs` | Returns full DTO or throws EntityNotFoundException |

**Validators:**
| File | Purpose |
|------|---------|
| `Validators/CreateExpenseRecordCommandValidator.cs` | ExpenseCategoryId NotEmpty, Amount GreaterThan(0), RecordedByUserId NotEmpty, ApplicationId NotEmpty |

**Translators:**
| File | Purpose |
|------|---------|
| `Translators/ExpenseRecordTranslator.cs` | Static: ToEntity(command, categoryName), ToDto(entity), ToSummaryDto(entity) |

---

#### Group G: CounterSessions (`Features/CounterSessions/`)

**Commands:**
| File | Purpose |
|------|---------|
| `Commands/OpenCounterSessionCommand.cs` | Carries OpenedByUserId (Guid), SessionDate, ApplicationId → IRequest\<Guid\> |
| `Commands/CloseCounterSessionCommand.cs` | Carries Id, ApplicationId → IRequest\<CounterSessionDto\> |

**Queries:**
| File | Purpose |
|------|---------|
| `Queries/GetCounterSessionsQuery.cs` | Carries ApplicationId → IRequest\<IEnumerable\<CounterSessionSummaryDto\>\> |
| `Queries/GetActiveCounterSessionQuery.cs` | Carries ApplicationId → IRequest\<CounterSessionDto?\> |
| `Queries/GetCounterSessionByIdQuery.cs` | Carries Id, ApplicationId → IRequest\<CounterSessionDto\> |

**Handlers:**
| File | Purpose |
|------|---------|
| `Handlers/OpenCounterSessionHandler.cs` | Checks no Open session already exists for tenant (throws InvalidOperationException if one exists); creates new session with Status=Open, OpenedAt=UtcNow; saves; returns Id |
| `Handlers/CloseCounterSessionHandler.cs` | Fetches session or throws EntityNotFoundException; throws InvalidOperationException if already Closed; aggregates TotalCollected = sum of OpdPayments with CounterSessionId=this session; aggregates TotalExpenses = sum of ExpenseRecords with CounterSessionId=this session; sets NetAmount, ClosedAt=UtcNow, Status=Closed; saves; returns DTO |
| `Handlers/GetCounterSessionsHandler.cs` | Returns summary list for tenant |
| `Handlers/GetActiveCounterSessionHandler.cs` | Returns active session DTO or null |
| `Handlers/GetCounterSessionByIdHandler.cs` | Returns full DTO or throws EntityNotFoundException |

**Validators:**
| File | Purpose |
|------|---------|
| `Validators/OpenCounterSessionCommandValidator.cs` | OpenedByUserId NotEmpty, ApplicationId NotEmpty |
| `Validators/CloseCounterSessionCommandValidator.cs` | Id NotEmpty, ApplicationId NotEmpty |

**Translators:**
| File | Purpose |
|------|---------|
| `Translators/CounterSessionTranslator.cs` | Static: ToEntity(command), ToDto(entity) |

---

#### Group H: AmountHandovers (`Features/AmountHandovers/`)

**Commands:**
| File | Purpose |
|------|---------|
| `Commands/CreateAmountHandoverCommand.cs` | Carries CounterSessionId, HandedOverByUserId (Guid), HandedOverToUserId (Guid), Amount, Notes, ApplicationId → IRequest\<Guid\> |
| `Commands/AcceptAmountHandoverCommand.cs` | Carries Id, ApplicationId → IRequest\<AmountHandoverDto\> |
| `Commands/DisputeAmountHandoverCommand.cs` | Carries Id, ResolutionNotes, ApplicationId → IRequest\<AmountHandoverDto\> |

**Queries:**
| File | Purpose |
|------|---------|
| `Queries/GetPendingAmountHandoversQuery.cs` | Carries ApplicationId → IRequest\<IEnumerable\<AmountHandoverSummaryDto\>\> |
| `Queries/GetAmountHandoversBySessionQuery.cs` | Carries CounterSessionId, ApplicationId → IRequest\<IEnumerable\<AmountHandoverSummaryDto\>\> |
| `Queries/GetAmountHandoverByIdQuery.cs` | Carries Id, ApplicationId → IRequest\<AmountHandoverDto\> |

**Handlers:**
| File | Purpose |
|------|---------|
| `Handlers/CreateAmountHandoverHandler.cs` | Validates CounterSession exists + belongs to tenant; creates with Status=Pending, HandedOverAt=UtcNow; saves; returns Id |
| `Handlers/AcceptAmountHandoverHandler.cs` | Fetches or throws EntityNotFoundException; throws InvalidOperationException if Status != Pending; sets Status=Accepted, AcceptedAt=UtcNow; saves; returns DTO |
| `Handlers/DisputeAmountHandoverHandler.cs` | Fetches or throws EntityNotFoundException; throws InvalidOperationException if Status != Pending; sets Status=Disputed, ResolutionNotes; saves; returns DTO |
| `Handlers/GetPendingAmountHandoversHandler.cs` | Returns pending handovers for tenant |
| `Handlers/GetAmountHandoversBySessionHandler.cs` | Returns handovers for session ID |
| `Handlers/GetAmountHandoverByIdHandler.cs` | Returns full DTO or throws EntityNotFoundException |

**Validators:**
| File | Purpose |
|------|---------|
| `Validators/CreateAmountHandoverCommandValidator.cs` | CounterSessionId NotEmpty, HandedOverByUserId NotEmpty, HandedOverToUserId NotEmpty, Amount >= 0, ApplicationId NotEmpty |
| `Validators/AcceptAmountHandoverCommandValidator.cs` | Id NotEmpty, ApplicationId NotEmpty |

**Translators:**
| File | Purpose |
|------|---------|
| `Translators/AmountHandoverTranslator.cs` | Static: ToEntity(command), ToDto(entity) |

---

#### Group I: FinanceReports (`Features/FinanceReports/`)

All read-only. No commands, no validators, no translator. Handlers use `IOpdPaymentRepository`, `IExpenseRecordRepository`, `ICounterSessionRepository`, and `IAmountHandoverRepository`.

**Queries:**
| File | Purpose |
|------|---------|
| `Queries/GetFinanceDashboardSummaryQuery.cs` | Carries Date, ApplicationId → IRequest\<FinanceDashboardSummaryDto\> |
| `Queries/GetDailyCollectionReportQuery.cs` | Carries Date, ApplicationId → IRequest\<DailyCollectionReportDto\> |
| `Queries/GetWeeklyCollectionReportQuery.cs` | Carries WeekStartDate, ApplicationId → IRequest\<WeeklyCollectionReportDto\> |
| `Queries/GetMonthlyCollectionReportQuery.cs` | Carries Year, Month, ApplicationId → IRequest\<MonthlyCollectionReportDto\> |
| `Queries/GetDateRangeCollectionReportQuery.cs` | Carries From, To, ApplicationId → IRequest\<DateRangeCollectionReportDto\> |
| `Queries/GetReceptionWiseReportQuery.cs` | Carries From, To, ApplicationId → IRequest\<IEnumerable\<ReceptionWiseReportDto\>\> |
| `Queries/GetExpenseSummaryReportQuery.cs` | Carries From, To, ApplicationId → IRequest\<ExpenseSummaryReportDto\> |

**Handlers (7):** GetFinanceDashboardSummaryHandler, GetDailyCollectionReportHandler, GetWeeklyCollectionReportHandler, GetMonthlyCollectionReportHandler, GetDateRangeCollectionReportHandler, GetReceptionWiseReportHandler, GetExpenseSummaryReportHandler — each fetches from relevant repositories and assembles the DTO

---

### API Layer (`src/TenantCore.Api/Controllers/`)

| File | Purpose |
|------|---------|
| `DoctorFeeConfigsController.cs` | CRUD + by-doctor lookup — inherits ClinicControllerBase |
| `ParticularsController.cs` | CRUD for OPD service particulars |
| `OpdParticularsController.cs` | Add/update/remove OPD particulars; list by OPD ID |
| `OpdPaymentsController.cs` | Get payment by OPD ID; accept payment; apply discount |
| `ExpenseCategoriesController.cs` | CRUD for expense categories |
| `ExpenseRecordsController.cs` | Create/delete expense records; list with date filter; get by ID |
| `CounterSessionsController.cs` | Open/close sessions; get active; list |
| `AmountHandoversController.cs` | Create; accept; dispute; list pending; list by session |
| `FinanceReportsController.cs` | All 7 report query endpoints |

---

### Blazor Client (`src/TenantCore.Web.Client/`)

#### Typed HTTP Clients (`Clients/`)
| File | Purpose |
|------|---------|
| `DoctorFeeConfigClient.cs` | GetAll, GetById, GetByDoctorId, Create, Update, Delete |
| `ParticularClient.cs` | GetAll, GetById, Create, Update, Delete |
| `OpdParticularClient.cs` | GetByOpdId, Add, Update, Remove |
| `OpdPaymentClient.cs` | GetByOpdId, Accept, ApplyDiscount |
| `ExpenseCategoryClient.cs` | GetAll, GetById, Create, Update, Delete |
| `ExpenseRecordClient.cs` | GetAll (with date filter), GetById, Create, Delete |
| `CounterSessionClient.cs` | GetAll, GetActive, GetById, Open, Close |
| `AmountHandoverClient.cs` | GetPending, GetBySession, GetById, Create, Accept, Dispute |
| `FinanceReportClient.cs` | Dashboard, Daily, Weekly, Monthly, DateRange, ReceptionWise, ExpenseSummary |

#### Pages (`Pages/Finance/`)
| File | Purpose |
|------|---------|
| `DoctorFeeConfig.razor` + `DoctorFeeConfig.razor.cs` | Admin page — list of doctors with their configured visit fees; inline add/edit form; shows 0/unset for unconfigured doctors |
| `Particulars.razor` + `Particulars.razor.cs` | Admin page — CRUD table for OPD service items (name + default amount + active toggle) |
| `ExpenseCategories.razor` + `ExpenseCategories.razor.cs` | Admin page — CRUD table for expense category types |
| `ExpenseRecords.razor` + `ExpenseRecords.razor.cs` | Reception page — date-filtered list of expense records; add expense dialog (category dropdown + amount + notes) |
| `Counter.razor` + `Counter.razor.cs` | Reception counter page — shows today's OPD payment collections (patient name, fees, status), total collected, total expenses, net amount |
| `CounterSessions.razor` + `CounterSessions.razor.cs` | Reception page — opens/closes daily session; lists past sessions; submit handover dialog after close |
| `HandoverAccept.razor` + `HandoverAccept.razor.cs` | Admin/Doctor page — lists pending handovers with amount + reception details; accept or dispute with notes |
| `FinanceDashboard.razor` + `FinanceDashboard.razor.cs` | Separate finance dashboard page — tab/section navigation between Daily, Weekly, Monthly, Date-Range, Reception-Wise, and Expense Summary reports; date pickers per report type |

#### Components (`Components/Finance/`)
| File | Purpose |
|------|---------|
| `OpdParticularsPopup.razor` | MudDialog popup opened from OPD list row "Particulars" button — shows current particulars list, particular dropdown (active items), amount field (pre-filled with default), add/edit/remove controls, running total summary, "Accept Amount" button to mark payment received |
| `OpdDiscountDialog.razor` | Doctor-only MudDialog — shows OPD total, discount input field, live FinalAmount preview; confirms and calls ApplyDiscount endpoint |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add 8 new DbSets: DoctorFeeConfigs, Particulars, OpdParticulars, OpdPayments, ExpenseCategories, ExpenseRecords, CounterSessions, AmountHandovers |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register 8 new repositories as Scoped: IDoctorFeeConfigRepository → DoctorFeeConfigRepository (and 7 more) |
| `src/TenantCore.Web.Client/Layout/NavMenu.razor` | Add "Finance" section with role-guarded links to Counter, Expenses, Finance Dashboard, Doctor Fee Config, Particulars, Handovers |
| `src/TenantCore.Web.Client/Program.cs` | Register 9 new typed HTTP clients |
| `src/TenantCore.Application/Features/OpdRegistrations/Handlers/CreateOpdRegistrationHandler.cs` | After creating OPD, dispatch `EnsureOpdPaymentCommand` with DoctorProfileId to auto-create OpdPayment with visit fee (injecting `ISender` into handler) |
| Existing OPD list page (locate via Glob before modifying) | Add "Particulars" button per row (opens OpdParticularsPopup), "Accept Amount" status indicator, Doctor Discount button (visible to RequireClinical role) |

---

## API Endpoints

### DoctorFeeConfigsController (`api/doctor-fee-configs`)

| Method | Route | Request | Response | Auth Policy |
|--------|-------|---------|----------|-------------|
| GET | `api/doctor-fee-configs` | — | `IEnumerable<DoctorFeeConfigSummaryDto>` | RequireAuthenticated |
| GET | `api/doctor-fee-configs/{id:guid}` | — | `DoctorFeeConfigDto` | RequireAuthenticated |
| GET | `api/doctor-fee-configs/by-doctor/{doctorProfileId:guid}` | — | `DoctorFeeConfigDto?` | RequireAuthenticated |
| POST | `api/doctor-fee-configs` | `CreateDoctorFeeConfigRequest` | `Guid` (201) | RequireClinicAdmin |
| PUT | `api/doctor-fee-configs/{id:guid}` | `UpdateDoctorFeeConfigRequest` | `DoctorFeeConfigDto` | RequireClinicAdmin |
| DELETE | `api/doctor-fee-configs/{id:guid}` | — | 204 No Content | RequireClinicAdmin |

### ParticularsController (`api/particulars`)

| Method | Route | Request | Response | Auth Policy |
|--------|-------|---------|----------|-------------|
| GET | `api/particulars` | — | `IEnumerable<ParticularSummaryDto>` | RequireAuthenticated |
| GET | `api/particulars/{id:guid}` | — | `ParticularDto` | RequireAuthenticated |
| POST | `api/particulars` | `CreateParticularRequest` | `Guid` (201) | RequireClinicAdmin |
| PUT | `api/particulars/{id:guid}` | `UpdateParticularRequest` | `ParticularDto` | RequireClinicAdmin |
| DELETE | `api/particulars/{id:guid}` | — | 204 No Content | RequireClinicAdmin |

### OpdParticularsController (`api/opd-particulars`)

| Method | Route | Request | Response | Auth Policy |
|--------|-------|---------|----------|-------------|
| GET | `api/opd-particulars/by-opd/{opdRegistrationId:guid}` | — | `IEnumerable<OpdParticularDto>` | RequireAuthenticated |
| POST | `api/opd-particulars` | `AddOpdParticularRequest` | `OpdParticularDto` (201) | RequireReception |
| PUT | `api/opd-particulars/{id:guid}` | `UpdateOpdParticularRequest` | `OpdParticularDto` | RequireReception |
| DELETE | `api/opd-particulars/{id:guid}` | — | 204 No Content | RequireReception |

### OpdPaymentsController (`api/opd-payments`)

| Method | Route | Request | Response | Auth Policy |
|--------|-------|---------|----------|-------------|
| GET | `api/opd-payments/by-opd/{opdRegistrationId:guid}` | — | `OpdPaymentDto?` | RequireAuthenticated |
| POST | `api/opd-payments/accept` | `AcceptOpdPaymentRequest` | `OpdPaymentDto` | RequireReception |
| POST | `api/opd-payments/apply-discount` | `ApplyOpdDiscountRequest` | `OpdPaymentDto` | RequireClinical |

### ExpenseCategoriesController (`api/expense-categories`)

| Method | Route | Request | Response | Auth Policy |
|--------|-------|---------|----------|-------------|
| GET | `api/expense-categories` | — | `IEnumerable<ExpenseCategorySummaryDto>` | RequireAuthenticated |
| GET | `api/expense-categories/{id:guid}` | — | `ExpenseCategoryDto` | RequireAuthenticated |
| POST | `api/expense-categories` | `CreateExpenseCategoryRequest` | `Guid` (201) | RequireClinicAdmin |
| PUT | `api/expense-categories/{id:guid}` | `UpdateExpenseCategoryRequest` | `ExpenseCategoryDto` | RequireClinicAdmin |
| DELETE | `api/expense-categories/{id:guid}` | — | 204 No Content | RequireClinicAdmin |

### ExpenseRecordsController (`api/expense-records`)

| Method | Route | Query Params | Response | Auth Policy |
|--------|-------|------------|----------|-------------|
| GET | `api/expense-records` | from (optional), to (optional) | `IEnumerable<ExpenseRecordSummaryDto>` | RequireAuthenticated |
| GET | `api/expense-records/{id:guid}` | — | `ExpenseRecordDto` | RequireAuthenticated |
| POST | `api/expense-records` | body: `CreateExpenseRecordRequest` | `Guid` (201) | RequireReception |
| DELETE | `api/expense-records/{id:guid}` | — | 204 No Content | RequireClinicAdmin |

### CounterSessionsController (`api/counter-sessions`)

| Method | Route | Request | Response | Auth Policy |
|--------|-------|---------|----------|-------------|
| GET | `api/counter-sessions` | — | `IEnumerable<CounterSessionSummaryDto>` | RequireAuthenticated |
| GET | `api/counter-sessions/active` | — | `CounterSessionDto?` | RequireReception |
| GET | `api/counter-sessions/{id:guid}` | — | `CounterSessionDto` | RequireAuthenticated |
| POST | `api/counter-sessions/open` | `OpenCounterSessionRequest` | `Guid` (201) | RequireReception |
| POST | `api/counter-sessions/{id:guid}/close` | `CloseCounterSessionRequest` | `CounterSessionDto` | RequireReception |

### AmountHandoversController (`api/amount-handovers`)

| Method | Route | Request | Response | Auth Policy |
|--------|-------|---------|----------|-------------|
| GET | `api/amount-handovers/pending` | — | `IEnumerable<AmountHandoverSummaryDto>` | RequireAuthenticated |
| GET | `api/amount-handovers/by-session/{sessionId:guid}` | — | `IEnumerable<AmountHandoverSummaryDto>` | RequireAuthenticated |
| GET | `api/amount-handovers/{id:guid}` | — | `AmountHandoverDto` | RequireAuthenticated |
| POST | `api/amount-handovers` | `CreateAmountHandoverRequest` | `Guid` (201) | RequireReception |
| POST | `api/amount-handovers/{id:guid}/accept` | `ResolveAmountHandoverRequest` | `AmountHandoverDto` | RequireAuthenticated |
| POST | `api/amount-handovers/{id:guid}/dispute` | `ResolveAmountHandoverRequest` | `AmountHandoverDto` | RequireAuthenticated |

### FinanceReportsController (`api/finance-reports`)

| Method | Route | Query Params | Response | Auth Policy |
|--------|-------|------------|----------|-------------|
| GET | `api/finance-reports/dashboard` | date | `FinanceDashboardSummaryDto` | RequireAuthenticated |
| GET | `api/finance-reports/daily` | date | `DailyCollectionReportDto` | RequireAuthenticated |
| GET | `api/finance-reports/weekly` | weekStartDate | `WeeklyCollectionReportDto` | RequireAuthenticated |
| GET | `api/finance-reports/monthly` | year, month | `MonthlyCollectionReportDto` | RequireAuthenticated |
| GET | `api/finance-reports/date-range` | from, to | `DateRangeCollectionReportDto` | RequireAuthenticated |
| GET | `api/finance-reports/reception-wise` | from, to | `IEnumerable<ReceptionWiseReportDto>` | RequireAuthenticated |
| GET | `api/finance-reports/expense-summary` | from, to | `ExpenseSummaryReportDto` | RequireAuthenticated |

---

## Validation Rules

| Command | Field | Rules |
|---------|-------|-------|
| CreateDoctorFeeConfig | DoctorProfileId | NotEmpty |
| CreateDoctorFeeConfig | VisitFee | >= 0 |
| CreateDoctorFeeConfig | ApplicationId | NotEmpty |
| UpdateDoctorFeeConfig | Id | NotEmpty |
| UpdateDoctorFeeConfig | VisitFee | >= 0 |
| CreateParticular | Name | NotEmpty, MaxLength(150) |
| CreateParticular | DefaultAmount | >= 0 |
| UpdateParticular | Id | NotEmpty |
| UpdateParticular | Name | NotEmpty, MaxLength(150) |
| AddOpdParticular | OpdRegistrationId | NotEmpty |
| AddOpdParticular | ParticularId | NotEmpty |
| AddOpdParticular | Amount | >= 0 |
| UpdateOpdParticular | Id | NotEmpty |
| UpdateOpdParticular | Amount | >= 0 |
| AcceptOpdPayment | OpdRegistrationId | NotEmpty |
| ApplyOpdDiscount | OpdRegistrationId | NotEmpty |
| ApplyOpdDiscount | Discount | >= 0 |
| CreateExpenseCategory | Name | NotEmpty, MaxLength(150) |
| UpdateExpenseCategory | Id | NotEmpty |
| UpdateExpenseCategory | Name | NotEmpty, MaxLength(150) |
| CreateExpenseRecord | ExpenseCategoryId | NotEmpty |
| CreateExpenseRecord | Amount | GreaterThan(0) |
| CreateExpenseRecord | RecordedByUserId | NotEmpty (non-empty Guid) |
| OpenCounterSession | OpenedByUserId | NotEmpty (non-empty Guid) |
| CloseCounterSession | Id | NotEmpty |
| CreateAmountHandover | CounterSessionId | NotEmpty |
| CreateAmountHandover | HandedOverByUserId | NotEmpty (non-empty Guid) |
| CreateAmountHandover | HandedOverToUserId | NotEmpty (non-empty Guid) |
| CreateAmountHandover | Amount | >= 0 |
| AcceptAmountHandover | Id | NotEmpty |
| All commands | ApplicationId | NotEmpty |

---

## Business Rules

Enforced in handlers — throw named domain exceptions on violation:

1. **DoctorFeeConfig uniqueness:** Cannot create a second DoctorFeeConfig for the same DoctorProfileId + ApplicationId — throws `InvalidOperationException("A fee config already exists for this doctor.")`
2. **OpdPayment auto-creation:** Created automatically by `EnsureOpdPaymentHandler` when an OPD is registered. VisitFee is populated from DoctorFeeConfig if it exists; otherwise defaults to 0. Idempotent — no duplicate created if called twice.
3. **OpdPayment totals recalculation:** `TotalAmount = VisitFee + ParticularsTotal`. `FinalAmount = TotalAmount - Discount`. Recalculated in the handler whenever a particular is added, updated, or removed via `IOpdParticularRepository.GetTotalByOpdRegistrationIdAsync`.
4. **OpdPayment accept-once:** Cannot accept an already-Received payment — throws `InvalidOperationException("Payment has already been received.")`
5. **OpdDiscount bounds:** Discount cannot exceed TotalAmount — throws `InvalidOperationException("Discount cannot exceed the total amount.")`
6. **Doctor-only discount:** `ApplyOpdDiscount` endpoint is protected by `RequireClinical` policy — only clinical/doctor role can call it.
7. **CounterSession one-at-a-time:** Cannot open a session when one is already Open for the tenant — throws `InvalidOperationException("A counter session is already open.")`
8. **CounterSession close aggregation:** On close, TotalCollected = sum of `OpdPayments.FinalAmount` where `CounterSessionId == this session`; TotalExpenses = sum of `ExpenseRecords.Amount` where `CounterSessionId == this session`; NetAmount = TotalCollected - TotalExpenses.
9. **AmountHandover status guard:** Accept and Dispute actions only apply to Pending handovers — throws `InvalidOperationException` if status is already Accepted or Disputed.
10. **ExpenseRecord inactive category:** Cannot record an expense against an inactive ExpenseCategory — throws `InvalidOperationException("The selected expense category is inactive.")`

---

## Multi-Tenancy Checklist

- [ ] `ApplicationId` property present on all 8 entities
- [ ] `ApplicationId` passed in all 26 commands and 18 queries
- [ ] All repository queries filter by `applicationId`
- [ ] All controllers use `GetApplicationId()` from `ClinicControllerBase`
- [ ] Blazor clients send `X-Application-Id` header (via `ClinicAuthorizationHandler` registered in Program.cs)

---

## EF Migration

**Migration name:** `AddCounterExpensesManagement`

Run after all infrastructure files are created and DbContext is updated:

```
dotnet ef migrations add AddCounterExpensesManagement --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

---

## Implementation Order

Execute in this sequence to avoid compile errors:

**Phase 1 — Shared Foundation**
1. Enums: PaymentStatus, CounterSessionStatus, HandoverStatus
2. All Shared DTOs (Groups A through I)

**Phase 2 — Domain**
3. All 8 domain entities
4. All 8 repository interfaces

**Phase 3 — Infrastructure**
5. All 8 EF Fluent API configurations
6. All 8 repository implementations
7. Modify `ClinicDbContext` — add 8 DbSets
8. Modify `Infrastructure/DependencyInjection.cs` — register 8 repositories as Scoped

**Phase 4 — Application Layer (by group)**
9. Group A: DoctorFeeConfigs — commands, queries, validators, translator, handlers
10. Group B: Particulars — commands, queries, validators, translator, handlers
11. Group C: OpdParticulars — commands, queries, validators, translator, handlers
12. Group D: OpdPayments — commands, queries, validators, translator, handlers
13. Group E: ExpenseCategories — commands, queries, validators, translator, handlers
14. Group F: ExpenseRecords — commands, queries, validators, translator, handlers
15. Group G: CounterSessions — commands, queries, validators, translator, handlers
16. Group H: AmountHandovers — commands, queries, validators, translator, handlers
17. Group I: FinanceReports — queries and handlers only

**Phase 5 — API Layer**
18. DoctorFeeConfigsController
19. ParticularsController
20. OpdParticularsController
21. OpdPaymentsController
22. ExpenseCategoriesController
23. ExpenseRecordsController
24. CounterSessionsController
25. AmountHandoversController
26. FinanceReportsController

**Phase 6 — OPD Integration**
27. Locate existing OPD registration handler (Glob for CreateOpdRegistration) and modify to dispatch `EnsureOpdPaymentCommand` at the end

**Phase 7 — Blazor Client**
28. All 9 typed HTTP clients
29. Pages: DoctorFeeConfig, Particulars, ExpenseCategories, ExpenseRecords, Counter, CounterSessions, HandoverAccept, FinanceDashboard
30. Components: OpdParticularsPopup, OpdDiscountDialog
31. Locate existing OPD list page (Glob for OpdList.razor or similar) and add Particulars button + Accept Amount badge + Doctor Discount button
32. Modify NavMenu — add Finance section with role-appropriate links
33. Modify `Web.Client/Program.cs` — register 9 HTTP clients

**Phase 8 — Unit Tests**
34. All handler, validator, and translator tests (Groups A through H — see test file table below)

**Phase 9 — EF Migration**
35. Run `AddCounterExpensesManagement` migration

---

## Test Files to Create

All test files live under `tests/TenantCore.Application.Tests/Features/`.

### DoctorFeeConfigs
| File | Coverage |
|------|---------|
| `DoctorFeeConfigs/Commands/CreateDoctorFeeConfigHandlerTests.cs` | Happy path, duplicate throws InvalidOperationException, ApplicationId set on entity |
| `DoctorFeeConfigs/Commands/UpdateDoctorFeeConfigHandlerTests.cs` | VisitFee + IsActive updated, EntityNotFoundException when not found, cross-tenant isolation |
| `DoctorFeeConfigs/Commands/DeleteDoctorFeeConfigHandlerTests.cs` | Delete + Save called, EntityNotFoundException when not found |
| `DoctorFeeConfigs/Queries/GetDoctorFeeConfigsHandlerTests.cs` | Summary DTOs returned, empty list when no data |
| `DoctorFeeConfigs/Queries/GetDoctorFeeConfigByIdHandlerTests.cs` | Full DTO returned, EntityNotFoundException when not found |
| `DoctorFeeConfigs/Queries/GetDoctorFeeConfigByDoctorIdHandlerTests.cs` | Full DTO returned when exists, null returned when not found (no exception) |
| `DoctorFeeConfigs/Validators/CreateDoctorFeeConfigCommandValidatorTests.cs` | Valid passes; DoctorProfileId empty fails; VisitFee negative fails; ApplicationId empty fails |
| `DoctorFeeConfigs/Validators/UpdateDoctorFeeConfigCommandValidatorTests.cs` | Valid passes; Id empty fails; VisitFee negative fails |
| `DoctorFeeConfigs/Translators/DoctorFeeConfigTranslatorTests.cs` | ToEntity: Id non-empty, DoctorProfileId + VisitFee + ApplicationId mapped; ToDto: all fields; ToSummaryDto: display fields |

### Particulars
| File | Coverage |
|------|---------|
| `Particulars/Commands/CreateParticularHandlerTests.cs` | Happy path, Id generated, ApplicationId set |
| `Particulars/Commands/UpdateParticularHandlerTests.cs` | Fields updated, EntityNotFoundException when not found, cross-tenant isolation |
| `Particulars/Commands/DeleteParticularHandlerTests.cs` | Deleted, EntityNotFoundException when not found |
| `Particulars/Queries/GetParticularsHandlerTests.cs` | Summary DTOs returned, empty list |
| `Particulars/Queries/GetParticularByIdHandlerTests.cs` | Full DTO, EntityNotFoundException |
| `Particulars/Validators/CreateParticularCommandValidatorTests.cs` | Name required/maxlength(150); DefaultAmount negative fails; at-limit passes |
| `Particulars/Validators/UpdateParticularCommandValidatorTests.cs` | Same boundaries as Create |
| `Particulars/Translators/ParticularTranslatorTests.cs` | All fields mapped correctly |

### OpdParticulars
| File | Coverage |
|------|---------|
| `OpdParticulars/Commands/AddOpdParticularHandlerTests.cs` | Happy path, name snapshot from Particular, OpdPayment ParticularsTotal updated, Id returned |
| `OpdParticulars/Commands/UpdateOpdParticularHandlerTests.cs` | Amount updated, OpdPayment recalculated, EntityNotFoundException when not found |
| `OpdParticulars/Commands/RemoveOpdParticularHandlerTests.cs` | Deleted, OpdPayment recalculated, EntityNotFoundException when not found |
| `OpdParticulars/Queries/GetOpdParticularsHandlerTests.cs` | Returns list for OPD ID, returns empty for different OPD |
| `OpdParticulars/Validators/AddOpdParticularCommandValidatorTests.cs` | All required fields; Amount negative fails; Amount zero passes |
| `OpdParticulars/Validators/UpdateOpdParticularCommandValidatorTests.cs` | Id required; Amount >= 0 boundary |
| `OpdParticulars/Translators/OpdParticularTranslatorTests.cs` | ToEntity maps OpdRegistrationId + ParticularId + Amount + ParticularName; ToDto maps all fields |

### OpdPayments
| File | Coverage |
|------|---------|
| `OpdPayments/Commands/EnsureOpdPaymentHandlerTests.cs` | Creates with VisitFee when DoctorFeeConfig exists; creates with 0 when no config; no duplicate when already exists |
| `OpdPayments/Commands/AcceptOpdPaymentHandlerTests.cs` | PaymentStatus=Received + AmountReceivedAt set; InvalidOperationException when already Received |
| `OpdPayments/Commands/ApplyOpdDiscountHandlerTests.cs` | FinalAmount = TotalAmount - Discount; InvalidOperationException when Discount > TotalAmount |
| `OpdPayments/Queries/GetOpdPaymentByOpdIdHandlerTests.cs` | Returns DTO when exists, null when not found |
| `OpdPayments/Validators/AcceptOpdPaymentCommandValidatorTests.cs` | OpdRegistrationId + ApplicationId required |
| `OpdPayments/Validators/ApplyOpdDiscountCommandValidatorTests.cs` | Discount >= 0 boundary; OpdRegistrationId required |
| `OpdPayments/Translators/OpdPaymentTranslatorTests.cs` | All monetary fields mapped; PaymentStatus enum mapped |

### ExpenseCategories
| File | Coverage |
|------|---------|
| `ExpenseCategories/Commands/CreateExpenseCategoryHandlerTests.cs` | Happy path, Id generated |
| `ExpenseCategories/Commands/UpdateExpenseCategoryHandlerTests.cs` | Fields updated, EntityNotFoundException |
| `ExpenseCategories/Commands/DeleteExpenseCategoryHandlerTests.cs` | Deleted, EntityNotFoundException |
| `ExpenseCategories/Queries/GetExpenseCategoriesHandlerTests.cs` | Summary list, empty |
| `ExpenseCategories/Queries/GetExpenseCategoryByIdHandlerTests.cs` | Full DTO, not found |
| `ExpenseCategories/Validators/CreateExpenseCategoryCommandValidatorTests.cs` | Name required, maxlength(150), boundary |
| `ExpenseCategories/Validators/UpdateExpenseCategoryCommandValidatorTests.cs` | Same boundaries |
| `ExpenseCategories/Translators/ExpenseCategoryTranslatorTests.cs` | All fields including nullable Description mapped |

### ExpenseRecords
| File | Coverage |
|------|---------|
| `ExpenseRecords/Commands/CreateExpenseRecordHandlerTests.cs` | Happy path, category name snapshot stored, RecordedAt set to UtcNow; InvalidOperationException when category inactive |
| `ExpenseRecords/Commands/DeleteExpenseRecordHandlerTests.cs` | Deleted, EntityNotFoundException when not found |
| `ExpenseRecords/Queries/GetExpenseRecordsHandlerTests.cs` | Summary list returned; date range filter applied when provided |
| `ExpenseRecords/Queries/GetExpenseRecordByIdHandlerTests.cs` | Full DTO, EntityNotFoundException |
| `ExpenseRecords/Validators/CreateExpenseRecordCommandValidatorTests.cs` | Amount > 0 (zero fails); ExpenseCategoryId + RecordedByUserId required |
| `ExpenseRecords/Translators/ExpenseRecordTranslatorTests.cs` | CategoryName snapshot, Amount, Notes mapped |

### CounterSessions
| File | Coverage |
|------|---------|
| `CounterSessions/Commands/OpenCounterSessionHandlerTests.cs` | Happy path, Status=Open, OpenedAt set; InvalidOperationException when session already Open for tenant |
| `CounterSessions/Commands/CloseCounterSessionHandlerTests.cs` | TotalCollected = sum of accepted OpdPayments for session; TotalExpenses = sum of ExpenseRecords; NetAmount calculated; ClosedAt set; InvalidOperationException when already Closed |
| `CounterSessions/Queries/GetCounterSessionsHandlerTests.cs` | Summary DTOs returned |
| `CounterSessions/Queries/GetActiveCounterSessionHandlerTests.cs` | Returns Open session DTO; null when no active session |
| `CounterSessions/Queries/GetCounterSessionByIdHandlerTests.cs` | Full DTO, EntityNotFoundException |
| `CounterSessions/Validators/OpenCounterSessionCommandValidatorTests.cs` | OpenedByUserId + ApplicationId required |
| `CounterSessions/Validators/CloseCounterSessionCommandValidatorTests.cs` | Id + ApplicationId required |
| `CounterSessions/Translators/CounterSessionTranslatorTests.cs` | All fields including CounterSessionStatus enum mapped |

### AmountHandovers
| File | Coverage |
|------|---------|
| `AmountHandovers/Commands/CreateAmountHandoverHandlerTests.cs` | Happy path, Status=Pending, HandedOverAt=UtcNow, CounterSession existence validated |
| `AmountHandovers/Commands/AcceptAmountHandoverHandlerTests.cs` | Status=Accepted, AcceptedAt set; InvalidOperationException when not Pending |
| `AmountHandovers/Commands/DisputeAmountHandoverHandlerTests.cs` | Status=Disputed, ResolutionNotes stored; InvalidOperationException when not Pending |
| `AmountHandovers/Queries/GetPendingAmountHandoversHandlerTests.cs` | Returns only Pending; cross-tenant isolation |
| `AmountHandovers/Queries/GetAmountHandoversBySessionHandlerTests.cs` | Returns all for given session |
| `AmountHandovers/Queries/GetAmountHandoverByIdHandlerTests.cs` | Full DTO, EntityNotFoundException |
| `AmountHandovers/Validators/CreateAmountHandoverCommandValidatorTests.cs` | All required fields; Amount >= 0 boundary |
| `AmountHandovers/Validators/AcceptAmountHandoverCommandValidatorTests.cs` | Id + ApplicationId required |
| `AmountHandovers/Translators/AmountHandoverTranslatorTests.cs` | All fields including HandoverStatus enum mapped |

---

## Open Questions / Risks

1. **OpdParticulars recalculation cross-repo:** `AddOpdParticularHandler`, `UpdateOpdParticularHandler`, and `RemoveOpdParticularHandler` must update `OpdPayment.ParticularsTotal`. These handlers will inject both `IOpdParticularRepository` (to compute sum) and `IOpdPaymentRepository` (to update the record). This is valid — handlers can inject multiple repositories.

2. **EnsureOpdPayment dispatch from CreateOpdRegistrationHandler:** The existing `CreateOpdRegistrationHandler` must be located and modified to dispatch `EnsureOpdPaymentCommand` at the end. If `ISender` is not already injected into that handler, it needs to be added. The executor must Glob/Read the existing handler before modifying.

3. **UserId in commands:** All user ID fields (`ReceivedByUserId`, `OpenedByUserId`, `HandedOverByUserId`, `HandedOverToUserId`, `RecordedByUserId`) are `Guid`. Controllers must read the user ID claim from the JWT (`User.FindFirstValue(ClaimTypes.NameIdentifier)` or the project-specific claim name) and parse it with `Guid.Parse(...)` before passing into commands. Executor must confirm the exact claim name used in this project before implementing (check existing controllers or `ClinicContextMiddleware`).

4. **OPD list page filename:** The exact filename/path of the existing OPD list/registration page must be confirmed via Glob before modification. Expected location: `Pages/Opd/OpdList.razor` or `Pages/OpdRegistrations/OpdList.razor`.

5. **Finance dashboard report performance:** Report handlers aggregate data in memory across all OpdPayments and ExpenseRecords for a date range. For larger clinics with months of data, this may need future pagination or database-level aggregation. For the initial implementation, in-memory grouping is acceptable.

6. **Decimal precision:** All monetary fields must be explicitly configured as `decimal(18,2)` in EF Fluent API configurations. EF Core defaults to `decimal(18,2)` on SQL Server, but explicit configuration is required per project conventions.
