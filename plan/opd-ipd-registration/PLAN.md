# Feature Plan: OPD & IPD Registration

## Repo
TenantCore.App

## Overview
Receptionists register patients for Outpatient (OPD) and Inpatient (IPD) visits, selecting from a live doctor list fetched from the Auth system. Clinic Admins configure per-tenant OPD fees. Patient records support optional Aadhaar number and photo URL. All clinic data lives in a dedicated `ClinicDbContext` backed by a separate SQL Server database (`TenantClinicDb`). The existing `AppDbContext` / `TenantCoreDb` (tenant management entity, repo, commands, pages) is no longer needed and is removed as part of this work — `ClinicDbContext` becomes the sole DbContext in the application.

## Domain Area
`Patients`, `OpdRegistrations`, `IpdRegistrations`, `ClinicSettings` — touches all layers: Domain, Infrastructure (new ClinicDbContext + generic ClinicRepository), Application, Api, Shared, Web.Client.

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/TenantCore.Shared/Enums/ClinicRole.cs` | Enum for clinic-specific roles (Doctor, Receptionist, etc.) |
| `src/TenantCore.Shared/Enums/Gender.cs` | Patient gender enum |
| `src/TenantCore.Shared/Enums/OpdStatus.cs` | OPD registration lifecycle states |
| `src/TenantCore.Shared/Enums/IpdStatus.cs` | IPD admission lifecycle states |
| `src/TenantCore.Shared/Dtos/PatientDto.cs` | Patient read DTO + write DTOs |
| `src/TenantCore.Shared/Dtos/OpdRegistrationDto.cs` | OPD read DTO + write DTOs |
| `src/TenantCore.Shared/Dtos/IpdRegistrationDto.cs` | IPD read DTO + write DTOs |
| `src/TenantCore.Shared/Dtos/ClinicFeeConfigDto.cs` | Fee config read + write DTOs |
| `src/TenantCore.Shared/Dtos/DoctorDto.cs` | Doctor list item DTO |
| `src/TenantCore.Domain/Entities/Patient.cs` | Patient domain entity |
| `src/TenantCore.Domain/Entities/OpdRegistration.cs` | OPD registration entity |
| `src/TenantCore.Domain/Entities/IpdRegistration.cs` | IPD admission entity |
| `src/TenantCore.Domain/Entities/ClinicFeeConfig.cs` | Per-tenant fee config entity |
| `src/TenantCore.Domain/Interfaces/IClinicRepository.cs` | Generic clinic repo interface extending IRepository<T> |
| `src/TenantCore.Domain/Interfaces/IPatientRepository.cs` | Patient-specific repo interface |
| `src/TenantCore.Domain/Interfaces/IOpdRegistrationRepository.cs` | OPD-specific repo interface |
| `src/TenantCore.Domain/Interfaces/IIpdRegistrationRepository.cs` | IPD-specific repo interface |
| `src/TenantCore.Domain/Interfaces/IClinicFeeConfigRepository.cs` | FeeConfig repo interface |
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Separate EF DbContext for clinic DB |
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContextFactory.cs` | IDesignTimeDbContextFactory for migrations |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/PatientConfiguration.cs` | Fluent API config for Patient |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/OpdRegistrationConfiguration.cs` | Fluent API config for OpdRegistration |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/IpdRegistrationConfiguration.cs` | Fluent API config for IpdRegistration |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/ClinicFeeConfigConfiguration.cs` | Fluent API config for ClinicFeeConfig |
| `src/TenantCore.Infrastructure/Repositories/ClinicRepository.cs` | Generic base repo backed by ClinicDbContext |
| `src/TenantCore.Infrastructure/Repositories/PatientRepository.cs` | Patient repo (extends ClinicRepository<Patient>) |
| `src/TenantCore.Infrastructure/Repositories/OpdRegistrationRepository.cs` | OPD repo |
| `src/TenantCore.Infrastructure/Repositories/IpdRegistrationRepository.cs` | IPD repo |
| `src/TenantCore.Infrastructure/Repositories/ClinicFeeConfigRepository.cs` | FeeConfig repo |
| `src/TenantCore.Application/Features/Patients/Translators/PatientTranslator.cs` | Entity → DTO |
| `src/TenantCore.Application/Features/Patients/Commands/RegisterPatientCommand.cs` | Create patient command |
| `src/TenantCore.Application/Features/Patients/Commands/UpdatePatientCommand.cs` | Update patient command |
| `src/TenantCore.Application/Features/Patients/Queries/GetPatientsQuery.cs` | Paged patient list |
| `src/TenantCore.Application/Features/Patients/Queries/GetPatientByIdQuery.cs` | Single patient |
| `src/TenantCore.Application/Features/Patients/Handlers/RegisterPatientHandler.cs` | Handle RegisterPatientCommand |
| `src/TenantCore.Application/Features/Patients/Handlers/UpdatePatientHandler.cs` | Handle UpdatePatientCommand |
| `src/TenantCore.Application/Features/Patients/Handlers/GetPatientsHandler.cs` | Handle GetPatientsQuery |
| `src/TenantCore.Application/Features/Patients/Handlers/GetPatientByIdHandler.cs` | Handle GetPatientByIdQuery |
| `src/TenantCore.Application/Features/Patients/Validators/RegisterPatientCommandValidator.cs` | FluentValidation for registration |
| `src/TenantCore.Application/Features/Patients/Validators/UpdatePatientCommandValidator.cs` | FluentValidation for update |
| `src/TenantCore.Application/Features/OpdRegistrations/Translators/OpdRegistrationTranslator.cs` | Entity → DTO |
| `src/TenantCore.Application/Features/OpdRegistrations/Commands/CreateOpdRegistrationCommand.cs` | Create OPD command |
| `src/TenantCore.Application/Features/OpdRegistrations/Commands/UpdateOpdRegistrationCommand.cs` | Update OPD command |
| `src/TenantCore.Application/Features/OpdRegistrations/Queries/GetOpdRegistrationsQuery.cs` | Paged OPD list |
| `src/TenantCore.Application/Features/OpdRegistrations/Queries/GetOpdRegistrationByIdQuery.cs` | Single OPD |
| `src/TenantCore.Application/Features/OpdRegistrations/Handlers/CreateOpdRegistrationHandler.cs` | Handle create OPD |
| `src/TenantCore.Application/Features/OpdRegistrations/Handlers/UpdateOpdRegistrationHandler.cs` | Handle update OPD |
| `src/TenantCore.Application/Features/OpdRegistrations/Handlers/GetOpdRegistrationsHandler.cs` | Handle list OPD |
| `src/TenantCore.Application/Features/OpdRegistrations/Handlers/GetOpdRegistrationByIdHandler.cs` | Handle get by id |
| `src/TenantCore.Application/Features/OpdRegistrations/Validators/CreateOpdRegistrationCommandValidator.cs` | Validation |
| `src/TenantCore.Application/Features/IpdRegistrations/Translators/IpdRegistrationTranslator.cs` | Entity → DTO |
| `src/TenantCore.Application/Features/IpdRegistrations/Commands/CreateIpdRegistrationCommand.cs` | Create IPD command |
| `src/TenantCore.Application/Features/IpdRegistrations/Commands/UpdateIpdRegistrationCommand.cs` | Update IPD command |
| `src/TenantCore.Application/Features/IpdRegistrations/Commands/DischargePatientCommand.cs` | Discharge command |
| `src/TenantCore.Application/Features/IpdRegistrations/Queries/GetIpdRegistrationsQuery.cs` | Paged IPD list |
| `src/TenantCore.Application/Features/IpdRegistrations/Queries/GetIpdRegistrationByIdQuery.cs` | Single IPD |
| `src/TenantCore.Application/Features/IpdRegistrations/Handlers/CreateIpdRegistrationHandler.cs` | Handle create IPD |
| `src/TenantCore.Application/Features/IpdRegistrations/Handlers/UpdateIpdRegistrationHandler.cs` | Handle update IPD |
| `src/TenantCore.Application/Features/IpdRegistrations/Handlers/DischargePatientHandler.cs` | Handle discharge |
| `src/TenantCore.Application/Features/IpdRegistrations/Handlers/GetIpdRegistrationsHandler.cs` | Handle list IPD |
| `src/TenantCore.Application/Features/IpdRegistrations/Handlers/GetIpdRegistrationByIdHandler.cs` | Handle get by id |
| `src/TenantCore.Application/Features/IpdRegistrations/Validators/CreateIpdRegistrationCommandValidator.cs` | Validation |
| `src/TenantCore.Application/Features/IpdRegistrations/Validators/DischargePatientCommandValidator.cs` | Discharge validation |
| `src/TenantCore.Application/Features/ClinicSettings/Translators/ClinicFeeConfigTranslator.cs` | Entity → DTO |
| `src/TenantCore.Application/Features/ClinicSettings/Commands/UpdateClinicFeeConfigCommand.cs` | Update fees command |
| `src/TenantCore.Application/Features/ClinicSettings/Queries/GetClinicFeeConfigQuery.cs` | Get fees query |
| `src/TenantCore.Application/Features/ClinicSettings/Handlers/UpdateClinicFeeConfigHandler.cs` | Handle update fees |
| `src/TenantCore.Application/Features/ClinicSettings/Handlers/GetClinicFeeConfigHandler.cs` | Handle get fees |
| `src/TenantCore.Application/Features/ClinicSettings/Validators/UpdateClinicFeeConfigCommandValidator.cs` | Validation |
| `src/TenantCore.Application/Features/Clinics/Queries/GetClinicDoctorsQuery.cs` | Query doctor list from Auth |
| `src/TenantCore.Application/Features/Clinics/Handlers/GetClinicDoctorsHandler.cs` | Calls IAuthApplicationService, filters Doctor role |
| `src/TenantCore.Api/Controllers/PatientsController.cs` | CRUD patient endpoints |
| `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` | OPD CRUD + doctor list endpoint |
| `src/TenantCore.Api/Controllers/IpdRegistrationsController.cs` | IPD CRUD + discharge endpoint |
| `src/TenantCore.Api/Controllers/ClinicSettingsController.cs` | Fee config get/update |
| `src/TenantCore.Web.Client/Clients/ClinicApiClient.cs` | Typed HTTP client for all clinic endpoints |
| `src/TenantCore.Web.Client/Pages/Patients/PatientList.razor` | Patient search/list page |
| `src/TenantCore.Web.Client/Pages/Opd/OpdRegistration.razor` | OPD registration form |
| `src/TenantCore.Web.Client/Pages/Ipd/IpdRegistration.razor` | IPD registration form |

