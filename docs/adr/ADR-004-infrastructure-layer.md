# ADR-004 — Infrastructure Layer: EF Core, Repositories & External Services

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** `TenantCore.Infrastructure`  
**Path:** `src/TenantCore.Infrastructure/`

---

## Decision

The Infrastructure layer contains all implementations that touch external systems: the SQL Server database (via EF Core 8), Azure Blob Storage, email sending, PDF generation, and the HTTP client for TenantCore.Auth. It implements all interfaces defined in `TenantCore.Domain` (repositories) and `TenantCore.Application` (services).

---

## Folder Structure

```
TenantCore.Infrastructure/
├── Persistence/
│   ├── ClinicDbContext.cs                  # Main EF Core DbContext
│   ├── ClinicDbContextFactory.cs           # Design-time factory (for migrations)
│   ├── ClinicMigrations/                   # EF Core migration files
│   └── Configurations/                     # Fluent API entity configurations
│       ├── PatientConfiguration.cs
│       ├── PrescriptionConfiguration.cs
│       └── ... (10+ files)
├── Repositories/
│   ├── ClinicRepository.cs                 # Generic base repository
│   ├── PatientRepository.cs
│   ├── MedicineRepository.cs
│   └── ... (17 total repository implementations)
├── Services/
│   ├── ApplicationAccessValidator.cs       # Multi-tenant access check
│   ├── AzureBlobStorageService.cs
│   ├── LocalFileStorageService.cs
│   ├── EmailService.cs
│   └── PdfConversionService.cs
├── ExternalServices/
│   ├── AuthApplicationService.cs           # HTTP client → TenantCore.Auth
│   └── AuthClinicService.cs
└── DependencyInjection.cs                  # Registers all infrastructure services
```

---

## Database: EF Core with SQL Server

### `ClinicDbContext`

The single DbContext for the entire App domain.

```csharp
// Key points of ClinicDbContext
public class ClinicDbContext : DbContext
{
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Medicine> Medicines { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    // ... 18 DbSets total
}
```

**Connection string key:** `ClinicConnection` in `appsettings.json`

**Rules:**
- One DbContext for the entire solution — do NOT create additional DbContexts
- The DbContext is `Scoped` (one per HTTP request)
- `SaveChangesAsync()` is always called through the repository, never directly from a controller or handler via the DbContext

### Entity Configurations (Fluent API)

Every entity has a dedicated configuration class. Never use Data Annotations on entities.

```csharp
// Persistence/Configurations/PatientConfiguration.cs
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.RowVersion)
            .IsRowVersion();          // Optimistic concurrency

        // Multi-tenancy filter — always filter by ApplicationId
        builder.HasQueryFilter(p => p.ApplicationId == EF.Property<Guid>(p, "ApplicationId"));
    }
}
```

**Rules:**
- One configuration class per entity, named `{Entity}Configuration.cs`
- Always configure `RowVersion` as a row version (optimistic concurrency)
- Apply global query filters for multi-tenancy where the entity is tenant-scoped
- Relationships (HasOne, HasMany) are defined in configurations, not as entity properties

### Migrations

- Migration project: `TenantCore.Infrastructure`
- Startup project: `TenantCore.Api`
- Migration folder: `Persistence/ClinicMigrations/`

**Command to add a migration:**
```bash
dotnet ef migrations add <MigrationName> \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api \
  --output-dir Persistence/ClinicMigrations
```

**Command to apply migrations:**
```bash
dotnet ef database update \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api
```

**Rules:**
- NEVER edit existing migration files — add a new migration to fix mistakes
- NEVER call `Database.Migrate()` or `Database.EnsureCreated()` in production startup (migrations are applied via CLI or CI/CD)
- Name migrations clearly: `AddPatientMrNumber`, `AddPrescriptionAuditFields`

---

## Repository Pattern

### `ClinicRepository<T>` — Generic Base

```csharp
public class ClinicRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ClinicDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public ClinicRepository(ClinicDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id) =>
        await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await _dbSet.ToListAsync();

    public async Task AddAsync(T entity) =>
        await _dbSet.AddAsync(entity);

    public void Update(T entity) =>
        _context.Entry(entity).State = EntityState.Modified;

    public void Delete(T entity) =>
        _dbSet.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id) =>
        await _dbSet.AnyAsync(e => e.Id == id);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
```

