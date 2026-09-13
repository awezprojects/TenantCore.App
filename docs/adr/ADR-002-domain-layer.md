# ADR-002 — Domain Layer

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** `TenantCore.Domain`  
**Path:** `src/TenantCore.Domain/`

---

## Decision

The Domain layer is the innermost layer of the clean architecture. It owns entity definitions, repository contracts, and domain exceptions. It has **zero project references** — it is the only project that nothing depends on inward.

---

## Folder Structure

```
TenantCore.Domain/
├── Common/
│   ├── BaseEntity.cs          # Root base class for all entities
│   └── AuditableEntity.cs     # Extends BaseEntity with audit fields
├── Entities/                  # 18 domain entities
│   ├── Patient.cs
│   ├── OpdRegistration.cs
│   ├── IpdRegistration.cs
│   ├── Medicine.cs
│   ├── MedicineType.cs
│   ├── MedicineDosageForm.cs
│   ├── Prescription.cs
│   ├── PrescriptionItem.cs
│   ├── PrescriptionReport.cs
│   ├── PrescriptionConfig.cs
│   ├── ObstetricPrescriptionData.cs
│   ├── DosageRemark.cs
│   ├── DoctorProfile.cs
│   ├── DoctorSpeciality.cs
│   ├── Ward.cs
│   ├── Room.cs
│   ├── Bed.cs
│   └── ClinicFeeConfig.cs
├── Interfaces/                # Repository contracts
│   ├── IRepository.cs         # Generic base interface
│   ├── IPatientRepository.cs
│   ├── IMedicineRepository.cs
│   ├── IPrescriptionRepository.cs
│   └── ... (17 total)
└── Exceptions/                # Domain-specific exceptions
```

---

## Base Classes

### `BaseEntity`

All domain entities **must** inherit from `BaseEntity`.

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; }  // Optimistic concurrency
}
```

**Rules:**
- `Id` is always a `Guid`, never `int` or `long`
- `RowVersion` enables optimistic concurrency — do not remove it
- `CreatedAt` / `UpdatedAt` are set by the infrastructure layer (EF interceptors or `SaveChanges` override)

### `AuditableEntity : BaseEntity`

Entities that need **who** created/updated them inherit from `AuditableEntity`.

```csharp
public abstract class AuditableEntity : BaseEntity
{
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}
```

**Use `AuditableEntity` for:** Patient, Medicine, Prescription, DoctorProfile — any entity where the user who made the change is clinically significant.

**Use `BaseEntity` for:** lookup/reference data (MedicineDosageForm, Ward, Room, Bed) where audit trail of who changed them is not needed.

---

## Repository Interfaces

### Generic Interface: `IRepository<T>`

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(Guid id);
    Task SaveChangesAsync();
}
```

**Rules:**
- `SaveChangesAsync()` is on the repository (implicit unit of work per request)
- Methods are `async` where they touch the database
- `Update` and `Delete` are synchronous (EF change tracking — no DB call yet)

### Specialized Repository Interfaces

Each entity with complex query needs gets its own interface extending `IRepository<T>`:

```csharp
public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByMrNumberAsync(string mrNumber, Guid applicationId);
    Task<IEnumerable<Patient>> SearchAsync(string term, Guid applicationId);
    // ... domain-specific query methods
}
```

**Pattern:**
- Specialized interfaces live alongside the generic interface in `Interfaces/`
- They add ONLY methods that cannot be expressed with the generic interface
- Method signatures always accept `Guid applicationId` (multi-tenancy) where the result must be tenant-scoped

### Current Repository Interface List (17 total)

| Interface | Purpose |
|-----------|---------|
| `IRepository<T>` | Generic base |
| `IPatientRepository` | Patient search, MR number lookup |
| `IPickRepository` | Lookup/pick-list data |
| `IMedicineRepository` | Medicine search |
| `IPrescriptionRepository` | Prescription with items |
| `IOpdRegistrationRepository` | OPD records |
| `IIpdRegistrationRepository` | IPD records |
| `IDoctorProfileRepository` | Doctor data |
| `IDoctorSpecialityRepository` | Speciality data |
| `IMedicineTypeRepository` | Medicine types |
| `IMedicineDosageFormRepository` | Dosage forms |
| `IDosageRemarkRepository` | Dosage remarks |
| `IPrescriptionConfigRepository` | Config per clinic |
| `IPrescriptionReportRepository` | Report storage |
| `IClinicFeeConfigRepository` | Fee config |
| `IWardRepository` | Ward data |
| `IRoomRepository` / `IBedRepository` | Room/bed data |

---

## Domain Exceptions

Domain exceptions represent **invariant violations** — things that should never happen if the code is correct.

```csharp
// Example pattern
public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, Guid id)
        : base($"{entityName} with id '{id}' was not found.") { }
}
```

**Rules:**
- Throw domain exceptions from handlers when a required entity doesn't exist
- Do NOT catch domain exceptions in handlers — let the `ExceptionHandlingMiddleware` map them to HTTP responses
- Do NOT throw `ArgumentNullException` or `InvalidOperationException` for domain invariants — use named domain exceptions

---

## Step-by-Step: Adding a New Entity

1. Decide: does this entity need audit fields? → `AuditableEntity` or `BaseEntity`
2. Create `TenantCore.Domain/Entities/NewEntity.cs`
3. Add only domain-relevant properties — no EF annotations, no navigation property loading hints
4. Create `TenantCore.Domain/Interfaces/INewEntityRepository.cs` extending `IRepository<NewEntity>`
5. Add query methods only if the generic interface doesn't cover the use case
6. Proceed to ADR-004 (Infrastructure) to implement the repository and add it to DbContext

---

## What NOT to Do

- Do NOT add `[Required]`, `[MaxLength]`, or any Data Annotation attributes — use Fluent API in Infrastructure
- Do NOT add EF navigation properties loading behavior hints here
- Do NOT reference any NuGet packages except `Microsoft.Extensions.*` abstractions if absolutely needed
- Do NOT add business logic methods to entities (this is not DDD rich domain model — it uses anemic entities with logic in handlers)
