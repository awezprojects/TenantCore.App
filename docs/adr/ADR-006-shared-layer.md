# ADR-006 — Shared Layer: DTOs, Constants & Enums

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** `TenantCore.Shared`  
**Path:** `src/TenantCore.Shared/`

---

## Decision

`TenantCore.Shared` is a **zero-dependency project** that holds data contracts (DTOs), authorization constants, shared enumerations, and error definitions. It is safe to reference from any layer. It contains NO business logic.

---

## Folder Structure

```
TenantCore.Shared/
├── Authorization/
│   └── AuthorizationConstants.cs   # Policy names, role names
├── Common/
│   └── (shared utilities if any)
├── Dtos/
│   ├── PatientDto.cs
│   ├── MedicineDto.cs
│   ├── PrescriptionDto.cs
│   └── ... (one DTO file per domain area)
├── Enums/
│   └── Gender.cs
│   └── ... (shared enumerations)
└── Errors/
    └── (error code definitions)
```

---

## DTOs

DTOs (Data Transfer Objects) are the API contract — what flows in and out of the API boundary.

### DTO Naming Conventions

| Scenario | Naming Example | Used For |
|---------|---------------|---------|
| Read response | `PatientDto` | GET endpoints returning a single entity |
| List response | `PatientSummaryDto` | GET endpoints returning a list (leaner than full DTO) |
| Create request | `CreatePatientRequest` | POST request body |
| Update request | `UpdatePatientRequest` | PUT/PATCH request body |

### DTO Rules

```csharp
// Always use records for DTOs — immutable, value-equality
public record PatientDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public Gender Gender { get; init; }
    public DateTime DateOfBirth { get; init; }
}
```

- DTOs are `record` types (immutable, init-only properties)
- DTOs contain NO methods, NO validation attributes, NO business logic
- DTOs reference only other DTOs, shared enums, and primitives — never domain entities
- Request DTOs (incoming) may use `System.ComponentModel.DataAnnotations` ONLY for Swagger documentation purposes (not for validation — validation is done in FluentValidation validators in the Application layer)

---

## Authorization Constants

```csharp
// TenantCore.Shared/Authorization/AuthorizationConstants.cs
public static class AuthorizationConstants
{
    public static class AuthPolicies
    {
        public const string RequireAuthenticated = "RequireAuthenticated";
        public const string RequireClinicAdmin = "RequireClinicAdmin";
        public const string RequireReception = "RequireReception";
        public const string RequireClinical = "RequireClinical";
    }

    public static class AppRoles
    {
        public const string ClinicAdmin = "ClinicAdmin";
        public const string Reception = "Reception";
        public const string Clinical = "Clinical";
    }
}
```

**Rules:**
- NEVER use raw string literals for policy names or role names in controllers/handlers — always use these constants
- When adding a new policy or role, add it here FIRST, then register it in `Program.cs` authorization setup
- Policy names and role names must exactly match what TenantCore.Auth issues in JWT claims

---

## Shared Enumerations

```csharp
// TenantCore.Shared/Enums/Gender.cs
public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}
```

**Rules:**
- Always use explicit integer values in enums (never leave them implicit) — ensures DB migration safety
- Enum names must match what the frontend Blazor client expects
- Add new enum values at the END (never reorder or reassign existing values)

---

## Error Definitions

Error types or error code constants that are shared between layers go here. These should be strings or strongly-typed error objects that are safe to serialize in API responses.

---

## Step-by-Step: Adding a New DTO

1. Determine which request/response shapes are needed for the new feature
2. Create the DTO in `TenantCore.Shared/Dtos/{Feature}Dto.cs`
3. Use `record` type with `init`-only properties
4. If the response is a list, create a slimmer summary DTO (e.g., `BillingSummaryDto`) with only the fields needed in list views
5. Reference the DTO in the Application layer query/command return type
6. Reference the DTO in the controller's `ActionResult<T>` return type

---

## What NOT to Do

- Do NOT add NuGet package dependencies to this project (it must stay zero-dependency)
- Do NOT add business logic, validation, or methods to DTOs
- Do NOT add DTOs for internal types that never cross the API boundary (use domain entities or anonymous types internally)
- Do NOT put infrastructure-specific models here (e.g., Azure SDK response types)
