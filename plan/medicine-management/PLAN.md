# Feature Plan: Medicine Management

## Repo
TenantCore.App

## Overview
Introduces a global medicine catalogue — MedicineTypes and Medicines — shared across all clinics with no per-clinic scoping. Only SystemAdmin and Doctor-level roles can create or update medicines; no delete endpoint is exposed in the clinic portal (reserved for a future super-admin dashboard). Duplicate prevention is enforced on create and update via fuzzy-match search across name, generic name, and brand name.

## Domain Area
`Medicines` / `MedicineTypes` — Api, Application, Domain, Infrastructure, Shared, Web.Client

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/TenantCore.Domain/Entities/MedicineType.cs` | Domain entity for medicine types (Tablet, Syrup, etc.) |
| `src/TenantCore.Domain/Entities/Medicine.cs` | Domain entity for medicines with all columns; nullable FK to MedicineType only |
| `src/TenantCore.Domain/Interfaces/IMedicineTypeRepository.cs` | Repository interface for MedicineType — extends IRepository\<MedicineType\>; adds GetPagedAsync and GetByNameAsync |
| `src/TenantCore.Domain/Interfaces/IMedicineRepository.cs` | Repository interface for Medicine — extends IRepository\<Medicine\>; adds GetPagedAsync with multi-filter and FindSimilarAsync for fuzzy-match duplicate detection |
| `src/TenantCore.Shared/Dtos/MedicineTypeDto.cs` | MedicineTypeDto (read), CreateMedicineTypeDto (write), UpdateMedicineTypeDto (write) |
| `src/TenantCore.Shared/Dtos/MedicineDto.cs` | MedicineDto (read — includes MedicineTypeName), CreateMedicineDto (write), UpdateMedicineDto (write) |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/MedicineTypeConfiguration.cs` | EF config for MedicineTypes table; maps CreatedAt→CreatedDate, UpdatedAt→ModifiedDate, UpdatedBy→ModifiedBy |
| `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/MedicineConfiguration.cs` | EF config for Medicines table; maps all columns; configures nullable FK to MedicineTypes with ON DELETE SET NULL |
| `src/TenantCore.Infrastructure/Repositories/MedicineTypeRepository.cs` | Implements IMedicineTypeRepository directly on ClinicDbContext (global, no ApplicationId filter) |
| `src/TenantCore.Infrastructure/Repositories/MedicineRepository.cs` | Implements IMedicineRepository; GetPagedAsync supports 5 filter params; FindSimilarAsync does case-insensitive contains match on Name, GenericName, and BrandName |
| `src/TenantCore.Application/Features/MedicineTypes/Translators/MedicineTypeTranslator.cs` | Static translator: ToDto, ToDtoList |
| `src/TenantCore.Application/Features/MedicineTypes/Commands/CreateMedicineTypeCommand.cs` | sealed record: Name, Description |
| `src/TenantCore.Application/Features/MedicineTypes/Commands/UpdateMedicineTypeCommand.cs` | sealed record: Id, Name, Description, IsActive |
| `src/TenantCore.Application/Features/MedicineTypes/Queries/GetMedicineTypesQuery.cs` | sealed record: Page, PageSize, Search? |
| `src/TenantCore.Application/Features/MedicineTypes/Queries/GetMedicineTypeByIdQuery.cs` | sealed record: Id |
| `src/TenantCore.Application/Features/MedicineTypes/Handlers/CreateMedicineTypeHandler.cs` | Checks duplicate name; calls AddAsync + SaveChangesAsync |
| `src/TenantCore.Application/Features/MedicineTypes/Handlers/UpdateMedicineTypeHandler.cs` | Loads entity; throws NotFoundException if missing; updates and saves |
| `src/TenantCore.Application/Features/MedicineTypes/Handlers/GetMedicineTypesHandler.cs` | Returns PagedResult\<MedicineTypeDto\> |
| `src/TenantCore.Application/Features/MedicineTypes/Handlers/GetMedicineTypeByIdHandler.cs` | Returns MedicineTypeDto; throws NotFoundException |
| `src/TenantCore.Application/Features/MedicineTypes/Validators/CreateMedicineTypeCommandValidator.cs` | Validates Name and Description lengths |
| `src/TenantCore.Application/Features/MedicineTypes/Validators/UpdateMedicineTypeCommandValidator.cs` | Validates Id not empty; Name and Description lengths |
| `src/TenantCore.Application/Features/Medicines/Translators/MedicineTranslator.cs` | Static translator: ToDto (includes MedicineTypeName), ToDtoList |
| `src/TenantCore.Application/Features/Medicines/Commands/CreateMedicineCommand.cs` | sealed record with all 16 write-able Medicine fields plus MedicineTypeId? |
| `src/TenantCore.Application/Features/Medicines/Commands/UpdateMedicineCommand.cs` | sealed record: Id + all 16 write-able fields + MedicineTypeId? |
| `src/TenantCore.Application/Features/Medicines/Queries/GetMedicinesQuery.cs` | sealed record: Page, PageSize, Search?, BrandName?, GenericName?, MedicineTypeId?, IsGeneric? |
| `src/TenantCore.Application/Features/Medicines/Queries/GetMedicineByIdQuery.cs` | sealed record: Id |
| `src/TenantCore.Application/Features/Medicines/Handlers/CreateMedicineHandler.cs` | Calls FindSimilarAsync; if matches found throws InvalidOperationException listing conflicts; creates entity |
| `src/TenantCore.Application/Features/Medicines/Handlers/UpdateMedicineHandler.cs` | Loads by Id (NotFoundException); calls FindSimilarAsync excluding current Id; updates entity |
| `src/TenantCore.Application/Features/Medicines/Handlers/GetMedicinesHandler.cs` | Returns PagedResult\<MedicineDto\> with all filters applied |
| `src/TenantCore.Application/Features/Medicines/Handlers/GetMedicineByIdHandler.cs` | Returns MedicineDto with MedicineType navigation data; throws NotFoundException |
| `src/TenantCore.Application/Features/Medicines/Validators/CreateMedicineCommandValidator.cs` | Validates all required string fields with MaxLength matching DB schema |
| `src/TenantCore.Application/Features/Medicines/Validators/UpdateMedicineCommandValidator.cs` | Same rules as create plus Id not empty |
| `src/TenantCore.Api/Controllers/MedicineTypesController.cs` | GET (paged), GET /{id}, POST, PUT /{id} — no DELETE |
| `src/TenantCore.Api/Controllers/MedicinesController.cs` | GET (paged + filters), GET /{id}, POST, PUT /{id} — no DELETE |
| `src/TenantCore.Web.Client/Clients/IMedicineApiClient.cs` | Interface: GetMedicinesAsync, GetMedicineByIdAsync, CreateMedicineAsync, UpdateMedicineAsync, GetMedicineTypesAsync, CreateMedicineTypeAsync, UpdateMedicineTypeAsync |
| `src/TenantCore.Web.Client/Clients/MedicineApiClient.cs` | Typed HttpClient implementation of IMedicineApiClient |
| `src/TenantCore.Web.Client/Pages/Medicines/MedicineList.razor` | Full medicine list page: MudDataGrid with search bar, brand name, generic name, type dropdown, and IsGeneric filter chip; create/edit via MudDialog; role-guard hides Add/Edit buttons for non-management roles |
| `src/TenantCore.Web.Client/Pages/Medicines/MedicineTypeList.razor` | Medicine type management page: MudDataGrid with search toolbar; Add/Edit via MudDialog |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add `DbSet<MedicineType> MedicineTypes`, `DbSet<Medicine> Medicines` |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `IMedicineTypeRepository → MedicineTypeRepository`, `IMedicineRepository → MedicineRepository` as Scoped |
| `src/TenantCore.Web.Client/Layout/NavMenu.razor` | Add `MudNavGroup Title="Medicines"` with two `MudNavLink` entries: `/medicines` (All Medicines, LocalPharmacy icon), `/medicine-types` (Medicine Types, Category icon) |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|--------------|----------|-------------|
| GET | /api/medicine-types | — | `PagedResult<MedicineTypeDto>` | RequireAuthenticated |
| GET | /api/medicine-types/{id} | — | `MedicineTypeDto` | RequireAuthenticated |
| POST | /api/medicine-types | `CreateMedicineTypeDto` | `MedicineTypeDto` (201) | RequireManagement |
| PUT | /api/medicine-types/{id} | `UpdateMedicineTypeDto` | `MedicineTypeDto` | RequireManagement |
| GET | /api/medicines | — | `PagedResult<MedicineDto>` | RequireAuthenticated |
| GET | /api/medicines/{id} | — | `MedicineDto` | RequireAuthenticated |
| POST | /api/medicines | `CreateMedicineDto` | `MedicineDto` (201) | RequireManagement |
| PUT | /api/medicines/{id} | `UpdateMedicineDto` | `MedicineDto` | RequireManagement |