---

## Files to Delete

Remove all existing tenant-management artefacts — these are replaced by the clinic DB.

| File | Reason |
|------|--------|
| `src/TenantCore.Infrastructure/Persistence/AppDbContext.cs` | Replaced by `ClinicDbContext` |
| `src/TenantCore.Infrastructure/Persistence/Configurations/TenantConfiguration.cs` | Entity removed |
| `src/TenantCore.Infrastructure/Persistence/Migrations/` _(entire folder)_ | AppDbContext migrations no longer needed |
| `src/TenantCore.Infrastructure/Repositories/TenantRepository.cs` | Replaced by clinic repos |
| `src/TenantCore.Domain/Entities/Tenant.cs` | Entity removed |
| `src/TenantCore.Domain/Interfaces/ITenantRepository.cs` | Interface removed |
| `src/TenantCore.Application/Features/Tenants/` _(entire folder)_ | Commands, queries, handlers, validators, translator all removed |
| `src/TenantCore.Shared/Dtos/TenantDto.cs` | DTO removed |
| `src/TenantCore.Api/Controllers/TenantsController.cs` | Controller removed |
| `src/TenantCore.Web.Client/Pages/Tenants/TenantList.razor` | Page removed |

> Keep: `ApplicationController`, `AuthController`, `AuthApplicationService`, `IAuthApplicationService`, base entities (`BaseEntity`, `AuditableEntity`), `IRepository<T>`, `PagedResult<T>`, `Result<T>`, authorization constants, and all other non-tenant infrastructure.

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Remove `AppDbContext` + `ITenantRepository` registrations; add `ClinicDbContext` with `ClinicConnection` string and register 4 new clinic repositories |
| `src/TenantCore.Api/appsettings.json` | Replace `"DefaultConnection"` with `"ClinicConnection"` under `ConnectionStrings` |
| `src/TenantCore.Api/appsettings.Development.json` | Same — replace `DefaultConnection` with `ClinicConnection` pointing to `TenantClinicDb` |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|--------------|----------|-------------|
| GET | /api/patients | — | `PagedResult<PatientDto>` | RequireAuthenticated |
| GET | /api/patients/{id} | — | `PatientDto` | RequireAuthenticated |
| POST | /api/patients | `CreatePatientDto` | `PatientDto` (201) | RequireManagement |
| PUT | /api/patients/{id} | `UpdatePatientDto` | `PatientDto` | RequireManagement |
| GET | /api/opd-registrations | — | `PagedResult<OpdRegistrationDto>` | RequireAuthenticated |
| GET | /api/opd-registrations/{id} | — | `OpdRegistrationDto` | RequireAuthenticated |
| POST | /api/opd-registrations | `CreateOpdRegistrationDto` | `OpdRegistrationDto` (201) | RequireManagement |
| PUT | /api/opd-registrations/{id} | `UpdateOpdRegistrationDto` | `OpdRegistrationDto` | RequireManagement |
| GET | /api/ipd-registrations | — | `PagedResult<IpdRegistrationDto>` | RequireAuthenticated |
| GET | /api/ipd-registrations/{id} | — | `IpdRegistrationDto` | RequireAuthenticated |
| POST | /api/ipd-registrations | `CreateIpdRegistrationDto` | `IpdRegistrationDto` (201) | RequireManagement |
| PUT | /api/ipd-registrations/{id} | `UpdateIpdRegistrationDto` | `IpdRegistrationDto` | RequireManagement |
| PATCH | /api/ipd-registrations/{id}/discharge | `DischargePatientDto` | `IpdRegistrationDto` | RequireManagement |
| GET | /api/clinic-settings/fees | — | `ClinicFeeConfigDto` | RequireAuthenticated |
| PUT | /api/clinic-settings/fees | `UpdateClinicFeeConfigDto` | `ClinicFeeConfigDto` | RequireClinicAdmin |
| GET | /api/clinic-settings/doctors | — | `IEnumerable<DoctorDto>` | RequireAuthenticated |

---

## Entity Properties

### Patient
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | Required, indexed |
| `FirstName` | `string` | Required, max 100 |
| `LastName` | `string` | Required, max 100 |
| `DateOfBirth` | `DateOnly` | Required |
| `Gender` | `Gender` (enum) | Required |
| `PhoneNumber` | `string` | Required, max 20 |
| `Email` | `string?` | Optional, max 256 |
| `AadhaarNumber` | `string?` | Optional, max 12, stored encrypted |
| `PhotoUrl` | `string?` | Optional, max 500 |
| `Address` | `string?` | Optional, max 500 |
| `IsActive` | `bool` | Default true |

### OpdRegistration
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | Required, indexed |
| `PatientId` | `Guid` | FK → Patient |
| `DoctorUserId` | `Guid` | Required (Auth user ID, no FK) |
| `DoctorName` | `string` | Required, max 200 (denormalized) |
| `RegistrationNumber` | `string` | Required, max 30, unique per tenant |
| `RegistrationDate` | `DateTime` | Required (UTC) |
| `Fee` | `decimal` | Required, precision 18,2 |
| `Status` | `OpdStatus` (enum) | Default Pending |
| `Notes` | `string?` | Optional, max 1000 |

### IpdRegistration
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | Required, indexed |
| `PatientId` | `Guid` | FK → Patient |
| `DoctorUserId` | `Guid` | Required |
| `DoctorName` | `string` | Required, max 200 (denormalized) |
| `AdmissionNumber` | `string` | Required, max 30, unique per tenant |
| `AdmissionDate` | `DateTime` | Required (UTC) |
| `DischargeDate` | `DateTime?` | Nullable |
| `WardName` | `string?` | Optional, max 100 |
| `RoomNumber` | `string?` | Optional, max 20 |
| `BedNumber` | `string?` | Optional, max 20 |
| `InitialFee` | `decimal` | Required, precision 18,2 |
| `Status` | `IpdStatus` (enum) | Default Admitted |
| `AdmissionNotes` | `string?` | Optional, max 1000 |
| `DischargeNotes` | `string?` | Nullable, max 1000 |

