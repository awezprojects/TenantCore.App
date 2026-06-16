# TenantCore.App — ADR Index & Quick Reference

## Repository Path
`C:\Users\Dell\source\repos\awezprojects\TenantCore.App`

## Solution Overview

TenantCore.App is a **multi-tenant clinic management system** built with:
- **Backend:** ASP.NET Core 8 Web API (Clean Architecture + CQRS)
- **Frontend:** Blazor WebAssembly (hosted within the same API project)
- **Database:** SQL Server via Entity Framework Core 8
- **Auth:** JWT Bearer tokens validated against TenantCore.Auth service

## Project Structure at a Glance

```
TenantCore.App/
├── src/
│   ├── TenantCore.Api/           # Entry point — controllers, middleware, DI wiring
│   ├── TenantCore.Application/   # CQRS — commands, queries, handlers, validators
│   ├── TenantCore.Domain/        # Entities, repository interfaces, domain logic
│   ├── TenantCore.Infrastructure/ # EF Core, repositories, external services
│   ├── TenantCore.Shared/        # DTOs, enums, error codes, auth constants
│   └── TenantCore.Web.Client/    # Blazor WebAssembly frontend
├── tests/
│   ├── TenantCore.Application.Tests/
│   └── TenantCore.Domain.Tests/
└── tools/
    └── MedicineNormalizer/       # One-off console tool
```

## Dependency Direction (STRICTLY ENFORCED)

```
TenantCore.Api
    └──> TenantCore.Application
             └──> TenantCore.Domain
TenantCore.Api
    └──> TenantCore.Infrastructure
             └──> TenantCore.Domain
All projects
    └──> TenantCore.Shared  (read-only, no business logic here)
TenantCore.Web.Client
    └──> (no server-side references — communicates via HTTP only)
```

**Rule:** Inner layers NEVER reference outer layers. Domain knows nothing about Infrastructure or Application.

## ADR Documents

| ADR | Layer / Topic | Key Decisions |
|-----|--------------|---------------|
| [ADR-001](ADR-001-solution-structure.md) | Solution Structure | 6-project clean architecture, dependency rules |
| [ADR-002](ADR-002-domain-layer.md) | Domain Layer | BaseEntity, AuditableEntity, IRepository<T>, 17 repo interfaces |
| [ADR-003](ADR-003-application-layer.md) | Application Layer | CQRS, MediatR, Features folder, pipeline behaviors |
| [ADR-004](ADR-004-infrastructure-layer.md) | Infrastructure Layer | ClinicDbContext, ClinicRepository<T>, EF Fluent API, external services |
| [ADR-005](ADR-005-api-layer.md) | API Layer | ClinicControllerBase, middleware order, authorization policies |
| [ADR-006](ADR-006-shared-layer.md) | Shared Layer | DTOs, AuthorizationConstants, enums, error types |
| [ADR-007](ADR-007-blazor-client.md) | Blazor Client | Component structure, HTTP clients, SPA fallback |
| [ADR-008](ADR-008-multi-tenancy.md) | Multi-Tenancy | X-Application-Id header, JWT claims, per-tenant roles |
| [ADR-009](ADR-009-unit-testing.md) | Unit Testing | xUnit, Moq, FluentAssertions, test organization |

## Key Technology Decisions

| Concern | Decision | Package |
|---------|---------|---------|
| CQRS Mediator | MediatR | `MediatR 12.4.1` |
| Validation | FluentValidation | `FluentValidation.DependencyInjectionExtensions 11.11.0` |
| ORM | EF Core SQL Server | `Microsoft.EntityFrameworkCore.SqlServer 8.0.14` |
| Auth | JWT Bearer | `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.14` |
| Logging | Serilog | `Serilog.AspNetCore 8.0.3` |
| Blob Storage | Azure Blob | `Azure.Storage.Blobs 12.19.1` |
| Testing | xUnit + Moq + FluentAssertions | See ADR-009 |
| API Docs | Swagger/OpenAPI | `Swashbuckle.AspNetCore 7.3.1` |
