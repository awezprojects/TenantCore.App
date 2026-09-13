# ADR-001 — Solution Structure & Clean Architecture

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** Cross-cutting (solution-level)

---

## Decision

TenantCore.App follows **Clean Architecture** with a strict four-layer dependency rule. The solution is organized into 6 source projects + 2 test projects.

---

## Project Responsibilities

### `TenantCore.Domain` — The Core
- **What lives here:** Entities, repository interfaces, domain exceptions, base classes
- **Depends on:** Nothing (zero project references)
- **Rules:**
  - All entities inherit from `BaseEntity` or `AuditableEntity`
  - Repository contracts (`IRepository<T>`, specialized interfaces) live here
  - NO EF Core, NO MediatR, NO framework dependencies
  - This is pure C# — it must be portable

### `TenantCore.Application` — Use Cases
- **What lives here:** CQRS commands, queries, handlers, validators, pipeline behaviors, service interfaces
- **Depends on:** `TenantCore.Domain`, `TenantCore.Shared`
- **Rules:**
  - All business logic goes here, not in controllers or repositories
  - External service interfaces (e.g., `IAuthApplicationService`, `IBlobStorageService`) are defined here, implemented in Infrastructure
  - MediatR and FluentValidation are the only framework dependencies allowed

### `TenantCore.Infrastructure` — Implementation Details
- **What lives here:** EF Core DbContext, repository implementations, external service implementations (Azure, Email, PDF, HTTP clients to Auth)
- **Depends on:** `TenantCore.Domain`, `TenantCore.Application`, `TenantCore.Shared`
- **Rules:**
  - Implements ALL interfaces from Domain (repositories) and Application (services)
  - DbContext and migrations live here — never in other layers
  - HTTP client wrappers for TenantCore.Auth API live here

### `TenantCore.Api` — Entry Point
- **What lives here:** Controllers, middleware, authorization handlers, DI wiring (Program.cs, extension methods)
- **Depends on:** All other projects (it is the composition root)
- **Rules:**
  - Controllers are thin — they receive a request, call MediatR, return a response
  - All DI registration for Application and Infrastructure is done via extension methods (`AddApplication()`, `AddInfrastructure()`)
  - Middleware pipeline order is fixed — see ADR-005

### `TenantCore.Shared` — Shared Contracts
- **What lives here:** Request/response DTOs, authorization constant strings, shared enums, error definitions
- **Depends on:** Nothing (zero project references)
- **Rules:**
  - No business logic — only data shapes and constants
  - DTOs here are safe to reference from any layer

### `TenantCore.Web.Client` — Blazor Frontend
- **What lives here:** Blazor WebAssembly pages, components, client-side HTTP services
- **Depends on:** `TenantCore.Shared` (for DTO types)
- **Rules:**
  - Never references server-side projects directly
  - All server communication goes through HTTP clients pointing to `TenantCore.Api`
  - Hosted and served by `TenantCore.Api` (via `AddBlazorWebAssembly`)

---

## Dependency Direction (Diagram)

```
┌─────────────────────────────────────┐
│          TenantCore.Api             │  ← Composition Root
│  (Controllers, Middleware, Program) │
└───────────┬─────────────────────────┘
            │ references
    ┌───────┴──────────────────────────────────────┐
    │                                              │
    ▼                                              ▼
┌──────────────────────┐          ┌───────────────────────────┐
│ TenantCore.Application│          │ TenantCore.Infrastructure │
│ (CQRS, Behaviors)    │          │ (EF Core, Repos, Services) │
└──────────┬───────────┘          └────────────┬──────────────┘
           │ references                         │ references
           └──────────────┬─────────────────────┘
                          ▼
              ┌───────────────────────┐
              │   TenantCore.Domain   │  ← No outward dependencies
              │ (Entities, Interfaces)│
              └───────────────────────┘

All layers also reference:
              ┌───────────────────────┐
              │   TenantCore.Shared   │  ← DTOs, Constants
              └───────────────────────┘
```

---

## Step-by-Step: Adding a New Feature Domain

When adding a completely new feature domain (e.g., "Billing"):

1. **Domain layer** — Add entity in `TenantCore.Domain/Entities/Billing.cs` inheriting `AuditableEntity`
2. **Domain layer** — Add repository interface in `TenantCore.Domain/Interfaces/IBillingRepository.cs`
3. **Infrastructure layer** — Add EF configuration in `TenantCore.Infrastructure/Persistence/Configurations/BillingConfiguration.cs`
4. **Infrastructure layer** — Add `DbSet<Billing> Billings` to `ClinicDbContext`
5. **Infrastructure layer** — Add repository implementation in `TenantCore.Infrastructure/Repositories/BillingRepository.cs`
6. **Infrastructure layer** — Register in `DependencyInjection.cs`
7. **Infrastructure layer** — Run EF migration: `dotnet ef migrations add AddBillingTable --project TenantCore.Infrastructure --startup-project TenantCore.Api`
8. **Shared layer** — Add request/response DTOs in `TenantCore.Shared/Dtos/`
9. **Application layer** — Create feature folder `TenantCore.Application/Features/Billing/` with Commands, Queries, Handlers, Validators, Translators
10. **API layer** — Add `BillingController : ClinicControllerBase` in `TenantCore.Api/Controllers/`

---

## What NOT to Do

- Do NOT add EF Core attributes (`[Key]`, `[Required]`) to Domain entities — use Fluent API configurations in Infrastructure
- Do NOT reference `TenantCore.Infrastructure` from `TenantCore.Application`
- Do NOT put business logic in controllers
- Do NOT add new top-level projects without explicit architecture review