### ClinicFeeConfig
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | Required, unique index (one per tenant) |
| `OpdFee` | `decimal` | Required, precision 18,2 |
| `UpdatedAt` | `DateTime?` | Nullable |

---

## Validation Rules

| Field | Rule |
|-------|------|
| `RegisterPatientCommand.FirstName` | NotEmpty, MaxLength(100) |
| `RegisterPatientCommand.LastName` | NotEmpty, MaxLength(100) |
| `RegisterPatientCommand.PhoneNumber` | NotEmpty, MaxLength(20), Matches(`^\+?[0-9\s\-]+$`) |
| `RegisterPatientCommand.AadhaarNumber` | Optional; when set: Length(12), Matches(`^[0-9]{12}$`) |
| `RegisterPatientCommand.Email` | Optional; when set: valid email, MaxLength(256) |
| `CreateOpdRegistrationCommand.PatientId` | NotEmpty |
| `CreateOpdRegistrationCommand.DoctorUserId` | NotEmpty |
| `CreateOpdRegistrationCommand.DoctorName` | NotEmpty, MaxLength(200) |
| `CreateOpdRegistrationCommand.Fee` | GreaterThanOrEqualTo(0) |
| `CreateIpdRegistrationCommand.PatientId` | NotEmpty |
| `CreateIpdRegistrationCommand.DoctorUserId` | NotEmpty |
| `CreateIpdRegistrationCommand.DoctorName` | NotEmpty, MaxLength(200) |
| `CreateIpdRegistrationCommand.InitialFee` | GreaterThanOrEqualTo(0) |
| `DischargePatientCommand.Id` | NotEmpty |
| `DischargePatientCommand.DischargeNotes` | Optional, MaxLength(1000) |
| `UpdateClinicFeeConfigCommand.OpdFee` | GreaterThanOrEqualTo(0) |

---

## Business Rules

- A patient's `TenantId` is set from the JWT claim `tid` (or `tenantId`) — patients are scoped per clinic.
- `OpdRegistration.RegistrationNumber` is generated as `OPD-{yyyyMMdd}-{4-digit-sequence}` within the handler.
- `IpdRegistration.AdmissionNumber` is generated as `IPD-{yyyyMMdd}-{4-digit-sequence}`.
- OPD `Fee` defaults to the tenant's configured `ClinicFeeConfig.OpdFee` when not explicitly provided.
- A patient cannot be discharged if `Status != Admitted`.
- An OPD registration cannot be updated if `Status == Cancelled` or `Status == Completed`.
- `GetClinicDoctorsHandler` calls `IAuthApplicationService.GetApplicationUsersAsync(applicationId)` and filters results where the user has the `Doctor` role string. The `applicationId` is taken from the JWT claim `applicationId`.
- Aadhaar numbers are stored as-is (plain text in this version). A note in the plan flags that encryption should be added before production use.
- `ClinicFeeConfig` is upserted: if no config exists for the tenant, one is created on the first `PUT`.

---

## Exact Code Snippets

### `src/TenantCore.Shared/Enums/ClinicRole.cs`
```csharp
namespace TenantCore.Shared.Enums;

public enum ClinicRole
{
    Pharmacist = 1,
    LabTechnician = 2,
    Nurse = 3,
    Doctor = 4,
    ClinicAdmin = 5,
    ClinicManager = 6,
    Receptionist = 7
}
```

### `src/TenantCore.Shared/Enums/Gender.cs`
```csharp
namespace TenantCore.Shared.Enums;

public enum Gender { Male = 1, Female = 2, Other = 3 }
```

### `src/TenantCore.Shared/Enums/OpdStatus.cs`
```csharp
namespace TenantCore.Shared.Enums;

public enum OpdStatus { Pending = 1, InProgress = 2, Completed = 3, Cancelled = 4 }
```

### `src/TenantCore.Shared/Enums/IpdStatus.cs`
```csharp
namespace TenantCore.Shared.Enums;

public enum IpdStatus { Admitted = 1, Discharged = 2, Transferred = 3, Cancelled = 4 }
```

### `src/TenantCore.Shared/Dtos/PatientDto.cs`
```csharp
using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class PatientDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateOnly DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? AadhaarNumber { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Address { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreatePatientDto(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string PhoneNumber,
    string? Email,
    string? AadhaarNumber,
    string? PhotoUrl,
    string? Address);

public sealed record UpdatePatientDto(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string PhoneNumber,
    string? Email,
    string? AadhaarNumber,
    string? PhotoUrl,
    string? Address);
```

### `src/TenantCore.Shared/Dtos/OpdRegistrationDto.cs`
```csharp
using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class OpdRegistrationDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid DoctorUserId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string RegistrationNumber { get; init; } = string.Empty;
    public DateTime RegistrationDate { get; init; }
    public decimal Fee { get; init; }
    public OpdStatus Status { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateOpdRegistrationDto(
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    decimal? Fee,
    string? Notes);

public sealed record UpdateOpdRegistrationDto(
    Guid DoctorUserId,
    string DoctorName,
    decimal Fee,
    OpdStatus Status,
    string? Notes);
```

### `src/TenantCore.Shared/Dtos/IpdRegistrationDto.cs`
```csharp
using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class IpdRegistrationDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid DoctorUserId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string AdmissionNumber { get; init; } = string.Empty;
    public DateTime AdmissionDate { get; init; }
    public DateTime? DischargeDate { get; init; }
    public string? WardName { get; init; }
    public string? RoomNumber { get; init; }
    public string? BedNumber { get; init; }
    public decimal InitialFee { get; init; }
    public IpdStatus Status { get; init; }
    public string? AdmissionNotes { get; init; }
    public string? DischargeNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateIpdRegistrationDto(
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    string? WardName,
    string? RoomNumber,
    string? BedNumber,
    decimal InitialFee,
    string? AdmissionNotes);

public sealed record UpdateIpdRegistrationDto(
    Guid DoctorUserId,
    string DoctorName,
    string? WardName,
    string? RoomNumber,
    string? BedNumber,
    IpdStatus Status,
    string? AdmissionNotes);

public sealed record DischargePatientDto(string? DischargeNotes);
```

### `src/TenantCore.Shared/Dtos/ClinicFeeConfigDto.cs`
```csharp
namespace TenantCore.Shared.Dtos;

public class ClinicFeeConfigDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public decimal OpdFee { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record UpdateClinicFeeConfigDto(decimal OpdFee);
```

### `src/TenantCore.Shared/Dtos/DoctorDto.cs`
```csharp
namespace TenantCore.Shared.Dtos;

public class DoctorDto
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
}
```

---

### `src/TenantCore.Domain/Entities/Patient.cs`
```csharp
using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class Patient : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? AadhaarNumber { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    private Patient() { }

    public static Patient Create(
        Guid tenantId,
        string firstName, string lastName,
        DateOnly dateOfBirth, Gender gender,
        string phoneNumber, string? email,
        string? aadhaarNumber, string? photoUrl, string? address) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = dateOfBirth,
        Gender = gender,
        PhoneNumber = phoneNumber,
        Email = email,
        AadhaarNumber = aadhaarNumber,
        PhotoUrl = photoUrl,
        Address = address,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(
        string firstName, string lastName,
        DateOnly dateOfBirth, Gender gender,
        string phoneNumber, string? email,
        string? aadhaarNumber, string? photoUrl, string? address)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        PhoneNumber = phoneNumber;
        Email = email;
        AadhaarNumber = aadhaarNumber;
        PhotoUrl = photoUrl;
        Address = address;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
}
```