_No DELETE endpoints are exposed. Medicine deletion is reserved for the future super-admin dashboard._

---

## Entity Properties

### MedicineType
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK (from AuditableEntity) |
| `Name` | `string` | Required, MaxLength(50); DB column: `Name` |
| `Description` | `string?` | MaxLength(500) |
| `IsActive` | `bool` | Default true |
| `CreatedAt` | `DateTime` | from AuditableEntity; DB column: `CreatedDate` |
| `UpdatedAt` | `DateTime?` | from AuditableEntity; DB column: `ModifiedDate` |
| `CreatedBy` | `string?` | from AuditableEntity; DB column: `CreatedBy` |
| `UpdatedBy` | `string?` | from AuditableEntity; DB column: `ModifiedBy` |

### Medicine
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required, MaxLength(200) |
| `GenericName` | `string?` | MaxLength(200) |
| `BrandName` | `string?` | MaxLength(200) |
| `Description` | `string?` | MaxLength(1000) |
| `Composition` | `string?` | MaxLength(500) |
| `Composition2` | `string?` | MaxLength(500) |
| `Dosage` | `string?` | MaxLength(200) |
| `Form` | `string?` | MaxLength(50) |
| `Manufacturer` | `string?` | MaxLength(200) |
| `IsGeneric` | `bool` | Default false |
| `PackSize` | `string?` | MaxLength(100) |
| `Uses` | `string?` | MaxLength(1000) |
| `SideEffects` | `string?` | MaxLength(1000) |
| `Contraindications` | `string?` | MaxLength(1000) |
| `Storage` | `string?` | MaxLength(200) |
| `IsActive` | `bool` | Default true |
| `MedicineTypeId` | `Guid?` | FK → MedicineTypes.Id, ON DELETE SET NULL |
| `MedicineType` | `MedicineType?` | Navigation |
| `CreatedAt` | `DateTime` | DB column: `CreatedDate` |
| `UpdatedAt` | `DateTime?` | DB column: `ModifiedDate` |
| `CreatedBy` | `string?` | DB column: `CreatedBy` |
| `UpdatedBy` | `string?` | DB column: `ModifiedBy` |