### Specialized Repositories

Inherit from `ClinicRepository<T>` and add domain-specific queries:

```csharp
public class PatientRepository : ClinicRepository<Patient>, IPatientRepository
{
    public PatientRepository(ClinicDbContext context) : base(context) { }

    public async Task<Patient?> GetByMrNumberAsync(string mrNumber, Guid applicationId) =>
        await _dbSet
            .Where(p => p.MrNumber == mrNumber && p.ApplicationId == applicationId)
            .FirstOrDefaultAsync();
}
```

**Rules:**
- All tenant-scoped queries MUST filter by `applicationId` — never return data from other tenants
- Use `AsNoTracking()` for read-only queries (queries that don't need change tracking)
- Do NOT expose `IQueryable` from repositories — always materialize the result (`ToListAsync`, `FirstOrDefaultAsync`, etc.)
- Complex joins or projections belong in specialized repository methods, not in handlers

### DI Registration for Repositories

In `DependencyInjection.cs`, all repositories are registered as `Scoped`:

```csharp
services.AddScoped<IPatientRepository, PatientRepository>();
services.AddScoped<IMedicineRepository, MedicineRepository>();
// ... all 17 repositories
```

---

## External Service Implementations

### Azure Blob Storage — `AzureBlobStorageService`

Implements `IBlobStorageService` (defined in Application layer).

- Configuration key: `BlobStorage:ConnectionString` and `BlobStorage:ContainerName`
- Used for storing prescription PDF reports

### Local File Storage — `LocalFileStorageService`

Implements `IFileStorageService` — fallback for local development when Azure is unavailable.

### Email Service — `EmailService`

Implements `IEmailService`. Configuration in `appsettings.json`.

### PDF Conversion — `PdfConversionService`

Implements `IPdfConversionService`. Converts HTML prescription reports to PDF.

> **Adding a new config key for any Infrastructure service?** Follow the `appsettings.json` / `appsettings.Local.json` layering rule in [ADR-005's Configuration Layering section](ADR-005-api-layer.md#configuration-layering--appsettingsjson-vs-appsettingslocaljson) — placeholder/empty value in `appsettings.json`, the real value in gitignored `appsettings.Local.json`. Every Infrastructure config key listed below (`BlobStorage:*`, `AppLogging:*`, `AuthApi:BaseUrl`, etc.) follows this rule.

### HTTP Clients for TenantCore.Auth

`AuthApplicationService` and `AuthClinicService` implement `IAuthApplicationService` and `IAuthClinicService`. They use the named `HttpClient` called `"AuthApi"`:

- Base URL config key: `AuthApi:BaseUrl`
- Registered in `Program.cs` via `builder.Services.AddHttpClient("AuthApi", ...)`

---

## DI Registration Overview (`DependencyInjection.cs`)

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    // Database
    services.AddDbContext<ClinicDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("ClinicConnection")));

    // Repositories — all Scoped
    services.AddScoped<IPatientRepository, PatientRepository>();
    // ... 17 total

    // Services — all Scoped
    services.AddScoped<IFileStorageService, LocalFileStorageService>();
    services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IPdfConversionService, PdfConversionService>();
    services.AddScoped<IApplicationAccessValidator, ApplicationAccessValidator>();
    services.AddScoped<IAuthApplicationService, AuthApplicationService>();
    services.AddScoped<IAuthClinicService, AuthClinicService>();

    return services;
}
```

---

## Step-by-Step: Adding a New Repository

1. Ensure the entity and `INewEntityRepository` exist in Domain (ADR-002)
2. Create `Persistence/Configurations/NewEntityConfiguration.cs`
3. Add `DbSet<NewEntity>` to `ClinicDbContext`
4. Create `Repositories/NewEntityRepository.cs` extending `ClinicRepository<NewEntity>` and implementing `INewEntityRepository`
5. Register in `DependencyInjection.cs`: `services.AddScoped<INewEntityRepository, NewEntityRepository>()`
6. Create and apply EF migration

---

## What NOT to Do

- Do NOT put business logic in repository methods — repositories are data access only
- Do NOT call `SaveChangesAsync()` from service implementations — only from repository methods called by handlers
- Do NOT use `_context` directly from outside this layer
- Do NOT skip the Fluent API configuration step — every entity needs a configuration class