### `src/TenantCore.Domain/Entities/OpdRegistration.cs`
```csharp
using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class OpdRegistration : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorUserId { get; private set; }
    public string DoctorName { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;
    public DateTime RegistrationDate { get; private set; }
    public decimal Fee { get; private set; }
    public OpdStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public Patient Patient { get; private set; } = null!;

    private OpdRegistration() { }

    public static OpdRegistration Create(
        Guid tenantId, Guid patientId,
        Guid doctorUserId, string doctorName,
        string registrationNumber, decimal fee, string? notes) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        PatientId = patientId,
        DoctorUserId = doctorUserId,
        DoctorName = doctorName,
        RegistrationNumber = registrationNumber,
        RegistrationDate = DateTime.UtcNow,
        Fee = fee,
        Status = OpdStatus.Pending,
        Notes = notes,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(Guid doctorUserId, string doctorName, decimal fee, OpdStatus status, string? notes)
    {
        if (Status is OpdStatus.Cancelled or OpdStatus.Completed)
            throw new DomainValidationException("Cannot update a completed or cancelled OPD registration.");

        DoctorUserId = doctorUserId;
        DoctorName = doctorName;
        Fee = fee;
        Status = status;
        Notes = notes;
        SetUpdatedAt();
    }
}
```

### `src/TenantCore.Domain/Entities/IpdRegistration.cs`
```csharp
using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class IpdRegistration : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorUserId { get; private set; }
    public string DoctorName { get; private set; } = string.Empty;
    public string AdmissionNumber { get; private set; } = string.Empty;
    public DateTime AdmissionDate { get; private set; }
    public DateTime? DischargeDate { get; private set; }
    public string? WardName { get; private set; }
    public string? RoomNumber { get; private set; }
    public string? BedNumber { get; private set; }
    public decimal InitialFee { get; private set; }
    public IpdStatus Status { get; private set; }
    public string? AdmissionNotes { get; private set; }
    public string? DischargeNotes { get; private set; }

    public Patient Patient { get; private set; } = null!;

    private IpdRegistration() { }

    public static IpdRegistration Create(
        Guid tenantId, Guid patientId,
        Guid doctorUserId, string doctorName,
        string admissionNumber, string? wardName,
        string? roomNumber, string? bedNumber,
        decimal initialFee, string? admissionNotes) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        PatientId = patientId,
        DoctorUserId = doctorUserId,
        DoctorName = doctorName,
        AdmissionNumber = admissionNumber,
        AdmissionDate = DateTime.UtcNow,
        WardName = wardName,
        RoomNumber = roomNumber,
        BedNumber = bedNumber,
        InitialFee = initialFee,
        Status = IpdStatus.Admitted,
        AdmissionNotes = admissionNotes,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(
        Guid doctorUserId, string doctorName,
        string? wardName, string? roomNumber, string? bedNumber,
        IpdStatus status, string? admissionNotes)
    {
        if (Status is IpdStatus.Discharged or IpdStatus.Cancelled)
            throw new DomainValidationException("Cannot update a discharged or cancelled IPD registration.");

        DoctorUserId = doctorUserId;
        DoctorName = doctorName;
        WardName = wardName;
        RoomNumber = roomNumber;
        BedNumber = bedNumber;
        Status = status;
        AdmissionNotes = admissionNotes;
        SetUpdatedAt();
    }

    public void Discharge(string? dischargeNotes)
    {
        if (Status != IpdStatus.Admitted)
            throw new DomainValidationException("Patient must be in Admitted status to discharge.");

        Status = IpdStatus.Discharged;
        DischargeDate = DateTime.UtcNow;
        DischargeNotes = dischargeNotes;
        SetUpdatedAt();
    }
}
```

### `src/TenantCore.Domain/Entities/ClinicFeeConfig.cs`
```csharp
using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class ClinicFeeConfig : BaseEntity
{
    public Guid TenantId { get; private set; }
    public decimal OpdFee { get; private set; }

    private ClinicFeeConfig() { }

    public static ClinicFeeConfig Create(Guid tenantId, decimal opdFee) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        OpdFee = opdFee,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(decimal opdFee)
    {
        OpdFee = opdFee;
        SetUpdatedAt();
    }
}
```

---

### `src/TenantCore.Domain/Interfaces/IClinicRepository.cs`
```csharp
using TenantCore.Domain.Common;

namespace TenantCore.Domain.Interfaces;

// Marker interface — clinic repos extend both IRepository<T> and this
// so DI can distinguish clinic vs app repos if needed.
public interface IClinicRepository<T> : IRepository<T> where T : BaseEntity
{
}
```

### `src/TenantCore.Domain/Interfaces/IPatientRepository.cs`
```csharp
using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IPatientRepository : IClinicRepository<Patient>
{
    Task<(IEnumerable<Patient> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search, CancellationToken ct = default);

    Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone, CancellationToken ct = default);
}
```

### `src/TenantCore.Domain/Interfaces/IOpdRegistrationRepository.cs`
```csharp
using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IOpdRegistrationRepository : IClinicRepository<OpdRegistration>
{
    Task<(IEnumerable<OpdRegistration> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search, CancellationToken ct = default);

    Task<int> CountTodayAsync(Guid tenantId, CancellationToken ct = default);
}
```

### `src/TenantCore.Domain/Interfaces/IIpdRegistrationRepository.cs`
```csharp
using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IIpdRegistrationRepository : IClinicRepository<IpdRegistration>
{
    Task<(IEnumerable<IpdRegistration> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search, CancellationToken ct = default);

    Task<int> CountTodayAsync(Guid tenantId, CancellationToken ct = default);
}
```

### `src/TenantCore.Domain/Interfaces/IClinicFeeConfigRepository.cs`
```csharp
using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IClinicFeeConfigRepository : IClinicRepository<ClinicFeeConfig>
{
    Task<ClinicFeeConfig?> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
```

---

### `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence;

public class ClinicDbContext(DbContextOptions<ClinicDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<OpdRegistration> OpdRegistrations => Set<OpdRegistration>();
    public DbSet<IpdRegistration> IpdRegistrations => Set<IpdRegistration>();
    public DbSet<ClinicFeeConfig> ClinicFeeConfigs => Set<ClinicFeeConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // No other DbContext in the assembly — scan all configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicDbContext).Assembly);
    }
}
```

### `src/TenantCore.Infrastructure/Persistence/ClinicDbContextFactory.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TenantCore.Infrastructure.Persistence;