---

## Validation Rules

### CreateMedicineTypeCommand / UpdateMedicineTypeCommand
| Field | Rule |
|-------|------|
| `Name` | NotEmpty, MaxLength(50) |
| `Description` | MaxLength(500) when provided |
| `Id` (Update only) | NotEmpty |

### CreateMedicineCommand / UpdateMedicineCommand
| Field | Rule |
|-------|------|
| `Name` | NotEmpty, MaxLength(200) |
| `GenericName` | MaxLength(200) when provided |
| `BrandName` | MaxLength(200) when provided |
| `Description` | MaxLength(1000) when provided |
| `Composition` | MaxLength(500) when provided |
| `Composition2` | MaxLength(500) when provided |
| `Dosage` | MaxLength(200) when provided |
| `Form` | MaxLength(50) when provided |
| `Manufacturer` | MaxLength(200) when provided |
| `PackSize` | MaxLength(100) when provided |
| `Uses` | MaxLength(1000) when provided |
| `SideEffects` | MaxLength(1000) when provided |
| `Contraindications` | MaxLength(1000) when provided |
| `Storage` | MaxLength(200) when provided |
| `Id` (Update only) | NotEmpty |

---

## Business Rules

- **Global catalogue**: Medicines and MedicineTypes are not scoped by `ApplicationId`. All clinics share the same records. Repository implementations must NOT filter by ApplicationId.
- **Fuzzy duplicate detection on create**: Before inserting a new medicine, `FindSimilarAsync` searches existing medicines by case-insensitive contains match on `Name`, `GenericName`, and `BrandName`. If any match is found, throw `InvalidOperationException` with a message listing the conflicting medicine names.
- **Fuzzy duplicate detection on update**: Same as create but excludes the current medicine's own Id from the match set.
- **MedicineType name uniqueness**: Duplicate name (case-insensitive) check before insert; throw `InvalidOperationException` if duplicate found.
- **No delete via API**: No DELETE endpoints for Medicines or MedicineTypes. Deactivation (`IsActive = false`) via PUT is the soft-delete mechanism available to authorised roles.
- **Access control**: GET endpoints require `RequireAuthenticated`; POST and PUT require `RequireManagement`. Read access is granted to all authenticated clinic users; write access is restricted to admin and management roles (which covers doctor-level access in the existing policy definitions).
- **Navigation property loading**: `GetMedicineByIdQuery` and list queries must include the `MedicineType` navigation property (via `.Include()`) so the translator can populate `MedicineTypeName` in the DTO.
- **IsActive filter**: List queries default to returning only active records. Optional `includeInactive` query param can be added to list endpoints for admin use.

