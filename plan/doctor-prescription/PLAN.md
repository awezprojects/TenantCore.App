# Feature Plan: Doctor Prescription

## Repo
TenantCore.App

## Overview
Enables doctors to create and submit prescriptions linked to OPD registrations. Doctors see their pending OPD patient count on the dashboard, open a per-patient prescription form, search and add medicines with per-slot dosage (morning/afternoon/evening/night) and auto-calculated quantity, apply multilingual dosage remarks (English/Hindi/Marathi) configured per clinic, set a next-visit date, add notes, and upload reports (auto-converted to PDF and stored in a patient folder). On submission the prescription is emailed to the patient if an email address is present.

## Domain Area
`Prescriptions` — Api, Application, Domain, Infrastructure, Shared, Web.Client (also touches DosageRemarks and PrescriptionConfig as supporting sub-areas, and modifies existing OPD/Dashboard code)

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/TenantCore.Shared/Enums/MedicineFormType.cs` | Enum: Tab (1), Capsule (2), Syrup (3), Drops (4), Tube (5), Injection (6), Powder (7), Other (8) |
| `src/TenantCore.Shared/Enums/PrescriptionStatus.cs` | Enum: Draft (1), Submitted (2) |
| `src/TenantCore.Shared/Enums/PrescriptionLanguage.cs` | Enum: English (1), Hindi (2), Marathi (3) |
| `src/TenantCore.Domain/Entities/Prescription.cs` | Aggregate root — links OpdRegistration + Patient; carries status, next-visit date, notes |
| `src/TenantCore.Domain/Entities/PrescriptionItem.cs` | Medicine line item owned by Prescription; stores per-slot dosage and multilingual remarks |
| `src/TenantCore.Domain/Entities/PrescriptionReport.cs` | Uploaded report file metadata owned by Prescription |
| `src/TenantCore.Domain/Entities/DosageRemark.cs` | Clinic-level multilingual remark template scoped per MedicineFormType |
| `src/TenantCore.Domain/Entities/PrescriptionConfig.cs` | Clinic-level prescription settings (default language, one row per ApplicationId) |
| `src/TenantCore.Domain/Interfaces/IPrescriptionRepository.cs` | Repository interface: GetPagedAsync, GetByIdAsync (with items+reports), GetByOpdRegistrationIdAsync, AddAsync, Update, SaveChangesAsync |
| `src/TenantCore.Domain/Interfaces/IDosageRemarkRepository.cs` | Repository interface: GetPagedAsync (filtered by form), GetByIdAsync, AddAsync, Update, Remove, SaveChangesAsync |
| `src/TenantCore.Domain/Interfaces/IPrescriptionConfigRepository.cs` | Repository interface: GetByApplicationIdAsync, AddAsync, Update, SaveChangesAsync |
| `src/TenantCore.Application/Services/IFileStorageService.cs` | Interface: SaveAsync(stream, patientFolder, fileName) → stored relative path |
| `src/TenantCore.Application/Services/IEmailService.cs` | Interface: SendAsync(to, subject, htmlBody, attachmentBytes?, attachmentName?) |
| `src/TenantCore.Application/Services/IPdfConversionService.cs` | Interface: ConvertToPdfAsync(fileBytes, originalExtension) → PDF bytes |
| `src/TenantCore.Shared/Dtos/PrescriptionItemDto.cs` | Read DTO for a single medicine line item |
| `src/TenantCore.Shared/Dtos/PrescriptionReportDto.cs` | Read DTO for an attached report |
| `src/TenantCore.Shared/Dtos/PrescriptionDto.cs` | Read DTO — full prescription with nested items and reports lists |
| `src/TenantCore.Shared/Dtos/CreatePrescriptionItemDto.cs` | Write DTO — single medicine line item for create/update |
| `src/TenantCore.Shared/Dtos/CreatePrescriptionDto.cs` | Write DTO — prescription header + Items list |
| `src/TenantCore.Shared/Dtos/UpdatePrescriptionDto.cs` | Write DTO — update header fields and replace items list |
| `src/TenantCore.Shared/Dtos/DosageRemarkDto.cs` | Read DTO for a dosage remark template |
| `src/TenantCore.Shared/Dtos/CreateDosageRemarkDto.cs` | Write DTO — create remark template |
| `src/TenantCore.Shared/Dtos/UpdateDosageRemarkDto.cs` | Write DTO — update remark template |
| `src/TenantCore.Shared/Dtos/PrescriptionConfigDto.cs` | Read DTO for prescription config |
| `src/TenantCore.Shared/Dtos/UpdatePrescriptionConfigDto.cs` | Write DTO — update prescription config |
| `src/TenantCore.Infrastructure/Persistence/Configurations/PrescriptionConfiguration.cs` | EF config: Prescriptions table, unique index on (ApplicationId, PrescriptionNumber), FKs to OpdRegistration and Patient |
| `src/TenantCore.Infrastructure/Persistence/Configurations/PrescriptionItemConfiguration.cs` | EF config: PrescriptionItems table, FK to Prescription with cascade delete |
| `src/TenantCore.Infrastructure/Persistence/Configurations/PrescriptionReportConfiguration.cs` | EF config: PrescriptionReports table, FK to Prescription with cascade delete |
| `src/TenantCore.Infrastructure/Persistence/Configurations/DosageRemarkConfiguration.cs` | EF config: DosageRemarks table, index on (ApplicationId, MedicineForm) |
| `src/TenantCore.Infrastructure/Persistence/Configurations/PrescriptionConfigConfiguration.cs` | EF config: PrescriptionConfigs table, unique index on ApplicationId |
| `src/TenantCore.Infrastructure/Repositories/PrescriptionRepository.cs` | Implementation: eager-loads Items + Reports in GetByIdAsync; paged list with patient/doctor filters |
| `src/TenantCore.Infrastructure/Repositories/DosageRemarkRepository.cs` | Implementation: GetPagedAsync filtered by ApplicationId and optional MedicineForm |
| `src/TenantCore.Infrastructure/Repositories/PrescriptionConfigRepository.cs` | Implementation: GetByApplicationIdAsync returns null if not yet configured |
| `src/TenantCore.Infrastructure/Services/PdfConversionService.cs` | Converts uploaded images (jpg, png, bmp) and documents to PDF bytes; PDF pass-through |
| `src/TenantCore.Infrastructure/Services/LocalFileStorageService.cs` | Saves file stream to `{Storage:RootPath}/{patientId}+{PatientName}/`; creates folder if absent |
| `src/TenantCore.Infrastructure/Services/EmailService.cs` | SMTP email service reading `Email:Host`, `Email:Port`, `Email:From`, `Email:Username`, `Email:Password` from appsettings |
| `src/TenantCore.Application/Features/Prescriptions/Translators/PrescriptionTranslator.cs` | Static translator: Prescription + items + reports → PrescriptionDto |
| `src/TenantCore.Application/Features/Prescriptions/Commands/CreatePrescriptionCommand.cs` | Command: OpdRegistrationId, DoctorUserId, DoctorName, NextVisitDate?, Notes?, Items |
| `src/TenantCore.Application/Features/Prescriptions/Commands/UpdatePrescriptionCommand.cs` | Command: Id, NextVisitDate?, Notes?, Items (replaces all items) |
| `src/TenantCore.Application/Features/Prescriptions/Commands/SubmitPrescriptionCommand.cs` | Command: Id — set status Submitted, update OPD to Completed, send email |
| `src/TenantCore.Application/Features/Prescriptions/Commands/UploadPrescriptionReportCommand.cs` | Command: PrescriptionId, FileName, FileBytes, FileExtension |
| `src/TenantCore.Application/Features/Prescriptions/Queries/GetPrescriptionsQuery.cs` | Query: Page, PageSize, Search?, DoctorUserId?, PatientId?, From?, To? |
| `src/TenantCore.Application/Features/Prescriptions/Queries/GetPrescriptionByIdQuery.cs` | Query: Id |
| `src/TenantCore.Application/Features/Prescriptions/Queries/GetPrescriptionByOpdIdQuery.cs` | Query: OpdRegistrationId |
| `src/TenantCore.Application/Features/Prescriptions/Queries/GetDoctorOpdCountQuery.cs` | Query: DoctorUserId — returns count of today's Pending/InProgress OPD registrations |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/CreatePrescriptionHandler.cs` | Validates OPD exists, checks no existing prescription, calculates quantities, sets OPD status to InProgress |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/UpdatePrescriptionHandler.cs` | Rejects update if status is Submitted; replaces items collection |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/SubmitPrescriptionHandler.cs` | Sets Submitted, sets OPD Completed, sends email if patient.Email present (non-blocking on email failure) |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/UploadPrescriptionReportHandler.cs` | Converts to PDF, saves to patient folder, creates PrescriptionReport record |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/GetPrescriptionsHandler.cs` | Returns paginated prescription list |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/GetPrescriptionByIdHandler.cs` | Returns single prescription or throws NotFoundException |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/GetPrescriptionByOpdIdHandler.cs` | Returns prescription for OPD registration or throws NotFoundException |
| `src/TenantCore.Application/Features/Prescriptions/Handlers/GetDoctorOpdCountHandler.cs` | Returns today's OPD patient count for the given doctor |
| `src/TenantCore.Application/Features/Prescriptions/Validators/CreatePrescriptionCommandValidator.cs` | OpdRegistrationId NotEmpty; Items NotEmpty; each item valid |
| `src/TenantCore.Application/Features/Prescriptions/Validators/UpdatePrescriptionCommandValidator.cs` | Id NotEmpty; Items NotEmpty; each item valid |
| `src/TenantCore.Application/Features/DosageRemarks/Translators/DosageRemarkTranslator.cs` | Static translator: DosageRemark → DosageRemarkDto |
| `src/TenantCore.Application/Features/DosageRemarks/Commands/CreateDosageRemarkCommand.cs` | Command: ApplicationId, MedicineForm, RemarkEnglish, RemarkHindi?, RemarkMarathi? |
| `src/TenantCore.Application/Features/DosageRemarks/Commands/UpdateDosageRemarkCommand.cs` | Command: Id, MedicineForm, RemarkEnglish, RemarkHindi?, RemarkMarathi?, IsActive |
| `src/TenantCore.Application/Features/DosageRemarks/Commands/DeleteDosageRemarkCommand.cs` | Command: Id |
| `src/TenantCore.Application/Features/DosageRemarks/Queries/GetDosageRemarksQuery.cs` | Query: Page, PageSize, MedicineForm? (optional filter) |
| `src/TenantCore.Application/Features/DosageRemarks/Queries/GetDosageRemarkByIdQuery.cs` | Query: Id |
| `src/TenantCore.Application/Features/DosageRemarks/Handlers/CreateDosageRemarkHandler.cs` | Creates dosage remark template |
| `src/TenantCore.Application/Features/DosageRemarks/Handlers/UpdateDosageRemarkHandler.cs` | Updates dosage remark template |
| `src/TenantCore.Application/Features/DosageRemarks/Handlers/DeleteDosageRemarkHandler.cs` | Deletes dosage remark template |
| `src/TenantCore.Application/Features/DosageRemarks/Handlers/GetDosageRemarksHandler.cs` | Returns paginated list, optionally filtered by MedicineForm |
| `src/TenantCore.Application/Features/DosageRemarks/Handlers/GetDosageRemarkByIdHandler.cs` | Returns single remark or throws NotFoundException |
| `src/TenantCore.Application/Features/DosageRemarks/Validators/CreateDosageRemarkCommandValidator.cs` | MedicineForm valid enum; RemarkEnglish NotEmpty MaxLength(500); Hindi/Marathi MaxLength(500) |
| `src/TenantCore.Application/Features/DosageRemarks/Validators/UpdateDosageRemarkCommandValidator.cs` | Same rules as Create plus Id NotEmpty |
| `src/TenantCore.Application/Features/PrescriptionConfig/Translators/PrescriptionConfigTranslator.cs` | Static translator: PrescriptionConfig → PrescriptionConfigDto |
| `src/TenantCore.Application/Features/PrescriptionConfig/Commands/UpsertPrescriptionConfigCommand.cs` | Command: ApplicationId, DefaultLanguage |
| `src/TenantCore.Application/Features/PrescriptionConfig/Queries/GetPrescriptionConfigQuery.cs` | Query: ApplicationId |
| `src/TenantCore.Application/Features/PrescriptionConfig/Handlers/UpsertPrescriptionConfigHandler.cs` | Creates config if not exists, otherwise updates |
| `src/TenantCore.Application/Features/PrescriptionConfig/Handlers/GetPrescriptionConfigHandler.cs` | Returns config or default (English) if not yet configured |
| `src/TenantCore.Application/Features/PrescriptionConfig/Validators/UpsertPrescriptionConfigCommandValidator.cs` | DefaultLanguage must be valid PrescriptionLanguage enum value |
| `src/TenantCore.Api/Controllers/PrescriptionsController.cs` | Thin controller: GET list, GET by id, GET by opd, POST create, PUT update, POST submit, POST report upload |
| `src/TenantCore.Api/Controllers/DosageRemarksController.cs` | Thin controller: GET list, GET by id, POST create, PUT update, DELETE |
| `src/TenantCore.Api/Controllers/PrescriptionConfigController.cs` | Thin controller: GET config, PUT upsert config |
| `src/TenantCore.Web.Client/Clients/IPrescriptionApiClient.cs` | Typed client interface for all prescription API calls |
| `src/TenantCore.Web.Client/Clients/PrescriptionApiClient.cs` | Typed client implementation |
| `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionList.razor` | Blazor page: paginated prescription list with search/filter |
| `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionForm.razor` | Blazor page: create/edit prescription — medicine search, dosage grid, remark auto-fill, report upload |
| `src/TenantCore.Web.Client/Pages/Settings/DosageRemarkSettings.razor` | Blazor settings page: manage multilingual dosage remark templates per form type |
| `src/TenantCore.Web.Client/Pages/Settings/PrescriptionSettings.razor` | Blazor settings page: default language selection for prescription remarks |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Domain/Interfaces/IOpdRegistrationRepository.cs` | Add `CountTodayByDoctorAsync(Guid doctorUserId, CancellationToken ct)` method |
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add `DbSet<Prescription>`, `DbSet<PrescriptionItem>`, `DbSet<PrescriptionReport>`, `DbSet<DosageRemark>`, `DbSet<PrescriptionConfig>` |
| `src/TenantCore.Infrastructure/Repositories/OpdRegistrationRepository.cs` | Implement `CountTodayByDoctorAsync` — filters by today's date and DoctorUserId, counts Pending + InProgress |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `IPrescriptionRepository`, `IDosageRemarkRepository`, `IPrescriptionConfigRepository`, `IFileStorageService` (LocalFileStorageService), `IEmailService` (EmailService), `IPdfConversionService` (PdfConversionService) |
| `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` | Add `GET /api/opd-registrations/doctor-count?doctorUserId=` endpoint sending `GetDoctorOpdCountQuery` |
| `src/TenantCore.Web.Client/Clients/IClinicApiClient.cs` | Add `GetDoctorOpdCountAsync(Guid doctorUserId, CancellationToken ct)` returning `int` |
| `src/TenantCore.Web.Client/Clients/ClinicApiClient.cs` | Implement `GetDoctorOpdCountAsync` calling GET /api/opd-registrations/doctor-count |
| `src/TenantCore.Web.Client/Pages/Dashboard.razor` | Add OPD patient count summary card (click navigates to PrescriptionForm) visible for Doctor role |
| `src/TenantCore.Web.Client/Program.cs` | Register `IPrescriptionApiClient` as scoped typed HTTP client |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|--------------|----------|-------------|
| GET | /api/opd-registrations/doctor-count | `?doctorUserId` | `int` | RequireAuthenticated |
| GET | /api/prescriptions | `?page&pageSize&search&doctorUserId&patientId&from&to` | `PagedResult<PrescriptionDto>` | RequireAuthenticated |
| GET | /api/prescriptions/{id} | — | `PrescriptionDto` | RequireAuthenticated |
| GET | /api/prescriptions/opd/{opdRegistrationId} | — | `PrescriptionDto` | RequireAuthenticated |
| POST | /api/prescriptions | `CreatePrescriptionDto` | `PrescriptionDto` (201) | RequireManagement |
| PUT | /api/prescriptions/{id} | `UpdatePrescriptionDto` | `PrescriptionDto` | RequireManagement |
| POST | /api/prescriptions/{id}/submit | — | `PrescriptionDto` | RequireManagement |
| POST | /api/prescriptions/{id}/reports | multipart/form-data (`IFormFile`) | `PrescriptionReportDto` (201) | RequireManagement |
| GET | /api/dosage-remarks | `?page&pageSize&form` | `PagedResult<DosageRemarkDto>` | RequireAuthenticated |
| GET | /api/dosage-remarks/{id} | — | `DosageRemarkDto` | RequireAuthenticated |
| POST | /api/dosage-remarks | `CreateDosageRemarkDto` | `DosageRemarkDto` (201) | RequireManagement |
| PUT | /api/dosage-remarks/{id} | `UpdateDosageRemarkDto` | `DosageRemarkDto` | RequireManagement |
| DELETE | /api/dosage-remarks/{id} | — | 204 | RequireAdmin |
| GET | /api/prescription-config | — | `PrescriptionConfigDto` | RequireAuthenticated |
| PUT | /api/prescription-config | `UpdatePrescriptionConfigDto` | `PrescriptionConfigDto` | RequireManagement |

---

## Entity Properties

### Prescription
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `ApplicationId` | `Guid` | Required, Index |
| `OpdRegistrationId` | `Guid` | FK → OpdRegistration, Restrict |
| `PatientId` | `Guid` | FK → Patient, Restrict |
| `DoctorUserId` | `Guid` | Required |
| `DoctorName` | `string` | Required, MaxLength(200) |
| `PrescriptionNumber` | `string` | Required, MaxLength(30), Unique per (ApplicationId, PrescriptionNumber) |
| `PrescribedDate` | `DateTime` | Required |
| `NextVisitDate` | `DateTime?` | Nullable |
| `Notes` | `string?` | MaxLength(2000) |
| `Status` | `PrescriptionStatus` | Required |
| `IsEmailSent` | `bool` | Default false |
| `Items` | `ICollection<PrescriptionItem>` | Navigation, owned — cascade delete |
| `Reports` | `ICollection<PrescriptionReport>` | Navigation, owned — cascade delete |

### PrescriptionItem
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `PrescriptionId` | `Guid` | FK → Prescription, Cascade |
| `MedicineId` | `Guid` | FK → Medicine, Restrict |
| `MedicineName` | `string` | Required, MaxLength(300) — name snapshot at prescribe time |
| `MedicineForm` | `MedicineFormType` | Required |
| `DosageUnit` | `string` | Required, MaxLength(20) — e.g. "tablet", "ml", "drops" |
| `DosageMorning` | `decimal?` | Precision(5,2) |
| `DosageAfternoon` | `decimal?` | Precision(5,2) |
| `DosageEvening` | `decimal?` | Precision(5,2) |
| `DosageNight` | `decimal?` | Precision(5,2) |
| `DurationDays` | `int` | Required, > 0 |
| `Quantity` | `decimal` | Precision(10,2) — calculated: (Morning+Afternoon+Evening+Night) × DurationDays |
| `RemarkEnglish` | `string?` | MaxLength(500) |
| `RemarkHindi` | `string?` | MaxLength(500) |
| `RemarkMarathi` | `string?` | MaxLength(500) |
| `SortOrder` | `int` | Display/print order |

### PrescriptionReport
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `PrescriptionId` | `Guid` | FK → Prescription, Cascade |
| `OriginalFileName` | `string` | Required, MaxLength(255) |
| `StoredFileName` | `string` | Required, MaxLength(255) — file name in storage (always .pdf) |
| `FilePath` | `string` | Required, MaxLength(500) — relative path under storage root |
| `FileSizeBytes` | `long?` | Nullable |
| `UploadedAt` | `DateTime` | Required |

### DosageRemark
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `ApplicationId` | `Guid` | Required, Index |
| `MedicineForm` | `MedicineFormType` | Required |
| `RemarkEnglish` | `string` | Required, MaxLength(500) |
| `RemarkHindi` | `string?` | MaxLength(500) |
| `RemarkMarathi` | `string?` | MaxLength(500) |
| `IsActive` | `bool` | Default true |

### PrescriptionConfig
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `ApplicationId` | `Guid` | Required, Unique |
| `DefaultLanguage` | `PrescriptionLanguage` | Required, Default English |

---

## Validation Rules

| Field | Rule |
|-------|------|
| `CreatePrescription.OpdRegistrationId` | NotEmpty |
| `CreatePrescription.DoctorUserId` | NotEmpty |
| `CreatePrescription.DoctorName` | NotEmpty, MaxLength(200) |
| `CreatePrescription.Items` | NotEmpty (at least one medicine required) |
| `CreatePrescriptionItem.MedicineId` | NotEmpty |
| `CreatePrescriptionItem.MedicineForm` | Valid MedicineFormType enum value |
| `CreatePrescriptionItem.DosageUnit` | NotEmpty, MaxLength(20) |
| `CreatePrescriptionItem.DurationDays` | GreaterThan(0) |
| `CreatePrescriptionItem.Quantity` | GreaterThan(0) |
| `UpdatePrescription.Id` | NotEmpty |
| `UpdatePrescription.Items` | NotEmpty |
| `CreateDosageRemark.MedicineForm` | Valid MedicineFormType enum value |
| `CreateDosageRemark.RemarkEnglish` | NotEmpty, MaxLength(500) |
| `CreateDosageRemark.RemarkHindi` | MaxLength(500) when provided |
| `CreateDosageRemark.RemarkMarathi` | MaxLength(500) when provided |
| `UpsertPrescriptionConfig.DefaultLanguage` | Valid PrescriptionLanguage enum value |

---

## Business Rules

- A prescription can only be created when the linked OpdRegistration exists and its ApplicationId matches the current clinic.
- Only one prescription is allowed per OpdRegistration; creating a second throws `InvalidOperationException`.
- Creating a prescription sets the linked OpdRegistration status to `InProgress`.
- `PrescriptionItem.Quantity` is calculated by the handler: `(DosageMorning + DosageAfternoon + DosageEvening + DosageNight) × DurationDays`; the frontend also computes this live for display but the server value is authoritative.
- Prescriptions in Draft status can be updated; updating a Submitted prescription throws `DomainValidationException`.
- Submitting a prescription sets its status to Submitted and the linked OpdRegistration status to Completed.
- Submitting an already-Submitted prescription throws `DomainValidationException`.
- On submission, if `Patient.Email` is not null/empty, `IEmailService.SendAsync` is called with an HTML prescription summary; `Prescription.IsEmailSent` is set to `true` on success. Email failure is non-blocking — log the error, set `IsEmailSent = false`, continue.
- Uploaded report files are always converted to PDF before storage regardless of input format (image or document); native PDF files are stored as-is.
- Report files are stored under `{Storage:RootPath}/{patientId}+{FirstName LastName}/` as configured in `appsettings.json` under `Storage:RootPath`.
- `PrescriptionNumber` is auto-generated in the format `RX-{yyyyMMdd}-{sequence}` where sequence is zero-padded to 4 digits and resets per day per ApplicationId.
- DosageRemark templates are scoped per ApplicationId and MedicineFormType; when a doctor adds a medicine on the prescription form, the frontend fetches matching remarks for the selected form and pre-fills the remark field in the clinic's configured default language. The doctor can override the pre-filled text before submitting.
- `GetDoctorOpdCountQuery` returns the count of OpdRegistrations where RegistrationDate.Date == today, Status is Pending or InProgress, DoctorUserId matches, and ApplicationId matches.
- `DosageUnit` is determined by MedicineFormType on the frontend: Tab/Capsule → "tablet", Syrup → "ml", Drops → "drops", Tube → "application", Injection → "dose", Powder → "sachet", Other → "unit".

---

## Implementation Order

1. `src/TenantCore.Shared/Enums/MedicineFormType.cs` — create enum
2. `src/TenantCore.Shared/Enums/PrescriptionStatus.cs` — create enum
3. `src/TenantCore.Shared/Enums/PrescriptionLanguage.cs` — create enum
4. `src/TenantCore.Domain/Entities/Prescription.cs` — create entity with Create, Submit, and Update factory/methods
5. `src/TenantCore.Domain/Entities/PrescriptionItem.cs` — create entity with Create factory method
6. `src/TenantCore.Domain/Entities/PrescriptionReport.cs` — create entity with Create factory method
7. `src/TenantCore.Domain/Entities/DosageRemark.cs` — create entity with Create and Update methods
8. `src/TenantCore.Domain/Entities/PrescriptionConfig.cs` — create entity with Create and Update methods
9. `src/TenantCore.Domain/Interfaces/IPrescriptionRepository.cs` — create interface
10. `src/TenantCore.Domain/Interfaces/IDosageRemarkRepository.cs` — create interface
11. `src/TenantCore.Domain/Interfaces/IPrescriptionConfigRepository.cs` — create interface
12. `src/TenantCore.Domain/Interfaces/IOpdRegistrationRepository.cs` — add `CountTodayByDoctorAsync`
13. `src/TenantCore.Application/Services/IFileStorageService.cs` — create interface
14. `src/TenantCore.Application/Services/IEmailService.cs` — create interface
15. `src/TenantCore.Application/Services/IPdfConversionService.cs` — create interface
16. `src/TenantCore.Shared/Dtos/PrescriptionItemDto.cs` — create read DTO
17. `src/TenantCore.Shared/Dtos/PrescriptionReportDto.cs` — create read DTO
18. `src/TenantCore.Shared/Dtos/PrescriptionDto.cs` — create read DTO with nested Items and Reports lists
19. `src/TenantCore.Shared/Dtos/CreatePrescriptionItemDto.cs` — create write DTO
20. `src/TenantCore.Shared/Dtos/CreatePrescriptionDto.cs` — create write DTO
21. `src/TenantCore.Shared/Dtos/UpdatePrescriptionDto.cs` — create write DTO
22. `src/TenantCore.Shared/Dtos/DosageRemarkDto.cs` — create read DTO
23. `src/TenantCore.Shared/Dtos/CreateDosageRemarkDto.cs` — create write DTO
24. `src/TenantCore.Shared/Dtos/UpdateDosageRemarkDto.cs` — create write DTO
25. `src/TenantCore.Shared/Dtos/PrescriptionConfigDto.cs` — create read DTO
26. `src/TenantCore.Shared/Dtos/UpdatePrescriptionConfigDto.cs` — create write DTO
27. `src/TenantCore.Infrastructure/Persistence/Configurations/PrescriptionConfiguration.cs` — create EF config
28. `src/TenantCore.Infrastructure/Persistence/Configurations/PrescriptionItemConfiguration.cs` — create EF config
29. `src/TenantCore.Infrastructure/Persistence/Configurations/PrescriptionReportConfiguration.cs` — create EF config
30. `src/TenantCore.Infrastructure/Persistence/Configurations/DosageRemarkConfiguration.cs` — create EF config
31. `src/TenantCore.Infrastructure/Persistence/Configurations/PrescriptionConfigConfiguration.cs` — create EF config
32. `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` — add 5 new DbSets
33. `src/TenantCore.Infrastructure/Repositories/PrescriptionRepository.cs` — implement repository
34. `src/TenantCore.Infrastructure/Repositories/DosageRemarkRepository.cs` — implement repository
35. `src/TenantCore.Infrastructure/Repositories/PrescriptionConfigRepository.cs` — implement repository
36. `src/TenantCore.Infrastructure/Repositories/OpdRegistrationRepository.cs` — implement `CountTodayByDoctorAsync`
37. `src/TenantCore.Infrastructure/Services/PdfConversionService.cs` — implement PDF conversion (image → PDF embed; document conversion; PDF pass-through)
38. `src/TenantCore.Infrastructure/Services/LocalFileStorageService.cs` — implement local file storage with patient folder creation
39. `src/TenantCore.Infrastructure/Services/EmailService.cs` — implement SMTP email service
40. `src/TenantCore.Infrastructure/DependencyInjection.cs` — register 3 repositories + 3 infrastructure services
41. `src/TenantCore.Application/Features/Prescriptions/Translators/PrescriptionTranslator.cs` — create static translator
42. `src/TenantCore.Application/Features/DosageRemarks/Translators/DosageRemarkTranslator.cs` — create static translator
43. `src/TenantCore.Application/Features/PrescriptionConfig/Translators/PrescriptionConfigTranslator.cs` — create static translator
44. `src/TenantCore.Application/Features/Prescriptions/Commands/CreatePrescriptionCommand.cs` — create command
45. `src/TenantCore.Application/Features/Prescriptions/Commands/UpdatePrescriptionCommand.cs` — create command
46. `src/TenantCore.Application/Features/Prescriptions/Commands/SubmitPrescriptionCommand.cs` — create command
47. `src/TenantCore.Application/Features/Prescriptions/Commands/UploadPrescriptionReportCommand.cs` — create command
48. `src/TenantCore.Application/Features/Prescriptions/Queries/GetPrescriptionsQuery.cs` — create query
49. `src/TenantCore.Application/Features/Prescriptions/Queries/GetPrescriptionByIdQuery.cs` — create query
50. `src/TenantCore.Application/Features/Prescriptions/Queries/GetPrescriptionByOpdIdQuery.cs` — create query
51. `src/TenantCore.Application/Features/Prescriptions/Queries/GetDoctorOpdCountQuery.cs` — create query
52. `src/TenantCore.Application/Features/Prescriptions/Handlers/CreatePrescriptionHandler.cs` — create handler
53. `src/TenantCore.Application/Features/Prescriptions/Handlers/UpdatePrescriptionHandler.cs` — create handler
54. `src/TenantCore.Application/Features/Prescriptions/Handlers/SubmitPrescriptionHandler.cs` — create handler
55. `src/TenantCore.Application/Features/Prescriptions/Handlers/UploadPrescriptionReportHandler.cs` — create handler
56. `src/TenantCore.Application/Features/Prescriptions/Handlers/GetPrescriptionsHandler.cs` — create handler
57. `src/TenantCore.Application/Features/Prescriptions/Handlers/GetPrescriptionByIdHandler.cs` — create handler
58. `src/TenantCore.Application/Features/Prescriptions/Handlers/GetPrescriptionByOpdIdHandler.cs` — create handler
59. `src/TenantCore.Application/Features/Prescriptions/Handlers/GetDoctorOpdCountHandler.cs` — create handler
60. `src/TenantCore.Application/Features/Prescriptions/Validators/CreatePrescriptionCommandValidator.cs` — create validator
61. `src/TenantCore.Application/Features/Prescriptions/Validators/UpdatePrescriptionCommandValidator.cs` — create validator
62. `src/TenantCore.Application/Features/DosageRemarks/Commands/CreateDosageRemarkCommand.cs` — create command
63. `src/TenantCore.Application/Features/DosageRemarks/Commands/UpdateDosageRemarkCommand.cs` — create command
64. `src/TenantCore.Application/Features/DosageRemarks/Commands/DeleteDosageRemarkCommand.cs` — create command
65. `src/TenantCore.Application/Features/DosageRemarks/Queries/GetDosageRemarksQuery.cs` — create query
66. `src/TenantCore.Application/Features/DosageRemarks/Queries/GetDosageRemarkByIdQuery.cs` — create query
67. `src/TenantCore.Application/Features/DosageRemarks/Handlers/CreateDosageRemarkHandler.cs` — create handler
68. `src/TenantCore.Application/Features/DosageRemarks/Handlers/UpdateDosageRemarkHandler.cs` — create handler
69. `src/TenantCore.Application/Features/DosageRemarks/Handlers/DeleteDosageRemarkHandler.cs` — create handler
70. `src/TenantCore.Application/Features/DosageRemarks/Handlers/GetDosageRemarksHandler.cs` — create handler
71. `src/TenantCore.Application/Features/DosageRemarks/Handlers/GetDosageRemarkByIdHandler.cs` — create handler
72. `src/TenantCore.Application/Features/DosageRemarks/Validators/CreateDosageRemarkCommandValidator.cs` — create validator
73. `src/TenantCore.Application/Features/DosageRemarks/Validators/UpdateDosageRemarkCommandValidator.cs` — create validator
74. `src/TenantCore.Application/Features/PrescriptionConfig/Commands/UpsertPrescriptionConfigCommand.cs` — create command
75. `src/TenantCore.Application/Features/PrescriptionConfig/Queries/GetPrescriptionConfigQuery.cs` — create query
76. `src/TenantCore.Application/Features/PrescriptionConfig/Handlers/UpsertPrescriptionConfigHandler.cs` — create handler
77. `src/TenantCore.Application/Features/PrescriptionConfig/Handlers/GetPrescriptionConfigHandler.cs` — create handler
78. `src/TenantCore.Application/Features/PrescriptionConfig/Validators/UpsertPrescriptionConfigCommandValidator.cs` — create validator
79. `src/TenantCore.Api/Controllers/PrescriptionsController.cs` — create controller
80. `src/TenantCore.Api/Controllers/DosageRemarksController.cs` — create controller
81. `src/TenantCore.Api/Controllers/PrescriptionConfigController.cs` — create controller
82. `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` — add doctor-count endpoint
83. `src/TenantCore.Web.Client/Clients/IClinicApiClient.cs` — add GetDoctorOpdCountAsync
84. `src/TenantCore.Web.Client/Clients/ClinicApiClient.cs` — implement GetDoctorOpdCountAsync
85. `src/TenantCore.Web.Client/Clients/IPrescriptionApiClient.cs` — create typed client interface
86. `src/TenantCore.Web.Client/Clients/PrescriptionApiClient.cs` — create typed client implementation
87. `src/TenantCore.Web.Client/Program.cs` — register IPrescriptionApiClient typed HTTP client
88. `src/TenantCore.Web.Client/Pages/Dashboard.razor` — add OPD patient count card visible for Doctor role
89. `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionList.razor` — create prescription list page
90. `src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionForm.razor` — create prescription create/edit page
91. `src/TenantCore.Web.Client/Pages/Settings/DosageRemarkSettings.razor` — create dosage remark settings page
92. `src/TenantCore.Web.Client/Pages/Settings/PrescriptionSettings.razor` — create prescription config settings page
93. Run migration: `dotnet ef migrations add AddPrescriptionFeature --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api`

---

## Migration Name

AddPrescriptionFeature

---

## Execution Status

- **Status**: Plan fully executed and completed
- **Started**: 2026-04-26
- **Development completed**: 2026-04-26
- **Security check completed**: 2026-04-26
- **Completed**: 2026-04-26
