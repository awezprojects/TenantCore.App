# ADR-008 — Multi-Tenancy Pattern

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** Cross-cutting (API + Domain + Infrastructure)

---

## Decision

TenantCore.App is a **multi-tenant** system where each "clinic" is a tenant identified by an `ApplicationId` (a `Guid`). The tenant context flows from an HTTP header (`X-Application-Id`), through middleware validation, into every repository query and command. This ensures complete data isolation between clinics.

---

## Multi-Tenancy Architecture

```
HTTP Request
    │
    ├─ Header: X-Application-Id: {clinicGuid}
    │
    ▼
ClinicContextMiddleware
    ├─ Reads X-Application-Id header
    ├─ Reads JWT claim "app_ids" (comma-separated list of clinic IDs the user belongs to)
    ├─ Validates: is this ApplicationId in the user's app_ids claim?
    ├─ If YES → stores ApplicationId in HttpContext.Items["ApplicationId"]
    └─ If NO  → returns 403 Forbidden
    │
    ▼
ClinicControllerBase.GetApplicationId()
    ├─ Returns (Guid) HttpContext.Items["ApplicationId"]
    └─ Passed explicitly into every MediatR command/query
    │
    ▼
Application Layer (Handler)
    ├─ Receives ApplicationId as a field on the command/query
    └─ Passes it to repository methods
    │
    ▼
Repository Layer
    └─ Always filters queries with: WHERE ApplicationId = @applicationId
```

---

## JWT Claims for Multi-Tenancy

TenantCore.Auth issues JWT tokens with these multi-tenancy claims:

| Claim Key | Type | Example | Purpose |
|-----------|------|---------|---------|
| `app_ids` | `string` | `"guid1,guid2,guid3"` | Comma-separated list of all clinic IDs the user belongs to |
| `app_roles` | `string` (JSON) | `[{"appId":"...","roleName":"ClinicAdmin"}]` | User's role per clinic |
| `role` | `string` | `"ClinicAdmin"` | Global role (fallback) |

---

## ClinicContextMiddleware Behavior

```csharp
// Pseudocode of ClinicContextMiddleware
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    // Skip for non-authenticated routes (e.g., /health)
    if (!context.User.Identity?.IsAuthenticated == true)
    {
        await next(context);
        return;
    }

    var applicationId = context.Request.Headers["X-Application-Id"].FirstOrDefault();

    if (string.IsNullOrEmpty(applicationId))
    {
        context.Response.StatusCode = 403;
        return;
    }

    var userAppIds = context.User.FindFirst("app_ids")?.Value ?? string.Empty;
    var appIdList = userAppIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

    if (!appIdList.Contains(applicationId))
    {
        context.Response.StatusCode = 403;
        return;
    }

    context.Items["ApplicationId"] = Guid.Parse(applicationId);
    context.Items["ApplicationName"] = // resolved from app_roles claim
    await next(context);
}
```

---

## Per-Clinic Role Authorization

Beyond just belonging to a clinic, users have **roles within each clinic** (e.g., ClinicAdmin for Clinic A but Reception for Clinic B).

`ClinicRoleAuthorizationHandler` enforces this:

```
Policy: RequireClinicAdmin
    │
    ▼
ClinicRoleAuthorizationHandler
    ├─ Reads HttpContext.Items["ApplicationId"] (current clinic)
    ├─ Reads JWT claim "app_roles" (JSON array)
    ├─ Finds the entry for this specific ApplicationId
    ├─ Checks if roleName == "ClinicAdmin"
    └─ Succeeds or fails authorization
```

**This means:** A user can be a `ClinicAdmin` in Clinic A but only `Reception` in Clinic B. The `RequireClinicAdmin` policy will only pass for requests targeting Clinic A.

---

## Repository-Level Isolation

Every tenant-scoped repository query must filter by `applicationId`:

```csharp
// CORRECT — Always filter by applicationId
public async Task<IEnumerable<Patient>> GetAllAsync(Guid applicationId) =>
    await _dbSet
        .Where(p => p.ApplicationId == applicationId)
        .ToListAsync();

// WRONG — Missing applicationId filter (data leak across tenants)
public async Task<IEnumerable<Patient>> GetAllAsync() =>
    await _dbSet.ToListAsync();
```

EF Core global query filters in `PatientConfiguration.cs` provide an additional safety net, but **explicit `applicationId` parameters in repository methods are the primary guard** and must always be present.

---

## ApplicationId Flow in Commands and Queries

Every command and query that reads or writes tenant-scoped data must include `ApplicationId`:

```csharp
// Command
public record CreatePatientCommand : IRequest<Guid>
{
    public Guid ApplicationId { get; init; }  // Always included
    public string FirstName { get; init; }
    // ...
}

// Handler
public async Task<Guid> Handle(CreatePatientCommand request, CancellationToken ct)
{
    var patient = new Patient
    {
        ApplicationId = request.ApplicationId,  // Set on entity
        FirstName = request.FirstName,
        // ...
    };
    await _repo.AddAsync(patient);
    await _repo.SaveChangesAsync();
    return patient.Id;
}
```

---

## Entities with ApplicationId

Not all entities are tenant-scoped. The rule is:

| Entity Type | Has ApplicationId? | Example |
|------------|-------------------|---------|
| Clinic-specific data | YES | Patient, Prescription, DoctorProfile |
| Global reference/lookup data | NO | MedicineDosageForm, DoctorSpeciality |
| Clinic configuration | YES | PrescriptionConfig, ClinicFeeConfig |

If in doubt, ask: "Can two different clinics have different values/records for this entity?" If yes → tenant-scoped, add `ApplicationId`.

---

## What NOT to Do

- Do NOT call `GetApplicationId()` and pass the result as a hard-coded string — always pass as `Guid`
- Do NOT skip the `ApplicationId` filter in repository queries for tenant-scoped entities
- Do NOT expose data across clinics by returning the full table without a tenant filter
- Do NOT trust the `ApplicationId` from the request body — always use `GetApplicationId()` from the authenticated context (header-validated by middleware)
- Do NOT change `X-Application-Id` to be a query parameter — it is always a header