---

## Implementation Order

1. `src/TenantCore.Domain/Entities/MedicineType.cs` — create entity (AuditableEntity, static factory Create, Update method, Deactivate/Activate)
2. `src/TenantCore.Domain/Entities/Medicine.cs` — create entity with all properties, nullable FK to MedicineType, navigation property, static factory, Update method
3. `src/TenantCore.Domain/Interfaces/IMedicineTypeRepository.cs` — create interface extending IRepository\<MedicineType\>; add GetPagedAsync and GetByNameAsync
4. `src/TenantCore.Domain/Interfaces/IMedicineRepository.cs` — create interface; add GetPagedAsync(Page, PageSize, Search?, BrandName?, GenericName?, MedicineTypeId?, IsGeneric?, IncludeInactive?) and FindSimilarAsync(name, genericName?, brandName?, excludeId?)
5. `src/TenantCore.Shared/Dtos/MedicineTypeDto.cs` — MedicineTypeDto class + CreateMedicineTypeDto sealed record + UpdateMedicineTypeDto sealed record
6. `src/TenantCore.Shared/Dtos/MedicineDto.cs` — MedicineDto class (includes MedicineTypeName) + CreateMedicineDto sealed record + UpdateMedicineDto sealed record
7. `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/MedicineTypeConfiguration.cs` — EF IEntityTypeConfiguration; HasKey, property constraints, column name mappings (CreatedDate, ModifiedDate, ModifiedBy)
8. `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/MedicineConfiguration.cs` — all property constraints, column name mappings, HasOne/WithMany for MedicineType FK with ON DELETE SET NULL
9. `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` — add DbSet\<MedicineType\> and DbSet\<Medicine\>
10. `src/TenantCore.Infrastructure/Repositories/MedicineTypeRepository.cs` — implement IMedicineTypeRepository; use ClinicDbContext directly; GetPagedAsync with name search and IsActive filter
11. `src/TenantCore.Infrastructure/Repositories/MedicineRepository.cs` — implement IMedicineRepository; GetPagedAsync with all 5 filter params; FindSimilarAsync using EF .Where() with case-insensitive contains; queries include MedicineType navigation
12. `src/TenantCore.Infrastructure/DependencyInjection.cs` — register IMedicineTypeRepository and IMedicineRepository as Scoped
13. `src/TenantCore.Application/Features/MedicineTypes/Translators/MedicineTypeTranslator.cs` — static class; ToDto, ToDtoList
14. `src/TenantCore.Application/Features/MedicineTypes/Commands/CreateMedicineTypeCommand.cs` — sealed record: Name, Description
15. `src/TenantCore.Application/Features/MedicineTypes/Commands/UpdateMedicineTypeCommand.cs` — sealed record: Id, Name, Description, IsActive
16. `src/TenantCore.Application/Features/MedicineTypes/Queries/GetMedicineTypesQuery.cs` — sealed record: Page, PageSize, Search?
17. `src/TenantCore.Application/Features/MedicineTypes/Queries/GetMedicineTypeByIdQuery.cs` — sealed record: Id
18. `src/TenantCore.Application/Features/MedicineTypes/Handlers/CreateMedicineTypeHandler.cs`
19. `src/TenantCore.Application/Features/MedicineTypes/Handlers/UpdateMedicineTypeHandler.cs`
20. `src/TenantCore.Application/Features/MedicineTypes/Handlers/GetMedicineTypesHandler.cs`
21. `src/TenantCore.Application/Features/MedicineTypes/Handlers/GetMedicineTypeByIdHandler.cs`
22. `src/TenantCore.Application/Features/MedicineTypes/Validators/CreateMedicineTypeCommandValidator.cs`
23. `src/TenantCore.Application/Features/MedicineTypes/Validators/UpdateMedicineTypeCommandValidator.cs`
24. `src/TenantCore.Application/Features/Medicines/Translators/MedicineTranslator.cs`
25. `src/TenantCore.Application/Features/Medicines/Commands/CreateMedicineCommand.cs`
26. `src/TenantCore.Application/Features/Medicines/Commands/UpdateMedicineCommand.cs`
27. `src/TenantCore.Application/Features/Medicines/Queries/GetMedicinesQuery.cs`
28. `src/TenantCore.Application/Features/Medicines/Queries/GetMedicineByIdQuery.cs`
29. `src/TenantCore.Application/Features/Medicines/Handlers/CreateMedicineHandler.cs`
30. `src/TenantCore.Application/Features/Medicines/Handlers/UpdateMedicineHandler.cs`
31. `src/TenantCore.Application/Features/Medicines/Handlers/GetMedicinesHandler.cs`
32. `src/TenantCore.Application/Features/Medicines/Handlers/GetMedicineByIdHandler.cs`
33. `src/TenantCore.Application/Features/Medicines/Validators/CreateMedicineCommandValidator.cs`
34. `src/TenantCore.Application/Features/Medicines/Validators/UpdateMedicineCommandValidator.cs`
35. `src/TenantCore.Api/Controllers/MedicineTypesController.cs` — GET (paged), GET /{id}, POST, PUT /{id}
36. `src/TenantCore.Api/Controllers/MedicinesController.cs` — GET (paged + query filters), GET /{id}, POST, PUT /{id}
37. `src/TenantCore.Web.Client/Clients/IMedicineApiClient.cs` — 7-method interface
38. `src/TenantCore.Web.Client/Clients/MedicineApiClient.cs` — typed HttpClient implementation; register with DI
39. `src/TenantCore.Web.Client/Pages/Medicines/MedicineList.razor` — MudDataGrid; top toolbar with search input + BrandName + GenericName text fields + Type MudSelect dropdown + IsGeneric chip toggle; Add/Edit buttons (role-gated); MudDialog for create/edit form with all fields; pagination
40. `src/TenantCore.Web.Client/Pages/Medicines/MedicineTypeList.razor` — MudDataGrid with search toolbar; Add/Edit via MudDialog
41. `src/TenantCore.Web.Client/Layout/NavMenu.razor` — add MudNavGroup "Medicines" (LocalPharmacy icon) with 2 MudNavLink entries
42. Run migration: `dotnet ef migrations add AddMedicinesFeature --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api`

---

## Migration Name

`AddMedicinesFeature`

---

## Execution Status

- **Status**: Plan fully executed and completed
- **Started**: 2026-04-25
- **Development completed**: 2026-04-25
- **Security check completed**: 2026-04-25
- **Completed**: 2026-04-25