public class ClinicDbContextFactory : IDesignTimeDbContextFactory<ClinicDbContext>
{
    public ClinicDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClinicDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.\\SQLEXPRESS;Database=TenantClinicDb;Trusted_Connection=True;TrustServerCertificate=True;",
            b => b.MigrationsAssembly(typeof(ClinicDbContext).Assembly.FullName)
                  .MigrationsHistoryTable("__EFMigrationsHistory", "clinic"));
        return new ClinicDbContext(optionsBuilder.Options);
    }
}
```

---

### `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/PatientConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", "clinic");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.TenantId);

        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.DateOfBirth).IsRequired();
        builder.Property(p => p.Gender).IsRequired();
        builder.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.AadhaarNumber).HasMaxLength(12);
        builder.Property(p => p.PhotoUrl).HasMaxLength(500);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.UpdatedBy).HasMaxLength(256);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasIndex(p => new { p.TenantId, p.PhoneNumber });
    }
}
```

### `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/OpdRegistrationConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class OpdRegistrationConfiguration : IEntityTypeConfiguration<OpdRegistration>
{
    public void Configure(EntityTypeBuilder<OpdRegistration> builder)
    {
        builder.ToTable("OpdRegistrations", "clinic");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.TenantId).IsRequired();
        builder.HasIndex(o => o.TenantId);

        builder.Property(o => o.PatientId).IsRequired();
        builder.Property(o => o.DoctorUserId).IsRequired();
        builder.Property(o => o.DoctorName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.RegistrationNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(o => new { o.TenantId, o.RegistrationNumber }).IsUnique();
        builder.Property(o => o.RegistrationDate).IsRequired();
        builder.Property(o => o.Fee).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.CreatedBy).HasMaxLength(256);
        builder.Property(o => o.UpdatedBy).HasMaxLength(256);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne(o => o.Patient)
               .WithMany()
               .HasForeignKey(o => o.PatientId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/IpdRegistrationConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class IpdRegistrationConfiguration : IEntityTypeConfiguration<IpdRegistration>
{
    public void Configure(EntityTypeBuilder<IpdRegistration> builder)
    {
        builder.ToTable("IpdRegistrations", "clinic");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.TenantId).IsRequired();
        builder.HasIndex(i => i.TenantId);

        builder.Property(i => i.PatientId).IsRequired();
        builder.Property(i => i.DoctorUserId).IsRequired();
        builder.Property(i => i.DoctorName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.AdmissionNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(i => new { i.TenantId, i.AdmissionNumber }).IsUnique();
        builder.Property(i => i.AdmissionDate).IsRequired();
        builder.Property(i => i.WardName).HasMaxLength(100);
        builder.Property(i => i.RoomNumber).HasMaxLength(20);
        builder.Property(i => i.BedNumber).HasMaxLength(20);
        builder.Property(i => i.InitialFee).IsRequired().HasPrecision(18, 2);
        builder.Property(i => i.Status).IsRequired();
        builder.Property(i => i.AdmissionNotes).HasMaxLength(1000);
        builder.Property(i => i.DischargeNotes).HasMaxLength(1000);

        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.CreatedBy).HasMaxLength(256);
        builder.Property(i => i.UpdatedBy).HasMaxLength(256);
        builder.Property(i => i.RowVersion).IsRowVersion();

        builder.HasOne(i => i.Patient)
               .WithMany()
               .HasForeignKey(i => i.PatientId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/ClinicFeeConfigConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicFeeConfigConfiguration : IEntityTypeConfiguration<ClinicFeeConfig>
{
    public void Configure(EntityTypeBuilder<ClinicFeeConfig> builder)
    {
        builder.ToTable("ClinicFeeConfigs", "clinic");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId).IsRequired();
        builder.HasIndex(c => c.TenantId).IsUnique();

        builder.Property(c => c.OpdFee).IsRequired().HasPrecision(18, 2);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
```

---

### `src/TenantCore.Infrastructure/Repositories/ClinicRepository.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Common;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicRepository<T>(ClinicDbContext dbContext) : IClinicRepository<T>
    where T : BaseEntity
{
    protected readonly ClinicDbContext DbContext = dbContext;
    protected readonly DbSet<T> DbSet = dbContext.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Delete(T entity) => DbSet.Remove(entity);

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await DbSet.AnyAsync(e => e.Id == id, ct);

    public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await DbContext.SaveChangesAsync(ct);
}
```

### `src/TenantCore.Infrastructure/Repositories/PatientRepository.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class PatientRepository(ClinicDbContext dbContext)
    : ClinicRepository<Patient>(dbContext), IPatientRepository
{
    public async Task<(IEnumerable<Patient> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet.Where(p => p.TenantId == tenantId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.FirstName.Contains(search) ||
                p.LastName.Contains(search) ||
                p.PhoneNumber.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(
            p => p.TenantId == tenantId && p.PhoneNumber == phone, ct);
}
```

### `src/TenantCore.Infrastructure/Repositories/OpdRegistrationRepository.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class OpdRegistrationRepository(ClinicDbContext dbContext)
    : ClinicRepository<OpdRegistration>(dbContext), IOpdRegistrationRepository
{
    public async Task<(IEnumerable<OpdRegistration> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet
            .Include(o => o.Patient)
            .Where(o => o.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o =>
                o.RegistrationNumber.Contains(search) ||
                o.DoctorName.Contains(search) ||
                o.Patient.FirstName.Contains(search) ||
                o.Patient.LastName.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.RegistrationDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> CountTodayAsync(Guid tenantId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet.CountAsync(
            o => o.TenantId == tenantId && o.RegistrationDate.Date == today, ct);
    }
}
```

### `src/TenantCore.Infrastructure/Repositories/IpdRegistrationRepository.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class IpdRegistrationRepository(ClinicDbContext dbContext)
    : ClinicRepository<IpdRegistration>(dbContext), IIpdRegistrationRepository
{
    public async Task<(IEnumerable<IpdRegistration> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet
            .Include(i => i.Patient)
            .Where(i => i.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.AdmissionNumber.Contains(search) ||
                i.DoctorName.Contains(search) ||
                i.Patient.FirstName.Contains(search) ||
                i.Patient.LastName.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.AdmissionDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> CountTodayAsync(Guid tenantId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet.CountAsync(
            i => i.TenantId == tenantId && i.AdmissionDate.Date == today, ct);
    }
}
```

### `src/TenantCore.Infrastructure/Repositories/ClinicFeeConfigRepository.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicFeeConfigRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicFeeConfig>(dbContext), IClinicFeeConfigRepository
{
    public async Task<ClinicFeeConfig?> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);
}
```

---

### Modify `src/TenantCore.Infrastructure/DependencyInjection.cs`
Remove the `AppDbContext` and `ITenantRepository` registrations and replace with `ClinicDbContext`:
```csharp
// Remove these lines:
// services.AddDbContext<AppDbContext>(options => options.UseSqlServer(...));
// services.AddScoped<ITenantRepository, TenantRepository>();

// Add:
services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("ClinicConnection"),
        b => b.MigrationsAssembly(typeof(ClinicDbContext).Assembly.FullName)
              .MigrationsHistoryTable("__EFMigrationsHistory", "clinic")));

services.AddScoped<IPatientRepository, PatientRepository>();
services.AddScoped<IOpdRegistrationRepository, OpdRegistrationRepository>();
services.AddScoped<IIpdRegistrationRepository, IpdRegistrationRepository>();
services.AddScoped<IClinicFeeConfigRepository, ClinicFeeConfigRepository>();
```

### Modify `src/TenantCore.Api/appsettings.json`
Replace `DefaultConnection` with `ClinicConnection` (remove the old entry, add the new one):
```json
"ConnectionStrings": {
  "ClinicConnection": "Server=.\\SQLEXPRESS;Database=TenantClinicDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

---

### Application Layer — Translators

### `src/TenantCore.Application/Features/Patients/Translators/PatientTranslator.cs`
```csharp
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Translators;

public static class PatientTranslator
{
    public static PatientDto ToDto(Patient entity) => new()
    {
        Id = entity.Id,
        TenantId = entity.TenantId,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        DateOfBirth = entity.DateOfBirth,
        Gender = entity.Gender,
        PhoneNumber = entity.PhoneNumber,
        Email = entity.Email,
        AadhaarNumber = entity.AadhaarNumber,
        PhotoUrl = entity.PhotoUrl,
        Address = entity.Address,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };
}
```

### `src/TenantCore.Application/Features/OpdRegistrations/Translators/OpdRegistrationTranslator.cs`
```csharp
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Translators;

public static class OpdRegistrationTranslator
{
    public static OpdRegistrationDto ToDto(OpdRegistration entity) => new()
    {
        Id = entity.Id,
        TenantId = entity.TenantId,
        PatientId = entity.PatientId,
        PatientName = entity.Patient is not null
            ? $"{entity.Patient.FirstName} {entity.Patient.LastName}"
            : string.Empty,
        DoctorUserId = entity.DoctorUserId,
        DoctorName = entity.DoctorName,
        RegistrationNumber = entity.RegistrationNumber,
        RegistrationDate = entity.RegistrationDate,
        Fee = entity.Fee,
        Status = entity.Status,
        Notes = entity.Notes,
        CreatedAt = entity.CreatedAt
    };
}
```

`IpdRegistrationTranslator` follows the same shape — map all IPD properties including `AdmissionNumber`, `DischargeDate`, `WardName`, `RoomNumber`, `BedNumber`, `Status`, etc.

`ClinicFeeConfigTranslator` maps `Id`, `TenantId`, `OpdFee`, `UpdatedAt`.

---

### Application Layer — Commands & Queries

### `src/TenantCore.Application/Features/Patients/Commands/RegisterPatientCommand.cs`
```csharp
using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Patients.Commands;

public sealed record RegisterPatientCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string PhoneNumber,
    string? Email,
    string? AadhaarNumber,
    string? PhotoUrl,
    string? Address) : IRequest<PatientDto>;
```

### `src/TenantCore.Application/Features/OpdRegistrations/Commands/CreateOpdRegistrationCommand.cs`
```csharp
using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Commands;

public sealed record CreateOpdRegistrationCommand(
    Guid TenantId,
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    decimal? Fee,
    string? Notes) : IRequest<OpdRegistrationDto>;
```

### `src/TenantCore.Application/Features/IpdRegistrations/Commands/CreateIpdRegistrationCommand.cs`
```csharp
using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Commands;

public sealed record CreateIpdRegistrationCommand(
    Guid TenantId,
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    string? WardName,
    string? RoomNumber,
    string? BedNumber,
    decimal InitialFee,
    string? AdmissionNotes) : IRequest<IpdRegistrationDto>;
```

### `src/TenantCore.Application/Features/IpdRegistrations/Commands/DischargePatientCommand.cs`
```csharp
using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Commands;

public sealed record DischargePatientCommand(Guid Id, Guid TenantId, string? DischargeNotes)
    : IRequest<IpdRegistrationDto>;
```

### `src/TenantCore.Application/Features/Clinics/Queries/GetClinicDoctorsQuery.cs`
```csharp
using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Clinics.Queries;

public sealed record GetClinicDoctorsQuery(string ApplicationId) : IRequest<IEnumerable<DoctorDto>>;
```

---

### Application Layer — Handlers

### `src/TenantCore.Application/Features/Patients/Handlers/RegisterPatientHandler.cs`
```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class RegisterPatientHandler(
    IPatientRepository repository,
    ILogger<RegisterPatientHandler> logger)
    : IRequestHandler<RegisterPatientCommand, PatientDto>
{
    public async Task<PatientDto> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering patient {FirstName} {LastName} for tenant {TenantId}",
            request.FirstName, request.LastName, request.TenantId);

        var patient = Patient.Create(
            request.TenantId, request.FirstName, request.LastName,
            request.DateOfBirth, request.Gender, request.PhoneNumber,
            request.Email, request.AadhaarNumber, request.PhotoUrl, request.Address);

        await repository.AddAsync(patient, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return PatientTranslator.ToDto(patient);
    }
}
```

### `src/TenantCore.Application/Features/OpdRegistrations/Handlers/CreateOpdRegistrationHandler.cs`
```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class CreateOpdRegistrationHandler(
    IOpdRegistrationRepository opdRepository,
    IPatientRepository patientRepository,
    IClinicFeeConfigRepository feeRepository,
    ILogger<CreateOpdRegistrationHandler> logger)
    : IRequestHandler<CreateOpdRegistrationCommand, OpdRegistrationDto>
{
    public async Task<OpdRegistrationDto> Handle(
        CreateOpdRegistrationCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);

        // Resolve fee: use provided fee or fall back to clinic config
        var fee = request.Fee;
        if (fee is null)
        {
            var config = await feeRepository.GetByTenantAsync(request.TenantId, cancellationToken);
            fee = config?.OpdFee ?? 0m;
        }

        // Generate registration number: OPD-yyyyMMdd-XXXX (daily sequence)
        var todayCount = await opdRepository.CountTodayAsync(request.TenantId, cancellationToken);
        var registrationNumber = $"OPD-{DateTime.UtcNow:yyyyMMdd}-{(todayCount + 1):D4}";

        var registration = OpdRegistration.Create(
            request.TenantId, request.PatientId,
            request.DoctorUserId, request.DoctorName,
            registrationNumber, fee.Value, request.Notes);

        await opdRepository.AddAsync(registration, cancellationToken);
        await opdRepository.SaveChangesAsync(cancellationToken);

        // Reload with Patient navigation for DTO
        var loaded = await opdRepository.GetByIdAsync(registration.Id, cancellationToken);
        return OpdRegistrationTranslator.ToDto(loaded!);
    }
}
```

> **Note:** The reload above is needed because the navigation property `Patient` is not populated on the newly created entity. Alternatively, use a dedicated query that includes the navigation.

### `src/TenantCore.Application/Features/IpdRegistrations/Handlers/CreateIpdRegistrationHandler.cs`
Same pattern as OPD: resolve patient, generate `IPD-{yyyyMMdd}-{sequence}` using `CountTodayAsync`, create entity, save, reload.

### `src/TenantCore.Application/Features/IpdRegistrations/Handlers/DischargePatientHandler.cs`
```csharp
using MediatR;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Handlers;

public sealed class DischargePatientHandler(IIpdRegistrationRepository repository)
    : IRequestHandler<DischargePatientCommand, IpdRegistrationDto>
{
    public async Task<IpdRegistrationDto> Handle(
        DischargePatientCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IpdRegistration), request.Id);

        if (registration.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        registration.Discharge(request.DischargeNotes);
        repository.Update(registration);
        await repository.SaveChangesAsync(cancellationToken);

        return IpdRegistrationTranslator.ToDto(registration);
    }
}
```

### `src/TenantCore.Application/Features/ClinicSettings/Handlers/UpdateClinicFeeConfigHandler.cs`
```csharp
using MediatR;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class UpdateClinicFeeConfigHandler(IClinicFeeConfigRepository repository)
    : IRequestHandler<UpdateClinicFeeConfigCommand, ClinicFeeConfigDto>
{
    public async Task<ClinicFeeConfigDto> Handle(
        UpdateClinicFeeConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByTenantAsync(request.TenantId, cancellationToken);

        if (config is null)
        {
            config = ClinicFeeConfig.Create(request.TenantId, request.OpdFee);
            await repository.AddAsync(config, cancellationToken);
        }
        else
        {
            config.Update(request.OpdFee);
            repository.Update(config);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ClinicFeeConfigTranslator.ToDto(config);
    }
}
```

### `src/TenantCore.Application/Features/Clinics/Handlers/GetClinicDoctorsHandler.cs`
```csharp
using MediatR;
using TenantCore.Application.Features.Clinics.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Clinics.Handlers;

public sealed class GetClinicDoctorsHandler(IAuthApplicationService authService)
    : IRequestHandler<GetClinicDoctorsQuery, IEnumerable<DoctorDto>>
{
    public async Task<IEnumerable<DoctorDto>> Handle(
        GetClinicDoctorsQuery request, CancellationToken cancellationToken)
    {
        var users = await authService.GetApplicationUsersAsync(request.ApplicationId, cancellationToken);

        return users
            .Where(u => u.Roles.Contains("Doctor", StringComparer.OrdinalIgnoreCase))
            .Select(u => new DoctorDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email
            });
    }
}
```

> Adjust `u.Roles`, `u.UserId`, `u.FullName`, `u.Email` to match the actual property names of `ApplicationUserResponseDto`.

---

### Validators

### `src/TenantCore.Application/Features/Patients/Validators/RegisterPatientCommandValidator.cs`
```csharp
using FluentValidation;
using TenantCore.Application.Features.Patients.Commands;

namespace TenantCore.Application.Features.Patients.Validators;

public sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-]+$").WithMessage("Phone number format is invalid.");
        RuleFor(x => x.AadhaarNumber)
            .Length(12).Matches(@"^[0-9]{12}$")
            .WithMessage("Aadhaar number must be exactly 12 digits.")
            .When(x => !string.IsNullOrEmpty(x.AadhaarNumber));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
```

### `src/TenantCore.Application/Features/OpdRegistrations/Validators/CreateOpdRegistrationCommandValidator.cs`
```csharp
using FluentValidation;
using TenantCore.Application.Features.OpdRegistrations.Commands;

namespace TenantCore.Application.Features.OpdRegistrations.Validators;

public sealed class CreateOpdRegistrationCommandValidator : AbstractValidator<CreateOpdRegistrationCommand>
{
    public CreateOpdRegistrationCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Fee).GreaterThanOrEqualTo(0).When(x => x.Fee.HasValue);
    }
}
```

---

### Controllers

### `src/TenantCore.Api/Controllers/PatientsController.cs`
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class PatientsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PatientDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var tenantId = GetTenantId();
        return Ok(await sender.Send(new GetPatientsQuery(tenantId, page, pageSize, search), ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetPatientByIdQuery(id, GetTenantId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireManagement)]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreatePatientDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterPatientCommand(
            GetTenantId(), dto.FirstName, dto.LastName, dto.DateOfBirth,
            dto.Gender, dto.PhoneNumber, dto.Email,
            dto.AadhaarNumber, dto.PhotoUrl, dto.Address), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireManagement)]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdatePatientCommand(
            id, GetTenantId(), dto.FirstName, dto.LastName, dto.DateOfBirth,
            dto.Gender, dto.PhoneNumber, dto.Email,
            dto.AadhaarNumber, dto.PhotoUrl, dto.Address), ct));

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tid") ?? User.FindFirst("tenantId");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
```

### `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs`
```csharp
[ApiController]
[Route("api/opd-registrations")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class OpdRegistrationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default)
        => Ok(await sender.Send(new GetOpdRegistrationsQuery(GetTenantId(), page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetOpdRegistrationByIdQuery(id, GetTenantId()), ct));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireManagement)]
    [ProducesResponseType(typeof(OpdRegistrationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOpdRegistrationDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateOpdRegistrationCommand(
            GetTenantId(), dto.PatientId, dto.DoctorUserId,
            dto.DoctorName, dto.Fee, dto.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireManagement)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOpdRegistrationDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateOpdRegistrationCommand(
            id, GetTenantId(), dto.DoctorUserId, dto.DoctorName,
            dto.Fee, dto.Status, dto.Notes), ct));

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tid") ?? User.FindFirst("tenantId");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
```

### `src/TenantCore.Api/Controllers/IpdRegistrationsController.cs`
Same pattern as OpdRegistrationsController plus a `PATCH /{id}/discharge` endpoint:
```csharp
[HttpPatch("{id:guid}/discharge")]
[Authorize(Policy = AuthPolicies.RequireManagement)]
[ProducesResponseType(typeof(IpdRegistrationDto), StatusCodes.Status200OK)]
public async Task<IActionResult> Discharge(Guid id, [FromBody] DischargePatientDto dto, CancellationToken ct)
    => Ok(await sender.Send(new DischargePatientCommand(id, GetTenantId(), dto.DischargeNotes), ct));
```

### `src/TenantCore.Api/Controllers/ClinicSettingsController.cs`
```csharp
[ApiController]
[Route("api/clinic-settings")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class ClinicSettingsController(ISender sender) : ControllerBase
{
    [HttpGet("fees")]
    public async Task<IActionResult> GetFees(CancellationToken ct)
        => Ok(await sender.Send(new GetClinicFeeConfigQuery(GetTenantId()), ct));

    [HttpPut("fees")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    public async Task<IActionResult> UpdateFees([FromBody] UpdateClinicFeeConfigDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateClinicFeeConfigCommand(GetTenantId(), dto.OpdFee), ct));

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors(CancellationToken ct)
    {
        var applicationId = User.FindFirst("applicationId")?.Value ?? string.Empty;
        return Ok(await sender.Send(new GetClinicDoctorsQuery(applicationId), ct));
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tid") ?? User.FindFirst("tenantId");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
```

---

### `src/TenantCore.Web.Client/Clients/ClinicApiClient.cs`
```csharp
using System.Net.Http.Json;
using TenantCore.Shared.Dtos;

namespace TenantCore.Web.Client.Clients;

public class ClinicApiClient(HttpClient httpClient)
{
    // Patients
    public Task<PagedResult<PatientDto>?> GetPatientsAsync(int page = 1, int pageSize = 20, string? search = null)
        => httpClient.GetFromJsonAsync<PagedResult<PatientDto>>(
            $"api/patients?page={page}&pageSize={pageSize}&search={search}");

    public Task<PatientDto?> GetPatientByIdAsync(Guid id)
        => httpClient.GetFromJsonAsync<PatientDto>($"api/patients/{id}");

    public async Task<PatientDto?> RegisterPatientAsync(CreatePatientDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/patients", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PatientDto>();
    }

    public async Task<PatientDto?> UpdatePatientAsync(Guid id, UpdatePatientDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"api/patients/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PatientDto>();
    }

    // OPD
    public Task<PagedResult<OpdRegistrationDto>?> GetOpdRegistrationsAsync(
        int page = 1, int pageSize = 20, string? search = null)
        => httpClient.GetFromJsonAsync<PagedResult<OpdRegistrationDto>>(
            $"api/opd-registrations?page={page}&pageSize={pageSize}&search={search}");

    public Task<OpdRegistrationDto?> GetOpdRegistrationByIdAsync(Guid id)
        => httpClient.GetFromJsonAsync<OpdRegistrationDto>($"api/opd-registrations/{id}");

    public async Task<OpdRegistrationDto?> CreateOpdRegistrationAsync(CreateOpdRegistrationDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/opd-registrations", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OpdRegistrationDto>();
    }

    // IPD
    public Task<PagedResult<IpdRegistrationDto>?> GetIpdRegistrationsAsync(
        int page = 1, int pageSize = 20, string? search = null)
        => httpClient.GetFromJsonAsync<PagedResult<IpdRegistrationDto>>(
            $"api/ipd-registrations?page={page}&pageSize={pageSize}&search={search}");

    public async Task<IpdRegistrationDto?> CreateIpdRegistrationAsync(CreateIpdRegistrationDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/ipd-registrations", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IpdRegistrationDto>();
    }

    public async Task<IpdRegistrationDto?> DischargePatientAsync(Guid id, DischargePatientDto dto)
    {
        var response = await httpClient.PatchAsJsonAsync($"api/ipd-registrations/{id}/discharge", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IpdRegistrationDto>();
    }

    // Clinic Settings
    public Task<ClinicFeeConfigDto?> GetFeesAsync()
        => httpClient.GetFromJsonAsync<ClinicFeeConfigDto>("api/clinic-settings/fees");

    public async Task<ClinicFeeConfigDto?> UpdateFeesAsync(UpdateClinicFeeConfigDto dto)
    {
        var response = await httpClient.PutAsJsonAsync("api/clinic-settings/fees", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClinicFeeConfigDto>();
    }

    public Task<IEnumerable<DoctorDto>?> GetDoctorsAsync()
        => httpClient.GetFromJsonAsync<IEnumerable<DoctorDto>>("api/clinic-settings/doctors");
}
```

---

### `src/TenantCore.Web.Client/Pages/Opd/OpdRegistration.razor`
```razor
@page "/opd/register"
@using TenantCore.Shared.Dtos
@inject ClinicApiClient ClinicApi
@inject NavigationManager Nav

<h3>OPD Registration</h3>

@if (_doctors is null || _patients is null)
{
    <p>Loading...</p>
}
else
{
    <EditForm Model="_form" OnValidSubmit="HandleSubmit">
        <DataAnnotationsValidator />
        <ValidationSummary />

        <div class="mb-3">
            <label>Patient</label>
            <InputSelect @bind-Value="_form.PatientId" class="form-select">
                <option value="">-- Select Patient --</option>
                @foreach (var p in _patients.Items)
                {
                    <option value="@p.Id">@p.FullName (@p.PhoneNumber)</option>
                }
            </InputSelect>
        </div>

        <div class="mb-3">
            <label>Doctor</label>
            <InputSelect @bind-Value="_selectedDoctorId" class="form-select"
                         @onchange="OnDoctorChanged">
                <option value="">-- Select Doctor --</option>
                @foreach (var d in _doctors)
                {
                    <option value="@d.UserId">@d.FullName</option>
                }
            </InputSelect>
        </div>

        <div class="mb-3">
            <label>Fee (₹) — leave blank to use clinic default</label>
            <InputNumber @bind-Value="_form.Fee" class="form-control" />
        </div>

        <div class="mb-3">
            <label>Notes</label>
            <InputTextArea @bind-Value="_form.Notes" class="form-control" rows="3" />
        </div>

        <button type="submit" class="btn btn-primary" disabled="@_submitting">Register</button>
    </EditForm>

    @if (_error is not null)
    {
        <div class="alert alert-danger mt-3">@_error</div>
    }
}

@code {
    private CreateOpdRegistrationDto _form = new(Guid.Empty, Guid.Empty, string.Empty, null, null);
    private Guid _selectedDoctorId;
    private PagedResult<PatientDto>? _patients;
    private IEnumerable<DoctorDto>? _doctors;
    private bool _submitting;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        _patients = await ClinicApi.GetPatientsAsync(pageSize: 100);
        _doctors = await ClinicApi.GetDoctorsAsync();
    }

    private void OnDoctorChanged(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var id))
        {
            _selectedDoctorId = id;
            var doctor = _doctors?.FirstOrDefault(d => d.UserId == id);
            _form = _form with { DoctorUserId = id, DoctorName = doctor?.FullName ?? string.Empty };
        }
    }

    private async Task HandleSubmit()
    {
        _submitting = true;
        _error = null;
        try
        {
            var result = await ClinicApi.CreateOpdRegistrationAsync(_form);
            Nav.NavigateTo($"/opd/{result!.Id}");
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally { _submitting = false; }
    }
}
```

### `src/TenantCore.Web.Client/Pages/Ipd/IpdRegistration.razor`
Same structure as OpdRegistration.razor but uses `CreateIpdRegistrationDto` — adds ward, room, bed number fields and uses `ClinicApi.CreateIpdRegistrationAsync`.

### `src/TenantCore.Web.Client/Pages/Patients/PatientList.razor`
Standard list page with search input, paged table showing `FullName`, `PhoneNumber`, `Gender`, `DateOfBirth`. "Register New Patient" button navigates to `/patients/register`.

---

## Implementation Order

**Phase 1 — Delete old tenant-management code**

1. Delete `src/TenantCore.Api/Controllers/TenantsController.cs`
2. Delete `src/TenantCore.Web.Client/Pages/Tenants/TenantList.razor` (and any sub-pages in that folder)
3. Delete `src/TenantCore.Application/Features/Tenants/` entire folder
4. Delete `src/TenantCore.Domain/Entities/Tenant.cs`
5. Delete `src/TenantCore.Domain/Interfaces/ITenantRepository.cs`
6. Delete `src/TenantCore.Shared/Dtos/TenantDto.cs` (includes CreateTenantDto, UpdateTenantDto)
7. Delete `src/TenantCore.Infrastructure/Repositories/TenantRepository.cs`
8. Delete `src/TenantCore.Infrastructure/Persistence/Configurations/TenantConfiguration.cs`
9. Delete `src/TenantCore.Infrastructure/Persistence/AppDbContext.cs`
10. Delete `src/TenantCore.Infrastructure/Persistence/Migrations/` entire folder
11. `src/TenantCore.Infrastructure/DependencyInjection.cs` — remove `AppDbContext` and `ITenantRepository` registrations
12. `src/TenantCore.Api/appsettings.json` + `appsettings.Development.json` — replace `DefaultConnection` with `ClinicConnection`

**Phase 2 — Create clinic Shared layer**

13. `src/TenantCore.Shared/Enums/ClinicRole.cs` — create enum
14. `src/TenantCore.Shared/Enums/Gender.cs` — create enum
15. `src/TenantCore.Shared/Enums/OpdStatus.cs` — create enum
16. `src/TenantCore.Shared/Enums/IpdStatus.cs` — create enum
17. `src/TenantCore.Shared/Dtos/PatientDto.cs` — create (includes CreatePatientDto, UpdatePatientDto)
18. `src/TenantCore.Shared/Dtos/OpdRegistrationDto.cs` — create
19. `src/TenantCore.Shared/Dtos/IpdRegistrationDto.cs` — create
20. `src/TenantCore.Shared/Dtos/ClinicFeeConfigDto.cs` — create
21. `src/TenantCore.Shared/Dtos/DoctorDto.cs` — create

**Phase 3 — Domain**

22. `src/TenantCore.Domain/Entities/Patient.cs` — create entity
23. `src/TenantCore.Domain/Entities/OpdRegistration.cs` — create entity
24. `src/TenantCore.Domain/Entities/IpdRegistration.cs` — create entity
25. `src/TenantCore.Domain/Entities/ClinicFeeConfig.cs` — create entity
26. `src/TenantCore.Domain/Interfaces/IClinicRepository.cs` — create marker interface
27. `src/TenantCore.Domain/Interfaces/IPatientRepository.cs` — create interface
28. `src/TenantCore.Domain/Interfaces/IOpdRegistrationRepository.cs` — create interface
29. `src/TenantCore.Domain/Interfaces/IIpdRegistrationRepository.cs` — create interface
30. `src/TenantCore.Domain/Interfaces/IClinicFeeConfigRepository.cs` — create interface

**Phase 4 — Infrastructure**

31. `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/PatientConfiguration.cs` — create
32. `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/OpdRegistrationConfiguration.cs` — create
33. `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/IpdRegistrationConfiguration.cs` — create
34. `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/ClinicFeeConfigConfiguration.cs` — create
35. `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` — create
36. `src/TenantCore.Infrastructure/Persistence/ClinicDbContextFactory.cs` — create (needed for migrations)
37. `src/TenantCore.Infrastructure/Repositories/ClinicRepository.cs` — create generic base
38. `src/TenantCore.Infrastructure/Repositories/PatientRepository.cs` — create
39. `src/TenantCore.Infrastructure/Repositories/OpdRegistrationRepository.cs` — create
40. `src/TenantCore.Infrastructure/Repositories/IpdRegistrationRepository.cs` — create
41. `src/TenantCore.Infrastructure/Repositories/ClinicFeeConfigRepository.cs` — create
42. `src/TenantCore.Infrastructure/DependencyInjection.cs` — add ClinicDbContext + 4 repo registrations

**Phase 5 — Application layer**

43. `src/TenantCore.Application/Features/Patients/Translators/PatientTranslator.cs` — create
44. `src/TenantCore.Application/Features/OpdRegistrations/Translators/OpdRegistrationTranslator.cs` — create
45. `src/TenantCore.Application/Features/IpdRegistrations/Translators/IpdRegistrationTranslator.cs` — create
46. `src/TenantCore.Application/Features/ClinicSettings/Translators/ClinicFeeConfigTranslator.cs` — create
47. All Commands (RegisterPatient, UpdatePatient, CreateOpdRegistration, UpdateOpdRegistration, CreateIpdRegistration, UpdateIpdRegistration, DischargePatient, UpdateClinicFeeConfig) — create
48. All Queries (GetPatients, GetPatientById, GetOpdRegistrations, GetOpdRegistrationById, GetIpdRegistrations, GetIpdRegistrationById, GetClinicFeeConfig, GetClinicDoctors) — create
49. All Handlers — create in order: Patients → OPD → IPD → ClinicSettings → Clinics(doctors)
50. All Validators (RegisterPatient, UpdatePatient, CreateOpdRegistration, CreateIpdRegistration, DischargePatient, UpdateClinicFeeConfig) — create

**Phase 6 — API**

51. `src/TenantCore.Api/Controllers/PatientsController.cs` — create
52. `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` — create
53. `src/TenantCore.Api/Controllers/IpdRegistrationsController.cs` — create
54. `src/TenantCore.Api/Controllers/ClinicSettingsController.cs` — create
55. Run migration:
    ```bash
    dotnet ef migrations add AddClinicEntities \
      --context ClinicDbContext \
      --output-dir Persistence/ClinicMigrations \
      --project src/TenantCore.Infrastructure \
      --startup-project src/TenantCore.Api
    ```

**Phase 7 — Web Client**

56. `src/TenantCore.Web.Client/Clients/ClinicApiClient.cs` — create typed client
57. Register `ClinicApiClient` in `TenantCore.Web.Client` DI (Program.cs)
58. `src/TenantCore.Web.Client/Pages/Patients/PatientList.razor` — create
59. `src/TenantCore.Web.Client/Pages/Opd/OpdRegistration.razor` — create
60. `src/TenantCore.Web.Client/Pages/Ipd/IpdRegistration.razor` — create

---

## Migration Name

`AddClinicEntities`

---

## Security Note

> Aadhaar numbers are PII under Indian law (Aadhaar Act 2016). Before going to production, encrypt `AadhaarNumber` at rest using EF Core value converters with AES-256 or use SQL Server Always Encrypted. This plan stores them in plain text as a starting point only.

---

## Execution Status

- **Status**: Plan fully executed and completed
- **Started**: 2026-04-24
- **Development completed**: 2026-04-24
- **Security check completed**: 2026-04-24
- **Completed**: 2026-04-24
